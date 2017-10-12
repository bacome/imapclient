using System;
using System.Collections.Generic;
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
                // for tracing
                private static readonly int[] kBufferStartPointsBeginning = new int[1] { 0 };

                private bool mDisposed = false;
                private bool mStopped = false;

                // stuff
                private readonly cCallbackSynchroniser mSynchroniser;
                private readonly cConnection mConnection;
                private readonly cResponseTextProcessor mResponseTextProcessor;
                private cIdleConfiguration mIdleConfiguration;
                private readonly Action<cTrace.cContext> mDisconnect;

                // used to control idling
                private readonly cExclusiveAccess mIdleBlock = new cExclusiveAccess("idleblock", 100); 

                // installable components
                private readonly List<iResponseDataParser> mResponseDataParsers = new List<iResponseDataParser>();
                private readonly List<cUnsolicitedDataProcessor> mUnsolicitedDataProcessors = new List<cUnsolicitedDataProcessor>();

                // background task objects
                private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource(); 
                private readonly cMethodControl mBackgroundMC;
                private readonly cReleaser mBackgroundReleaser;
                private readonly cAwaiter mBackgroundAwaiter;
                private readonly Task mBackgroundTask; // background task
                private Exception mBackgroundTaskException = null;

                // set on enable
                private cMailboxCache mMailboxCache = null;
                private bool mLiteralPlus = false;
                private bool mLiteralMinus = false;
                private bool mIdleCommandSupported = false;

                // non-null when authenticating
                private cAuthenticateState mAuthenticateState = null; 

                // commands
                private readonly object mPipelineLock = new object(); // access to commands is protected by locking this
                private readonly Queue<cCommand> mQueuedCommands = new Queue<cCommand>();
                private cCommand mCurrentCommand = null;
                private readonly cActiveCommands mActiveCommands = new cActiveCommands();

                // growable buffers
                private readonly cByteList mSendBuffer = new cByteList();
                private readonly List<int> mBufferStartPoints = new List<int>();
                private readonly cByteList mTraceBuffer = new cByteList();

                public cCommandPipeline(cCallbackSynchroniser pSynchroniser, cConnection pConnection, cResponseTextProcessor pResponseTextProcessor, cIdleConfiguration pIdleConfiguration, Action<cTrace.cContext> pDisconnect, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cCommandPipeline), pIdleConfiguration);

                    mSynchroniser = pSynchroniser;
                    mConnection = pConnection;
                    mResponseTextProcessor = pResponseTextProcessor;
                    mIdleConfiguration = pIdleConfiguration;
                    mDisconnect = pDisconnect;

                    mBackgroundMC = new cMethodControl(System.Threading.Timeout.Infinite, mBackgroundCancellationTokenSource.Token);
                    mBackgroundReleaser = new cReleaser("commandpipeline_background", mBackgroundCancellationTokenSource.Token);
                    mBackgroundAwaiter = new cAwaiter(mBackgroundCancellationTokenSource.Token);
                    mBackgroundTask = ZBackgroundTaskAsync(lContext);

                    mIdleBlock.Released += mBackgroundReleaser.Release; // when the idle block is removed, kick the background process
                }

                public void Install(iResponseDataParser pResponseDataParser) => mResponseDataParsers.Add(pResponseDataParser);
                public void Install(cUnsolicitedDataProcessor pUnsolicitedDataProcessor) => mUnsolicitedDataProcessors.Add(pUnsolicitedDataProcessor);

                public void SetCapability(cCapabilities pCapabilities, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(SetCapability));
                    if (mMailboxCache != null) throw new InvalidOperationException();
                    if (pCapabilities == null) throw new ArgumentNullException(nameof(pCapabilities));
                    mLiteralPlus = pCapabilities.LiteralPlus;
                    mLiteralMinus = pCapabilities.LiteralMinus;
                }

                public void Enable(cMailboxCache pMailboxCache, cCapabilities pCapabilities, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(Enable));

                    if (mMailboxCache != null) throw new InvalidOperationException();

                    if (pMailboxCache == null) throw new ArgumentNullException(nameof(pMailboxCache));
                    if (pCapabilities == null) throw new ArgumentNullException(nameof(pCapabilities));

                    mMailboxCache = pMailboxCache;

                    mLiteralPlus = pCapabilities.LiteralPlus;
                    mLiteralMinus = pCapabilities.LiteralMinus;
                    mIdleCommandSupported = pCapabilities.Idle;

                    mBackgroundReleaser.Release(lContext); // to allow idle to start
                }

                public void SetIdleConfiguration(cIdleConfiguration pConfiguration, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(SetIdleConfiguration), pConfiguration);
                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    mIdleConfiguration = pConfiguration;
                    mBackgroundReleaser.Release(lContext);
                }

                public async Task<cExclusiveAccess.cToken> GetIdleBlockTokenAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(GetIdleBlockTokenAsync));
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

                    cCommand lCommand;

                    lock (mPipelineLock)
                    {
                        if (mStopped)
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

                public void Stop(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(Stop));
                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mStopped) return;
                    mBackgroundCancellationTokenSource.Cancel();

                    // note: the following line has caused problems
                    //  the issue is that if the thread that runs it is the one that is currently running the background task then this will lock up (as it then waits for itself to exit)
                    //   this can happen if a commandhook calls this directly
                    //    (the change I did to discover the problem was to move the "cSession.Disconnect" from the "LogoutAsync" to the "cLogoutCommandHook")
                    //    (I did this because the problem with not doing it on the commandhook was that the pipeline was going back to waiting for responses and then the server closed the connection on it,
                    //      causing it to complain that something unexpected had happened - now there is an explicit check in the backgroundtask loop on the cancellationtokensource)
                    //
                    //  I worry that this points to a possible general problem with the task architecture I have used. It is possible that the command pipeline may have been better on a dedicated thread
                    //   (then I could check before the wait if the current thread was the pipeline thread and not do the wait) (the explicit check would still be required in the backgroundtask loop)
                    //
                    //  At this stage my advice to myself is: don't call this directly from a commandhook
                    //
                    mBackgroundTask.Wait();
                }

                private async Task ZBackgroundTaskAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewRootMethod(nameof(cCommandPipeline), nameof(ZBackgroundTaskAsync));

                    try
                    {
                        while (true)
                        {
                            mBackgroundReleaser.Reset(lContext);

                            await ZSendAsync(lContext).ConfigureAwait(false);

                            if (mCurrentCommand != null) await ZProcessResponsesAsync(lContext).ConfigureAwait(false);
                            else if (mActiveCommands.Count != 0) await ZProcessResponsesAsync(lContext, mBackgroundReleaser.GetAwaitReleaseTask(lContext)).ConfigureAwait(false);
                            else
                            {
                                var lIdleConfiguration = mIdleConfiguration;

                                cExclusiveAccess.cBlock lIdleBlockBlock = null;

                                if (mMailboxCache != null && lIdleConfiguration != null) lIdleBlockBlock = mIdleBlock.TryGetBlock(lContext);

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

                            if (mBackgroundCancellationTokenSource.IsCancellationRequested) throw new OperationCanceledException();
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

                    lock (mPipelineLock)
                    {
                        mStopped = true;
                    }

                    foreach (var lCommand in mActiveCommands) lCommand.SetException(mBackgroundTaskException, lContext);
                    if (mCurrentCommand != null) mCurrentCommand.SetException(mBackgroundTaskException, lContext);
                    foreach (var lCommand in mQueuedCommands) lCommand.SetException(mBackgroundTaskException, lContext);

                    mDisconnect(lContext);
                }

                private async Task ZSendAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZSendAsync));

                    mSendBuffer.Clear();
                    mBufferStartPoints.Clear();
                    mTraceBuffer.Clear();

                    if (mCurrentCommand != null)
                    {
                        mBufferStartPoints.Add(0);
                        ZSendAppendCurrentPartDataAndMoveNextPart(lContext);
                    }

                    while (true)
                    {
                        if (mCurrentCommand == null)
                        {
                            ZSendMoveNextCommandAndAppendTag(lContext);
                            if (mCurrentCommand == null) break;
                        }

                        if (ZSendAppendCurrentPartLiteralHeader()) break; // have to wait for continuation before sending the data

                        if (ZSendAppendCurrentPartDataAndMoveNextPart(lContext))
                        {
                            // this is authentication - have to wait for a challenge before sending more
                            mAuthenticateState = new cAuthenticateState();
                            break; 
                        }
                    }

                    if (mSendBuffer.Count == 0) lContext.TraceVerbose("nothing to send");
                    else
                    {
                        lContext.TraceVerbose("sending {0}", mTraceBuffer);
                        mSynchroniser.InvokeNetworkActivity(mBufferStartPoints, mTraceBuffer, lContext);
                        await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);
                    }
                }

                private bool ZSendAppendCurrentPartDataAndMoveNextPart(cTrace.cContext pParentContext)
                {
                    // appends the current part of the current command to the buffer to send
                    //  if that was the last part of the current command
                    //   if this is authentication true is returned
                    //   else the command gets added to the active commands and the current command is nulled

                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZSendAppendCurrentPartDataAndMoveNextPart));

                    var lPart = mCurrentCommand.CurrentPart();

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

                private void ZSendMoveNextCommandAndAppendTag(cTrace.cContext pParentContext)
                {
                    // gets the next command (if there is one) appends the tag part and returns true if it did

                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZSendMoveNextCommandAndAppendTag));

                    lock (mPipelineLock)
                    {
                        while (mQueuedCommands.Count > 0)
                        {
                            var lCommand = mQueuedCommands.Dequeue();

                            if (lCommand.State == eCommandState.queued)
                            {
                                // check the references 


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

                private bool ZSendAppendCurrentPartLiteralHeader()
                {
                    // if the current part is a literal, appends the literal header
                    //  if the current part is a synchronising literal, returns true

                    var lPart = mCurrentCommand.CurrentPart();

                    if (lPart.Type == eCommandPartType.literal8) mSendBuffer.Add(cASCII.TILDA);
                    else if (lPart.Type != eCommandPartType.literal) return false;

                    mSendBuffer.Add(cASCII.LBRACE);
                    mSendBuffer.AddRange(lPart.LiteralLengthBytes);

                    mTraceBuffer.Add(cASCII.LBRACE);
                    if (lPart.Secret) mTraceBuffer.Add(cASCII.NUL);
                    else mTraceBuffer.AddRange(lPart.LiteralLengthBytes);

                    bool lSynchronising;

                    if (mLiteralPlus || mLiteralMinus && lPart.Bytes.Count < 4097)
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

                private async Task<Task> ZProcessResponsesAsync(cTrace.cContext pParentContext, params Task[] pMonitorTasks)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessResponsesAsync));

                    while (true)
                    {
                        lContext.TraceVerbose("waiting");

                        Task lBuildResponseTask = mConnection.GetBuildResponseTask(lContext);
                        Task lCompleted = await mBackgroundAwaiter.AwaitAny(lBuildResponseTask, pMonitorTasks).ConfigureAwait(false);
                        if (!ReferenceEquals(lCompleted, lBuildResponseTask)) return lCompleted;

                        var lLines = mConnection.GetResponse(lContext);
                        mSynchroniser.InvokeNetworkActivity(lLines, lContext);
                        var lCursor = new cBytesCursor(lLines);

                        if (mCurrentCommand != null)
                        {
                            if (mAuthenticateState == null)
                            {
                                if (ZResponseIsContinuation(lCursor, mCurrentCommand.Hook, lContext)) return null;
                            }
                            else
                            {
                                if (await ZResponseIsChallengeAsync(lCursor, lContext).ConfigureAwait(false)) continue;
                            }
                        }

                        if (ZProcessData(lCursor, lContext)) continue;

                        if (ZProcessActiveCommandCompletion(lCursor, lContext))
                        {
                            if (mCurrentCommand == null && mActiveCommands.Count == 0)
                            {
                                lContext.TraceVerbose("there are no more commands to process responses for");
                                return null;
                            }

                            continue;
                        }

                        if (mCurrentCommand != null)
                        {
                            if (ZProcessCommandCompletion(lCursor, mCurrentCommand, lContext))
                            {
                                mAuthenticateState = null;
                                mCurrentCommand = null;
                                return null;
                            }
                        }

                        lContext.TraceError("unrecognised response: {0}", lLines);
                    }
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
                        Task lCountdownTask = lCountdownTimer.GetAwaitCountdownTask();

                        lContext.TraceVerbose("waiting");
                        Task lCompleted = await ZProcessResponsesAsync(lContext, lCountdownTask, mBackgroundReleaser.GetAwaitReleaseTask(lContext)).ConfigureAwait(false);

                        if (ReferenceEquals(lCompleted, lCountdownTask)) return true;

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

                            Task lCountdownTask = lCountdownTimer.GetAwaitCountdownTask();

                            lContext.TraceVerbose("waiting");
                            Task lCompleted = await ZProcessResponsesAsync(lContext, lCountdownTask, mBackgroundReleaser.GetAwaitReleaseTask(lContext)).ConfigureAwait(false);

                            if (!ReferenceEquals(lCompleted, lCountdownTask))
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

                        if (pExpectContinuation && ZResponseIsContinuation(lCursor, null, lContext)) return null;

                        if (ZProcessData(lCursor, lContext)) continue;

                        var lResult = ZProcessCommandCompletion(lCursor, pTag, null, lContext);

                        if (lResult != null)
                        {
                            if (lResult.ResultType != eCommandResultType.ok) throw new cProtocolErrorException(lResult, fCapabilities.idle, lContext);
                            if (pExpectContinuation) throw new cUnexpectedServerActionException(fCapabilities.idle, "idle command completed before continuation received", lContext);
                            return null;
                        }

                        lContext.TraceError("unrecognised response: {0}", lLines);
                    }
                }

                private static readonly cBytes kPlusSpace = new cBytes("+ ");

                private bool ZResponseIsContinuation(cBytesCursor pCursor, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZResponseIsContinuation));
                    if (!pCursor.SkipBytes(kPlusSpace)) return false;
                    lContext.TraceVerbose("got a continuation");
                    mResponseTextProcessor.Process(eResponseTextType.continuerequest, pCursor, pTextCodeProcessor, lContext);
                    return true;
                }

                private static readonly cBytes kAsterisk = new cBytes("*");
                private static readonly cBytes kSASLAuthenticationResponse = new cBytes("<SASL authentication response>");

                private async Task<bool> ZResponseIsChallengeAsync(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZResponseIsChallengeAsync));

                    if (!pCursor.SkipBytes(kPlusSpace)) return false;

                    lContext.TraceVerbose("got a challenge");

                    if (mAuthenticateState.CancelSent) throw new cUnexpectedServerActionException(0, "authentication cancellation sent but subsequent server challenge received", lContext);

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
                        mAuthenticateState.CancelSent = true;
                        mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, kAsterisk, lContext);
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

                    return true;
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

                    var lBookmark = pCursor.Position;
                    var lResult = eProcessDataResult.notprocessed;

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

                    if (lResult == eProcessDataResult.notprocessed)
                    {
                        if (pCursor.SkipBytes(kByeSpace))
                        {
                            lContext.TraceVerbose("got a unilateral bye");
                            cResponseText lResponseText = mResponseTextProcessor.Process(eResponseTextType.bye, pCursor, null, lContext);
                            mConnection.Disconnect(lContext);
                            throw new cUnilateralByeException(lResponseText, lContext);
                        }

                        lContext.TraceWarning("unrecognised data response: {0}", pCursor);
                    }

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

                private cCommandResult ZProcessCommandCompletion(cBytesCursor pCursor, cCommandTag pTag, iTextCodeProcessor pTextCodeProcessor, cTrace.cContext pParentContext)
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
                        if (mAuthenticateState == null) lTextType = eResponseTextType.error;
                        else lTextType = eResponseTextType.authenticationcancelled;
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

                    var lResult = ZProcessCommandCompletion(pCursor, pCommand.Tag, pCommand.Hook, lContext);
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

                    mDisposed = true;
                }
            }
        }
    }
}
