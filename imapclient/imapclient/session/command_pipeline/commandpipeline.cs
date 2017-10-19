using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private sealed partial class cCommandPipeline : IDisposable
            {
                private static readonly cBytes kPlusSpace = new cBytes("+ ");

                // for tracing
                private static readonly int[] kBufferStartPointsBeginning = new int[1] { 0 };

                private bool mDisposed = false;

                // state
                private enum eState { notconnected, connecting, connected, enabled, stopped }
                private eState mState = eState.notconnected;

                // connection
                private readonly cConnection mConnection = new cConnection();

                // stuff
                private readonly cCallbackSynchroniser mSynchroniser;
                private readonly Action<cTrace.cContext> mDisconnected;
                private cIdleConfiguration mIdleConfiguration;
                private cBatchSizerConfiguration mAppendReadConfiguration;
                private cBatchSizer mAppendWriteSizer;

                // response text processing
                private readonly cResponseTextProcessor mResponseTextProcessor;

                // used to control idling
                private readonly cExclusiveAccess mIdleBlock = new cExclusiveAccess("idleblock", 100); 

                // installable components
                private readonly List<iResponseDataParser> mResponseDataParsers = new List<iResponseDataParser>();
                private readonly List<cUnsolicitedDataProcessor> mUnsolicitedDataProcessors = new List<cUnsolicitedDataProcessor>();

                // required background task objects
                private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource(); 
                private readonly cMethodControl mBackgroundMC;
                private readonly cReleaser mBackgroundReleaser;
                private readonly cAwaiter mBackgroundAwaiter;

                // background task
                private Task mBackgroundTask = null; // background task
                private Exception mBackgroundTaskException = null;

                // can be set only before and on enable
                private bool mLiteralPlus = false;
                private bool mLiteralMinus = false;

                // set on enable
                private cMailboxCache mMailboxCache = null;
                private bool mIdleCommandSupported = false;

                // commands
                private readonly object mPipelineLock = new object(); // access to commands is protected by locking this
                private readonly Queue<cCommand> mQueuedCommands = new Queue<cCommand>();
                private cCommand mCurrentCommand = null;
                private readonly cActiveCommands mActiveCommands = new cActiveCommands();

                // growable buffers
                private readonly cByteList mSendBuffer = new cByteList();
                private readonly List<int> mBufferStartPoints = new List<int>();
                private readonly cByteList mTraceBuffer = new cByteList();

                public cCommandPipeline(cCallbackSynchroniser pSynchroniser, Action<cTrace.cContext> pDisconnected, cIdleConfiguration pIdleConfiguration, cBatchSizerConfiguration pAppendReadConfiguration, cBatchSizerConfiguration pAppendWriteConfiguration, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cCommandPipeline), pIdleConfiguration);

                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mDisconnected = pDisconnected ?? throw new ArgumentNullException(nameof(pDisconnected));
                    mIdleConfiguration = pIdleConfiguration;
                    mAppendReadConfiguration = pAppendReadConfiguration;
                    mAppendWriteSizer = new cBatchSizer(pAppendWriteConfiguration);

                    mResponseTextProcessor = new cResponseTextProcessor(pSynchroniser);

                    // these depend on the cancellationtokensource being constructed
                    mBackgroundMC = new cMethodControl(System.Threading.Timeout.Infinite, mBackgroundCancellationTokenSource.Token);
                    mBackgroundReleaser = new cReleaser("commandpipeline_background", mBackgroundCancellationTokenSource.Token);
                    mBackgroundAwaiter = new cAwaiter(mBackgroundCancellationTokenSource.Token);

                    // plumbing
                    mIdleBlock.Released += mBackgroundReleaser.Release; // when the idle block is removed, kick the background process
                }

                private static readonly cBytes kAsteriskSpaceOKSpace = new cBytes("* OK ");
                private static readonly cBytes kAsteriskSpacePreAuthSpace = new cBytes("* PREAUTH ");
                private static readonly cBytes kAsteriskSpaceBYESpace = new cBytes("* BYE ");

                public async Task<sGreeting> ConnectAsync(cMethodControl pMC, cServer pServer, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ConnectAsync), pMC, pServer);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));

                    if (mState != eState.notconnected) throw new InvalidOperationException();
                    mState = eState.connecting;

                    try
                    {
                        await mConnection.ConnectAsync(pMC, pServer, lContext).ConfigureAwait(false);

                        var lHook = new cCommandHookInitial();

                        using (var lAwaiter = new cAwaiter(pMC))
                        {
                            while (true)
                            {
                                lContext.TraceVerbose("waiting");
                                await lAwaiter.AwaitAny(mConnection.GetBuildResponseTask(lContext)).ConfigureAwait(false);

                                var lLines = mConnection.GetResponse(lContext);
                                mSynchroniser.InvokeNetworkActivity(lLines, lContext);
                                var lCursor = new cBytesCursor(lLines);

                                if (lCursor.SkipBytes(kAsteriskSpaceOKSpace))
                                {
                                    cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextType.greeting, lCursor, lHook, lContext);
                                    lContext.TraceVerbose("got ok: {0}", lResponseText);

                                    mState = eState.connected;
                                    mBackgroundTask = ZBackgroundTaskAsync(lContext);
                                    return new sGreeting(eGreetingType.ok, null, lHook.Capabilities, lHook.AuthenticationMechanisms);
                                }

                                if (lCursor.SkipBytes(kAsteriskSpacePreAuthSpace))
                                {
                                    cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextType.greeting, lCursor, lHook, lContext);
                                    lContext.TraceVerbose("got preauth: {0}", lResponseText);

                                    mState = eState.connected;
                                    mBackgroundTask = ZBackgroundTaskAsync(lContext);
                                    return new sGreeting(eGreetingType.preauth, lResponseText, lHook.Capabilities, lHook.AuthenticationMechanisms);
                                }

                                if (lCursor.SkipBytes(kAsteriskSpaceBYESpace))
                                {
                                    cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextType.greeting, lCursor, lHook, lContext);
                                    lContext.TraceError("got bye: {0}", lResponseText);

                                    if (lHook.Capabilities != null) lContext.TraceError("received capability on a bye greeting");

                                    mConnection.Disconnect(lContext);

                                    mState = eState.stopped;
                                    return new sGreeting(eGreetingType.bye, lResponseText, null, null);
                                }

                                lContext.TraceError("unrecognised response: {0}", lLines);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        mConnection.Disconnect(lContext);
                        mState = eState.stopped;
                        throw;
                    }
                }

                public void SetCapabilities(cCapabilities pCapabilities, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(SetCapabilities));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mState != eState.connected) throw new InvalidOperationException();
                    if (pCapabilities == null) throw new ArgumentNullException(nameof(pCapabilities));

                    mLiteralPlus = pCapabilities.LiteralPlus;
                    mLiteralMinus = pCapabilities.LiteralMinus;
                }

                public bool TLSInstalled => mConnection.TLSInstalled;

                public void InstallTLS(cTrace.cContext pParentContext) 
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(InstallTLS));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mState != eState.connected) throw new InvalidOperationException();

                    mConnection.InstallTLS(lContext);
                }

                public bool SASLSecurityInstalled => mConnection.SASLSecurityInstalled;

                public void InstallSASLSecurity(cSASLSecurity pSASLSecurity, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(InstallSASLSecurity));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mState != eState.connected) throw new InvalidOperationException();

                    mConnection.InstallSASLSecurity(pSASLSecurity, lContext);
                }

                public void Enable(cMailboxCache pMailboxCache, cCapabilities pCapabilities, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(Enable));

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mState != eState.connected) throw new InvalidOperationException();

                    if (pMailboxCache == null) throw new ArgumentNullException(nameof(pMailboxCache));
                    if (pCapabilities == null) throw new ArgumentNullException(nameof(pCapabilities));

                    mResponseTextProcessor.Enable(pMailboxCache, lContext);

                    mMailboxCache = pMailboxCache;

                    mLiteralPlus = pCapabilities.LiteralPlus;
                    mLiteralMinus = pCapabilities.LiteralMinus;
                    mIdleCommandSupported = pCapabilities.Idle;

                    lock (mPipelineLock)
                    {
                        if (mState == eState.connected) mState = eState.enabled;
                    }

                    mBackgroundReleaser.Release(lContext); // to allow idle to start
                }

                public void Install(iResponseTextCodeParser pResponseTextCodeParser) => mResponseTextProcessor.Install(pResponseTextCodeParser);
                public void Install(iResponseDataParser pResponseDataParser) => mResponseDataParsers.Add(pResponseDataParser);
                public void Install(cUnsolicitedDataProcessor pUnsolicitedDataProcessor) => mUnsolicitedDataProcessors.Add(pUnsolicitedDataProcessor);

                public void SetIdleConfiguration(cIdleConfiguration pConfiguration, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(SetIdleConfiguration), pConfiguration);
                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    mIdleConfiguration = pConfiguration;
                    mBackgroundReleaser.Release(lContext);
                }

                public void SetAppendReadConfiguration(cBatchSizerConfiguration pConfiguration, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(SetAppendReadConfiguration), pConfiguration);
                    mAppendReadConfiguration = pConfiguration ?? throw new ArgumentNullException(nameof(pConfiguration));
                }

                public void SetAppendWriteConfiguration(cBatchSizerConfiguration pConfiguration, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(SetAppendWriteConfiguration), pConfiguration);
                    if (pConfiguration == null) throw new ArgumentNullException(nameof(pConfiguration));
                    mAppendWriteSizer = new cBatchSizer(pConfiguration);
                }

                public async Task<cExclusiveAccess.cToken> GetIdleBlockTokenAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(GetIdleBlockTokenAsync));
                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    var lTask = mIdleBlock.GetTokenAsync(pMC, lContext);
                    mBackgroundReleaser.Release(lContext);
                    return await lTask.ConfigureAwait(false);
                }

                private void ZIdleBlockReleased(cTrace.cContext pParentContext) => mBackgroundReleaser.Release(pParentContext);

                public async Task<cCommandResult> ExecuteAsync(cMethodControl pMC, sCommandDetails pCommandDetails, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ExecuteAsync), pMC, pCommandDetails);

                    if (mDisposed)
                    {
                        pCommandDetails.Disposables?.Dispose();
                        throw new ObjectDisposedException(nameof(cCommandPipeline));
                    }

                    if (mState < eState.connected)
                    {
                        pCommandDetails.Disposables?.Dispose();
                        throw new InvalidOperationException();
                    }

                    cCommand lCommand;

                    lock (mPipelineLock)
                    {
                        if (mState == eState.stopped)
                        {
                            pCommandDetails.Disposables.Dispose();
                            throw mBackgroundTaskException;
                        }

                        lCommand = new cCommand(pCommandDetails);
                        mQueuedCommands.Enqueue(lCommand);

                        mBackgroundReleaser.Release(lContext);
                    }

                    try { return await lCommand.WaitAsync(pMC, lContext).ConfigureAwait(false); }
                    finally
                    {
                        lock (mPipelineLock)
                        {
                            if (lCommand.State == eCommandState.queued) lCommand.SetComplete(lContext);
                        }
                    }
                }

                public void RequestStop(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(RequestStop));
                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    mBackgroundCancellationTokenSource.Cancel();
                }

                private async Task ZBackgroundTaskAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewRootMethod(nameof(cCommandPipeline), nameof(ZBackgroundTaskAsync));

                    Task lBackgroundSenderTask = null;

                    try
                    {
                        while (true)
                        {
                            mBackgroundReleaser.Reset(lContext);

                            if (lBackgroundSenderTask != null && lBackgroundSenderTask.IsCompleted)
                            {
                                lContext.TraceVerbose("sender is completed");
                                lBackgroundSenderTask.Wait(); // will throw if there was an error
                                lBackgroundSenderTask.Dispose();
                                lBackgroundSenderTask = null;
                            }

                            if (lBackgroundSenderTask == null &&
                                ((mCurrentCommand == null && mQueuedCommands.Count > 0) ||
                                 (mCurrentCommand != null && !mCurrentCommand.WaitingForContinuationRequest)
                                )
                               ) lBackgroundSenderTask = ZBackgroundSenderAsync(lContext);

                            if (lBackgroundSenderTask == null && mCurrentCommand == null && mActiveCommands.Count == 0)
                            {
                                var lIdleConfiguration = mIdleConfiguration;

                                cExclusiveAccess.cBlock lIdleBlockBlock = null;

                                if (mState == eState.enabled && lIdleConfiguration != null) lIdleBlockBlock = mIdleBlock.TryGetBlock(lContext);

                                if (lIdleBlockBlock != null)
                                {
                                    await ZIdleAsync(lIdleConfiguration, lContext).ConfigureAwait(false);
                                    lIdleBlockBlock.Dispose();
                                }
                                else
                                {
                                    lContext.TraceVerbose("there is nothing to do, waiting for something to change");
                                    await mBackgroundReleaser.GetAwaitReleaseTask(lContext).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                Task lBuildResponseTask = mConnection.GetBuildResponseTask(lContext);
                                Task lBackgroundReleaserTask = mBackgroundReleaser.GetAwaitReleaseTask(lContext);

                                Task lCompleted = await mBackgroundAwaiter.AwaitAny(lBuildResponseTask, lBackgroundReleaserTask, lBackgroundSenderTask).ConfigureAwait(false);

                                if (ReferenceEquals(lCompleted, lBuildResponseTask))
                                {
                                    var lLines = mConnection.GetResponse(lContext);
                                    mSynchroniser.InvokeNetworkActivity(lLines, lContext);
                                    var lCursor = new cBytesCursor(lLines);

                                    if (lCursor.SkipBytes(kPlusSpace))
                                    {
                                        if (mCurrentCommand == null) throw new cUnexpectedServerActionException(0, "unexpected continuation request (1)", lContext);

                                        if (!mCurrentCommand.WaitingForContinuationRequest) throw new cUnexpectedServerActionException(0, "unexpected continuation request (2)", lContext);

                                        if (mCurrentCommand.IsAuthentication)
                                        {
                                            await ZProcessChallengeAsync(lCursor, lContext).ConfigureAwait(false);
                                            continue;
                                        }
                                        else
                                        {
                                            mCurrentCommand.WaitingForContinuationRequest = false;
                                            return; // have to exit so that the sender has a chance to restart
                                        }
                                    }

                                    if (ZProcessData(lCursor, lContext)) continue;

                                    if (ZProcessActiveCommandCompletion(lCursor, lContext))
                                    {
                                        ;?; // log off exit

                                        if (mCurrentCommand == null && mActiveCommands.Count == 0)
                                        {
                                            lContext.TraceVerbose("there are no more commands to process responses for");
                                            return;
                                        }

                                        continue;
                                    }

                                    if (mCurrentCommand != null)
                                    {
                                        ;?; // log off exit

                                        if (ZProcessCommandCompletion(lCursor, mCurrentCommand, lContext))
                                        {
                                            mCurrentCommand = null;
                                            return;
                                        }
                                    }

                                    lContext.TraceError("unrecognised response: {0}", lLines);
                                }

                                // this check here to make sure logoff is handled correctly
                                if (mBackgroundCancellationTokenSource.IsCancellationRequested)
                                {
                                    lContext.TraceInformation("the pipeline is stopping as requested");
                                    mBackgroundTaskException = new cPipelineStoppedException();
                                    break;
                                }
                            }
                        }
                    }
                    catch (cUnilateralByeException e)
                    {
                        lContext.TraceInformation("the pipeline is stopping due to a unilateral bye");
                        mBackgroundTaskException = e;
                    }
                    catch (OperationCanceledException) when (mBackgroundCancellationTokenSource.IsCancellationRequested)
                    {
                        lContext.TraceInformation("the pipeline is stopping as requested");
                        mBackgroundTaskException = new cPipelineStoppedException();
                    }
                    catch (AggregateException e)
                    {
                        lContext.TraceException("the pipeline is stopping due to an unexpected exception", e);
                        var lException = e.Flatten();
                        if (lException.InnerExceptions.Count == 1) mBackgroundTaskException = new cPipelineStoppedException(lException.InnerExceptions[0], lContext);
                        else mBackgroundTaskException = new cPipelineStoppedException(e, lContext);
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException("the pipeline is stopping due to an unexpected exception", e);
                        mBackgroundTaskException = new cPipelineStoppedException(e, lContext);
                    }

                    // if the sender is still going this will kill it 
                    mConnection.Disconnect(lContext);

                    if (lBackgroundSenderTask != null)
                    {
                        lContext.TraceVerbose("waiting for the sender to complete");
                        try { await lBackgroundSenderTask.ConfigureAwait(false); }
                        catch (Exception e) { lContext.TraceException("the sender had an exception at pipeline stop", e); }
                        lBackgroundSenderTask.Dispose();
                    }

                    lock (mPipelineLock)
                    {
                        mState = eState.stopped;
                    }

                    foreach (var lCommand in mActiveCommands) lCommand.SetException(mBackgroundTaskException, lContext);
                    if (mCurrentCommand != null) mCurrentCommand.SetException(mBackgroundTaskException, lContext);
                    foreach (var lCommand in mQueuedCommands) lCommand.SetException(mBackgroundTaskException, lContext);

                    mDisconnected(lContext);
                }








                private async Task ZBackgroundSenderAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSenderAsync));

                    if (mCurrentCommand != null)
                    {
                        mBufferStartPoints.Add(0);
                        ZBackgroundSenderSendDataAsync(lContext);

                        ;?; // this is the sending of the data

                        x = ZSendAppendCurrentPartDataAndMoveNextPart(lContext);
                    }

                    while (true)
                    {
                        if (mCurrentCommand == null)
                        {
                            ZBackgroundSenderMoveNextCommandAndAppendTag(lContext);
                            if (mCurrentCommand == null) break;

                            if (mCurrentCommand.IsAuthentication)
                            {
                                // authentication can have no literal command parts

                                do
                                {
                                    ;?;
                                }
                                while (mCurrentCommand.MoveNext());


                                // waiting for challenge communication

                                ;?; // if it is authentication handle it specially to make it explicit that the handling is different

                                ;?; // the return value must specify that we are waiting for 

                                break;
                            }
                        }

                        if (ZBackgroundSenderAppendCurrentPartLiteralHeader()) break; // have to wait for continuation before sending the data

                        if (ZSendAppendCurrentPartDataAndMoveNextPart(lContext))
                        {
                            // this is authentication - have to wait for a challenge before sending more
                            mAuthenticateState = new cAuthenticateState();
                            break; 
                        }
                    }

                    await ZBackgroundSenderSendAsync(lContext).ConfigureAwait(false);
                }

                private Task ZBackgroundSenderSendDataAndMoveNextPartAsync(cTrace.cContext pParentContext)
                {
                    // appends the current part of the current command to the buffer to send
                    //  if that was the last part of the current command the command gets added to the active commands and the current command is nulled
                    //  (note: that this routine CANNOT be used for the authentication command)

                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZSendAppendCurrentPartDataAndMoveNextPart));

                    var lPart = mCurrentCommand.CurrentPart();

                    switch (lPart)
                    {
                        case cStreamCommandPart lStream:

                            ;?; // loop with calls to write buffer IF the buffer needs to be sent in parts
                            break;

                        case cLiteralCommandPart lLiteral:

                            
                            break;

                        case cTextCommandPart lText:

                            break;

                        default:

                            throw new cInternalErrorException(lContext);
                    }





                    ;?; // if the part is a stream part
                    // ask the part how much to get
                    // ask myself how much to send
                    //  choose the minimum
                    //  start a timer
                    //   getasync using the cancellation token

                    // reduce the number of bytes to go by the buffer we got
                    //  if there are bytes to go return true
                    //   (NOTE: that this means that this rouinte does the authenticate state)






                    mSendBuffer.AddRange(lPart.Bytes);

                    if (lPart.Secret) mTraceBuffer.Add(cASCII.NUL);
                    else mTraceBuffer.AddRange(lPart.Bytes);

                    if (mCurrentCommand.MoveNext()) return false;

                    mSendBuffer.Add(cASCII.CR);
                    mSendBuffer.Add(cASCII.LF);

                    mTraceBuffer.Add(cASCII.CR);
                    mTraceBuffer.Add(cASCII.LF);

                    if (mCurrentCommand.IsAuthentication) return true;

                    // the current command can be added to the list of active commands now
                    lock (mPipelineLock)
                    {
                        mActiveCommands.Add(mCurrentCommand);
                        mCurrentCommand.SetActive(lContext);
                        mCurrentCommand = null;
                    }

                    return false;
                }

                private ZBackgroundSenderSendDataWorker()
                {

                }


                private void ZBackgroundSenderMoveNextCommandAndAppendTag(cTrace.cContext pParentContext)
                {
                    // gets the next command (if there is one) appends the tag part and returns true if it did

                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSenderMoveNextCommandAndAppendTag));

                    lock (mPipelineLock)
                    {
                        while (mQueuedCommands.Count > 0)
                        {
                            var lCommand = mQueuedCommands.Dequeue();

                            if (lCommand.State == eCommandState.queued)
                            {
                                if (lCommand.UIDValidity != null && lCommand.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.Cache.UIDValidity) lCommand.SetException(new cUIDValidityChangedException(lContext), lContext);
                                else
                                {
                                    mCurrentCommand = lCommand;
                                    lCommand.SetCurrent(lContext);
                                    break;
                                }
                            }
                        }
                    }

                    if (mCurrentCommand == null) return;

                    mSendBuffer.AddRange(mCurrentCommand.Tag);
                    mSendBuffer.Add(cASCII.SPACE);

                    mBufferStartPoints.Add(mTraceBuffer.Count);

                    mTraceBuffer.AddRange(mCurrentCommand.Tag);
                    mTraceBuffer.Add(cASCII.SPACE);
                }

                private bool ZBackgroundSenderAppendCurrentPartLiteralHeader()
                {
                    // if the current part is a literal, appends the literal header
                    //  if the current part is a synchronising literal, returns true

                    if (!(mCurrentCommand.CurrentPart() is cLiteralCommandPartBase lLiteral)) return false;
                    
                    if (lLiteral.Binary) mSendBuffer.Add(cASCII.TILDA);

                    cByteList lLengthBytes = cTools.IntToBytesReverse(lLiteral.Length);
                    lLengthBytes.Reverse();

                    mSendBuffer.Add(cASCII.LBRACE);
                    mSendBuffer.AddRange(lLengthBytes);

                    mTraceBuffer.Add(cASCII.LBRACE);
                    if (lLiteral.Secret) mTraceBuffer.Add(cASCII.NUL);
                    else mTraceBuffer.AddRange(lLengthBytes);

                    bool lSynchronising;

                    if (mLiteralPlus || mLiteralMinus && lLiteral.Length < 4097)
                    {
                        mSendBuffer.Add(cASCII.PLUS);
                        mTraceBuffer.Add(cASCII.PLUS);
                        lSynchronising = false;
                    }
                    else lSynchronising = true;

                    mSendBuffer.Add(cASCII.RBRACE);
                    mSendBuffer.Add(cASCII.CR);
                    mSendBuffer.Add(cASCII.LF);

                    mTraceBuffer.Add(cASCII.RBRACE);
                    mTraceBuffer.Add(cASCII.CR);
                    mTraceBuffer.Add(cASCII.LF);

                    return lSynchronising;
                }

                public async Task ZBackgroundSenderWriteAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSenderWriteAsync));

                    if (mSendBuffer.Count == 0) return;

                    lContext.TraceVerbose("sending {0}", mTraceBuffer);
                    mSynchroniser.InvokeNetworkActivity(mBufferStartPoints, mTraceBuffer, lContext);

                    await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);

                    mSendBuffer.Clear();
                    mBufferStartPoints.Clear();
                    mTraceBuffer.Clear();
                }

                private async Task ZIdleAsync(cIdleConfiguration pIdleConfiguration, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdleAsync));

                    if (!await ZIdleDelayAsync(pIdleConfiguration.StartDelay, lContext).ConfigureAwait(false)) return;

                    if (mIdleCommandSupported) await ZIdleIdleAsync(pIdleConfiguration.IdleRestartInterval, lContext).ConfigureAwait(false);
                    else await ZIdlePollAsync(pIdleConfiguration.PollInterval, lContext).ConfigureAwait(false);
                }

                private async Task<bool> ZIdleDelayAsync(int pDelay, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdleDelayAsync), pDelay);

                    if (pDelay == 0) return true;

                    using (cCountdownTimer lCountdownTimer = new cCountdownTimer(pDelay, lContext))
                    {
                        lContext.TraceVerbose("waiting");
                        ;?; // need a special version for here
                        if (await ZProcessResponsesAsync(lCountdownTimer.GetAwaitCountdownTask(), lContext).ConfigureAwait(false)) return true;
                        lContext.TraceVerbose("idle terminated during start delay");
                        return false;
                    }
                }

                private static readonly cBytes kIdle = new cBytes("IDLE");
                private static readonly cBytes kDone = new cBytes("DONE");

                private async Task ZIdleIdleAsync(int pIdleRestartInterval, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdleIdleAsync), pIdleRestartInterval);

                    using (cCountdownTimer lCountdownTimer = new cCountdownTimer(pIdleRestartInterval, lContext))
                    {
                        while (true)
                        {
                            Task lCountdownTask = lCountdownTimer.GetAwaitCountdownTask();

                            cCommandTag lTag = new cCommandTag();

                            mSendBuffer.Clear();

                            mSendBuffer.AddRange(lTag);
                            mSendBuffer.Add(cASCII.SPACE);
                            mSendBuffer.AddRange(kIdle);
                            mSendBuffer.Add(cASCII.CR);
                            mSendBuffer.Add(cASCII.LF);

                            lContext.TraceVerbose("sending {0}", mSendBuffer);
                            mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, mSendBuffer, lContext);
                            await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);

                            // wait for continuation
                            await ZIdleProcessResponsesAsync(lTag, true, lContext).ConfigureAwait(false);

                            // process responses until (normally) the countdown or backgroundreleaser are signalled
                            Task lCompleted = await ZIdleProcessResponsesAsync(lTag, false, lContext, lCountdownTask, mBackgroundReleaser.GetAwaitReleaseTask(lContext)).ConfigureAwait(false);

                            if (lCompleted == null) throw new cUnexpectedServerActionException(fCapabilities.idle, "idle completed before done sent", lContext);

                            mSendBuffer.Clear();

                            mSendBuffer.AddRange(kDone);
                            mSendBuffer.Add(cASCII.CR);
                            mSendBuffer.Add(cASCII.LF);

                            lContext.TraceVerbose("sending {0}", mSendBuffer);
                            mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, mSendBuffer, lContext);
                            await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);

                            // process responses until the command completion is received
                            await ZIdleProcessResponsesAsync(lTag, false, lContext).ConfigureAwait(false);

                            if (!ReferenceEquals(lCompleted, lCountdownTask)) return;

                            lCountdownTimer.Restart(lContext);
                        }
                    }
                }

                private static readonly cBytes kNoOp = new cBytes("NOOP");
                private static readonly cBytes kCheck = new cBytes("CHECK");

                private async Task ZIdlePollAsync(int pPollInterval, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdlePollAsync), pPollInterval);

                    using (cCountdownTimer lCountdownTimer = new cCountdownTimer(pPollInterval, lContext))
                    {
                        while (true)
                        {
                            if (mMailboxCache?.SelectedMailboxDetails != null)
                            {
                                await ZIdlePollCommandAsync(kCheck, lContext).ConfigureAwait(false);

                                if (mBackgroundReleaser.IsReleased(lContext))
                                {
                                    lContext.TraceVerbose("idle terminated during check");
                                    return;
                                }
                            }

                            await ZIdlePollCommandAsync(kNoOp, lContext).ConfigureAwait(false);

                            if (mBackgroundReleaser.IsReleased(lContext))
                            {
                                lContext.TraceVerbose("idle terminated during noop");
                                return;
                            }

                            lContext.TraceVerbose("waiting");

                            ;?; // need a special version for here

                            if (!await ZProcessResponsesAsync(lCountdownTimer.GetAwaitCountdownTask(), lContext).ConfigureAwait(false))
                            {
                                lContext.TraceVerbose("idle terminated during poll delay");
                                return;
                            }

                            lCountdownTimer.Restart(lContext);
                        }
                    }
                }

                private async Task ZIdlePollCommandAsync(cBytes pCommand, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdlePollCommandAsync), pCommand);

                    cCommandTag lTag = new cCommandTag();

                    mSendBuffer.Clear();

                    mSendBuffer.AddRange(lTag);
                    mSendBuffer.Add(cASCII.SPACE);
                    mSendBuffer.AddRange(pCommand);
                    mSendBuffer.Add(cASCII.CR);
                    mSendBuffer.Add(cASCII.LF);

                    lContext.TraceVerbose("sending {0}", mSendBuffer);
                    mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, mSendBuffer, lContext);
                    await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);

                    await ZIdleProcessResponsesAsync(lTag, false, lContext).ConfigureAwait(false);
                }

                private async Task<Task> ZIdleProcessResponsesAsync(cCommandTag pTag, bool pExpectContinuation, cTrace.cContext pParentContext, params Task[] pMonitorTasks)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdleProcessResponsesAsync));

                    while (true)
                    {
                        lContext.TraceVerbose("waiting");

                        Task lBuildResponseTask = mConnection.GetBuildResponseTask(lContext);
                        Task lCompleted = await mBackgroundAwaiter.AwaitAny(lBuildResponseTask, pMonitorTasks).ConfigureAwait(false);
                        if (!ReferenceEquals(lCompleted, lBuildResponseTask)) return lCompleted;

                        var lLines = mConnection.GetResponse(lContext);
                        mSynchroniser.InvokeNetworkActivity(lLines, lContext);
                        var lCursor = new cBytesCursor(lLines);

                        ;?;
                        if (pExpectContinuation && ZResponseIsContinuation(lCursor, null, lContext)) return null;

                        if (ZProcessData(lCursor, lContext)) continue;

                        var lResult = ZProcessCommandCompletion(lCursor, pTag, false, null, lContext);

                        if (lResult != null)
                        {
                            if (lResult.ResultType != eCommandResultType.ok) throw new cProtocolErrorException(lResult, fCapabilities.idle, lContext);
                            if (pExpectContinuation) throw new cUnexpectedServerActionException(fCapabilities.idle, "idle command completed before continuation received", lContext);
                            return null;
                        }

                        lContext.TraceError("unrecognised response: {0}", lLines);
                    }
                }

                private static readonly cBytes kAsterisk = new cBytes("*");
                private static readonly cBytes kSASLAuthenticationResponse = new cBytes("<SASL authentication response>");

                private async Task ZProcessChallengeAsync(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessChallengeAsync));

                    IList<byte> lResponse;

                    if (cBase64.TryDecode(pCursor.GetRestAsBytes(), out var lChallenge, out var lError))
                    {
                        try { lResponse = mCurrentCommand.GetAuthenticationResponse(lChallenge); }
                        catch (Exception e)
                        {
                            lContext.TraceException("SASL authentication object threw", e);
                            lResponse = null;
                        }
                    }
                    else
                    {
                        lContext.TraceError("Could not decode challenge: {0}", lError);
                        lResponse = null;
                    }

                    byte[] lBuffer;

                    if (lResponse == null)
                    {
                        lContext.TraceVerbose("sending cancellation");
                        lBuffer = new byte[] { cASCII.ASTERISK, cASCII.CR, cASCII.LF };
                        mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, kAsterisk, lContext);
                        mCurrentCommand.WaitingForContinuationRequest = false;
                    }
                    else
                    {
                        lContext.TraceVerbose("sending response");
                        cByteList lBytes = cBase64.Encode(lResponse);
                        lBytes.Add(cASCII.CR);
                        lBytes.Add(cASCII.LF);
                        lBuffer = lBytes.ToArray();
                        mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, kSASLAuthenticationResponse, lContext);
                    }

                    await mConnection.WriteAsync(mBackgroundMC, lBuffer, lContext).ConfigureAwait(false);
                }

                private static readonly cBytes kAsteriskSpace = new cBytes("* ");
                private static readonly cBytes kOKSpace = new cBytes("OK ");
                private static readonly cBytes kNoSpace = new cBytes("NO ");
                private static readonly cBytes kBadSpace = new cBytes("BAD ");
                private static readonly cBytes kByeSpace = new cBytes("BYE ");

                private bool ZProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessData));

                    if (!pCursor.SkipBytes(kAsteriskSpace)) return false;

                    lContext.TraceVerbose("got data");

                    if (pCursor.SkipBytes(kOKSpace))
                    {
                        lContext.TraceVerbose("got information");
                        mResponseTextProcessor.Process(eResponseTextType.information, pCursor, mActiveCommands, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kNoSpace))
                    {
                        lContext.TraceVerbose("got a warning");
                        mResponseTextProcessor.Process(eResponseTextType.warning, pCursor, mActiveCommands, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kBadSpace))
                    {
                        lContext.TraceVerbose("got a protocol error");
                        mResponseTextProcessor.Process(eResponseTextType.protocolerror, pCursor, mActiveCommands, lContext);
                        return true;
                    }

                    var lResult = eProcessDataResult.notprocessed;

                    if (pCursor.SkipBytes(kByeSpace))
                    {
                        lContext.TraceVerbose("got a bye");

                        cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextType.bye, pCursor, null, lContext);
                        cResponseDataBye lData = new cResponseDataBye(lResponseText);

                        foreach (var lCommand in mActiveCommands) ZProcessDataWorker(ref lResult, lCommand.Hook.ProcessData(lData, lContext), lContext);

                        if (lResult == eProcessDataResult.notprocessed)
                        {
                            lContext.TraceVerbose("got a unilateral bye");
                            throw new cUnilateralByeException(lResponseText, lContext);
                        }

                        return true;
                    }

                    var lBookmark = pCursor.Position;

                    foreach (var lParser in mResponseDataParsers)
                    {
                        if (lParser.Process(pCursor, out var lData, lContext))
                        {
                            if (mMailboxCache != null) ZProcessDataWorker(ref lResult, mMailboxCache.ProcessData(lData, lContext), lContext);
                            foreach (var lCommand in mActiveCommands) ZProcessDataWorker(ref lResult, lCommand.Hook.ProcessData(lData, lContext), lContext);
                            foreach (var lDataProcessor in mUnsolicitedDataProcessors) ZProcessDataWorker(ref lResult, lDataProcessor.ProcessData(lData, lContext), lContext);

                            if (lResult == eProcessDataResult.notprocessed) lContext.TraceWarning("unprocessed data response: {0}", lData);

                            return true;
                        }

                        pCursor.Position = lBookmark;
                    }

                    if (mMailboxCache != null)
                    {
                        ZProcessDataWorker(ref lResult, mMailboxCache.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    foreach (var lCommand in mActiveCommands)
                    {
                        ZProcessDataWorker(ref lResult, lCommand.Hook.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    foreach (var lDataProcessor in mUnsolicitedDataProcessors)
                    {
                        ZProcessDataWorker(ref lResult, lDataProcessor.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    if (lResult == eProcessDataResult.notprocessed) lContext.TraceWarning("unrecognised data response: {0}", pCursor);

                    return true;
                }

                private void ZProcessDataWorker(ref eProcessDataResult pResult, eProcessDataResult pProcessDataResult, cTrace.cContext pContext)
                {
                    if (pProcessDataResult == eProcessDataResult.processed)
                    {
                        if (pResult != eProcessDataResult.notprocessed) throw new cPipelineConflictException(pContext);
                        pResult = eProcessDataResult.processed;
                    }
                    else if (pProcessDataResult == eProcessDataResult.observed)
                    {
                        if (pResult == eProcessDataResult.processed) throw new cPipelineConflictException(pContext);
                        pResult = eProcessDataResult.observed;
                    }
                }

                private bool ZProcessActiveCommandCompletion(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessActiveCommandCompletion));

                    for (int i = 0; i < mActiveCommands.Count; i++)
                    {
                        var lCommand = mActiveCommands[i];

                        if (ZProcessCommandCompletion(pCursor, lCommand, lContext))
                        {
                            mActiveCommands.RemoveAt(i);
                            return true;
                        }
                    }

                    return false;
                }

                private cCommandResult ZProcessCommandCompletion(cBytesCursor pCursor, cCommandTag pTag, bool pIsAuthentication, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessCommandCompletion), pTag);

                    var lBookmark = pCursor.Position;

                    if (!pCursor.SkipBytes(pTag, true)) return null;

                    if (!pCursor.SkipByte(cASCII.SPACE))
                    {
                        lContext.TraceWarning("likely badly formed command completion: {0}", pCursor);
                        pCursor.Position = lBookmark;
                        return null;
                    }

                    eCommandResultType lResultType;
                    eResponseTextType lTextType;

                    if (pCursor.SkipBytes(kOKSpace))
                    {
                        lResultType = eCommandResultType.ok;
                        lTextType = eResponseTextType.success;
                    }
                    else if (pCursor.SkipBytes(kNoSpace))
                    {
                        lResultType = eCommandResultType.no;
                        lTextType = eResponseTextType.failure;
                    }
                    else if (pCursor.SkipBytes(kBadSpace))
                    {
                        lResultType = eCommandResultType.bad;
                        if (pIsAuthentication) lTextType = eResponseTextType.authenticationcancelled; 
                        else lTextType = eResponseTextType.error;
                    }
                    else
                    {
                        lContext.TraceWarning("likely badly formed command completion: {0}", pCursor);
                        pCursor.Position = lBookmark;
                        return null;
                    }

                    var lResult = new cCommandResult(lResultType, mResponseTextProcessor.Process(lTextType, pCursor, pTextCodeProcessor, lContext));

                    if (mMailboxCache != null) mMailboxCache.CommandCompletion(lContext);

                    return lResult;
                }

                private bool ZProcessCommandCompletion(cBytesCursor pCursor, cCommand pCommand, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessCommandCompletion), pCommand);

                    var lResult = ZProcessCommandCompletion(pCursor, pCommand.Tag, pCommand.IsAuthentication, pCommand.Hook, lContext);
                    if (lResult == null) return false;

                    // check the references

                    if (pCommand.UIDValidity != null && pCommand.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.Cache.UIDValidity) pCommand.SetException(new cUIDValidityChangedException(lContext), lContext);
                    else pCommand.SetResult(lResult, lContext);

                    return true;
                }

                public void Dispose()
                {
                    if (mDisposed) return;

                    if (mBackgroundCancellationTokenSource != null && !mBackgroundCancellationTokenSource.IsCancellationRequested)
                    {
                        try { mBackgroundCancellationTokenSource.Cancel(); }
                        catch { }
                    }

                    // must dispose first as the background task uses the other objects to be disposed
                    if (mBackgroundTask != null)
                    {
                        // wait for the task to exit before disposing it
                        try { mBackgroundTask.Wait(); }
                        catch { }

                        try { mBackgroundTask.Dispose(); }
                        catch { }
                    }

                    if (mIdleBlock != null)
                    {
                        try { mIdleBlock.Dispose(); }
                        catch { }
                    }

                    if (mBackgroundReleaser != null)
                    {
                        try { mBackgroundReleaser.Dispose(); }
                        catch { }
                    }

                    if (mBackgroundAwaiter != null)
                    {
                        try { mBackgroundAwaiter.Dispose(); }
                        catch { }
                    }

                    if (mBackgroundCancellationTokenSource != null)
                    {
                        try { mBackgroundCancellationTokenSource.Dispose(); }
                        catch { }
                    }

                    if (mConnection != null)
                    {
                        try { mConnection.Dispose(); }
                        catch { }
                    }

                    mDisposed = true;
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(_Tests));
                    cConnection._Tests(lContext);
                }
            }
        }
    }
}
