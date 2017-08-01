using System;
using System.Collections.Concurrent;
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
                private bool mDisposed = false;
                
                private readonly cConnection mConnection;
                private readonly cResponseTextProcessor mResponseTextProcessor;
                private cIdleConfiguration mIdleConfiguration;
                private readonly Action<cTrace.cContext> mDisconnect;
                private readonly List<cResponseDataParser> mResponseDataParsers = new List<cResponseDataParser>();
                private readonly List<cUnsolicitedDataProcessor> mUnsolicitedDataProcessors = new List<cUnsolicitedDataProcessor>();
                private cMailboxCache mMailboxCache = null;
                private bool mLiteralPlus = false; // based on capability
                private bool mLiteralMinus = false; // based on capability
                private bool mIdleCommandSupported = false; // based on capability
                private readonly object mCommandQueueLock = new object();
                private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource(); // for use when stopping the background task
                private readonly cExclusiveAccess mIdleBlock = new cExclusiveAccess("idleblock", 100);
                private readonly cMethodControl mBackgroundMC;
                private readonly cReleaser mBackgroundReleaser;
                private readonly cTerminator mBackgroundTerminator;
                private readonly Task mBackgroundTask; // background task
                private cAuthenticateState mAuthenticateState = null; // non-null when authenticating
                private readonly ConcurrentQueue<cItem> mItemQueue = new ConcurrentQueue<cItem>();
                private cByteList mSendBuffer = new cByteList();
                private cByteList mTraceBuffer = new cByteList();
                private cCurrentItem mCurrentItem = null;
                private cActiveItems mActiveItems = new cActiveItems();

                public cCommandPipeline(cConnection pConnection, cResponseTextProcessor pResponseTextProcessor, cIdleConfiguration pIdleConfiguration, Action<cTrace.cContext> pDisconnect, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cCommandPipeline), pIdleConfiguration);
                    mConnection = pConnection;
                    mResponseTextProcessor = pResponseTextProcessor;
                    mIdleConfiguration = pIdleConfiguration;
                    mDisconnect = pDisconnect;
                    mIdleBlock.Released += ZIdleBlockReleased;
                    mBackgroundMC = new cMethodControl(System.Threading.Timeout.Infinite, mCancellationTokenSource.Token);
                    mBackgroundReleaser = new cReleaser("commandpipeline_background", mCancellationTokenSource.Token);
                    mBackgroundTerminator = new cTerminator(mCancellationTokenSource.Token);
                    mBackgroundTask = ZBackgroundTaskAsync(lContext);
                }

                public void Install(cResponseDataParser pResponseDataParser) => mResponseDataParsers.Add(pResponseDataParser);
                public void Install(cUnsolicitedDataProcessor pUnsolicitedDataProcessor) => mUnsolicitedDataProcessors.Add(pUnsolicitedDataProcessor);

                public void Go(cMailboxCache pMailboxCache, cCapability pCapability, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(Go));

                    if (mMailboxCache != null) throw new InvalidOperationException();

                    mMailboxCache = pMailboxCache ?? throw new ArgumentNullException(nameof(pMailboxCache));

                    mLiteralPlus = pCapability.LiteralPlus;
                    mLiteralMinus = pCapability.LiteralMinus;
                    mIdleCommandSupported = pCapability.Idle;

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

                public async Task<cCommandResult> ExecuteAsync(cMethodControl pMC, cCommand pCommand, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ExecuteAsync), pMC, pCommand);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    if (mCancellationTokenSource.IsCancellationRequested) throw new cPipelineStoppedException(lContext);

                    using (var lItem = new cItem(pCommand, mCommandQueueLock))
                    {
                        pCommand.SetEnqueued();
                        mItemQueue.Enqueue(lItem);

                        mBackgroundReleaser.Release(lContext);

                        using (var lCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(pMC.CancellationToken, mCancellationTokenSource.Token))
                        {
                            try { return await lItem.WaitAsync(pMC.Timeout, lCancellationTokenSource.Token, lContext).ConfigureAwait(false); }
                            catch (OperationCanceledException) when (mCancellationTokenSource.IsCancellationRequested) { throw new cPipelineStoppedException(lContext); }
                        }
                    }
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

                            if (mCurrentItem != null) await ZProcessResponsesAsync(lContext).ConfigureAwait(false);
                            else if (mActiveItems.Count != 0) await ZProcessResponsesAsync(lContext, mBackgroundReleaser.GetAwaitReleaseTask(lContext)).ConfigureAwait(false);
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
                        }
                    }
                    catch (Exception e) when (!mCancellationTokenSource.IsCancellationRequested)
                    {
                        lContext.TraceException("the pipeline is stopping due to an exception", e);
                        Stop(lContext);
                    }
                }

                private async Task ZSendAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZSendAsync));

                    mSendBuffer.Clear();
                    mTraceBuffer.Clear();

                    if (mCurrentItem != null) ZSendAppendCurrentPartDataAndMoveNextPart();

                    while (true)
                    {
                        if (mCurrentItem == null)
                        {
                            ZSendMoveNextCommandAndAppendTag(lContext);
                            if (mCurrentItem == null) break;
                        }

                        if (ZSendAppendCurrentPartLiteralHeader()) break; // have to wait for continuation before sending the data

                        if (ZSendAppendCurrentPartDataAndMoveNextPart())
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
                        await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);
                    }
                }

                private bool ZSendAppendCurrentPartDataAndMoveNextPart()
                {
                    // appends the current part of the current command to the buffer to send
                    //  if that was the last part of the current command
                    //   if this is authentication true is returned
                    //   else the command gets added to the active commands and the current command is nulled

                    var lPart = mCurrentItem.CurrentPart;

                    mSendBuffer.AddRange(lPart.Bytes);

                    if (lPart.Secret) mTraceBuffer.Add(cASCII.NUL);
                    else mTraceBuffer.AddRange(lPart.Bytes);

                    if (mCurrentItem.MoveNext()) return false;

                    mSendBuffer.Add(cASCII.CR);
                    mSendBuffer.Add(cASCII.LF);

                    mTraceBuffer.Add(cASCII.CR);
                    mTraceBuffer.Add(cASCII.LF);

                    if (mCurrentItem.Item.Authentication != null) return true;

                    // the current command can be added to the list of active commands now
                    lock (mCommandQueueLock)
                    {
                        mActiveItems.Add(mCurrentItem.Item);
                        mCurrentItem = null;
                    }

                    return false;
                }

                private void ZSendMoveNextCommandAndAppendTag(cTrace.cContext pParentContext)
                {
                    // gets the next command (if there is one) appends the tag part and returns true if it did

                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZSendMoveNextCommandAndAppendTag));

                    lock (mCommandQueueLock)
                    {
                        while (true)
                        {
                            if (!mItemQueue.TryDequeue(out var lItem)) break;

                            if (!lItem.WaitOver)
                            {
                                lItem.SetStarted(lContext); // mark the command as started

                                if (lItem.UIDValidity != null && lItem.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.Cache.UIDValidity) // don't run the command if it requires a specific uidvalidity and that isn't the current one
                                {
                                    lItem.SetException(new cUIDValidityChangedException(lContext), lContext);
                                }
                                else
                                {
                                    // found a command to run
                                    mCurrentItem = new cCurrentItem(lItem);
                                    break;
                                }
                            }
                        }
                    }

                    if (mCurrentItem == null) return;

                    mSendBuffer.AddRange(mCurrentItem.Item.Tag);
                    mSendBuffer.Add(cASCII.SPACE);

                    mTraceBuffer.AddRange(mCurrentItem.Item.Tag);
                    mTraceBuffer.Add(cASCII.SPACE);
                }

                private bool ZSendAppendCurrentPartLiteralHeader()
                {
                    // if the current part is a literal, appends the literal header
                    //  if the current part is a synchronising literal, returns true

                    var lPart = mCurrentItem.CurrentPart;

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

                        Task lAwaitResponseTask = mConnection.GetAwaitResponseTask(lContext);
                        Task lCompleted = await mBackgroundTerminator.WhenAny(lAwaitResponseTask, pMonitorTasks).ConfigureAwait(false);
                        if (!ReferenceEquals(lCompleted, lAwaitResponseTask)) return lCompleted;

                        var lLines = mConnection.GetResponse(lContext);
                        var lCursor = new cBytesCursor(lLines);

                        if (mCurrentItem != null)
                        {
                            if (mAuthenticateState == null)
                            {
                                if (ZResponseIsContinuation(lCursor, mCurrentItem.Item, lContext)) return null;
                            }
                            else
                            {
                                if (await ZResponseIsChallengeAsync(lCursor, lContext).ConfigureAwait(false)) continue;
                            }
                        }

                        if (ZProcessData(lCursor, lContext)) continue;

                        if (ZProcessActiveCommandCompletion(lCursor, lContext))
                        {
                            if (mCurrentItem == null && mActiveItems.Count == 0)
                            {
                                lContext.TraceVerbose("there are no more commands to process responses for");
                                return null;
                            }

                            continue;
                        }

                        if (mCurrentItem != null)
                        {
                            if (ZProcessCommandCompletion(lCursor, mCurrentItem.Item, lContext))
                            {
                                mAuthenticateState = null;
                                mCurrentItem = null;
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
                            await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);

                            // wait for continuation
                            await ZIdleProcessResponsesAsync(lTag, true, lContext).ConfigureAwait(false);

                            // process responses until (normally) the countdown or backgroundreleaser are signalled
                            Task lCompleted = await ZIdleProcessResponsesAsync(lTag, false, lContext, lCountdownTask, mBackgroundReleaser.GetAwaitReleaseTask(lContext)).ConfigureAwait(false);

                            if (lCompleted == null) throw new cUnexpectedServerActionException(fCapabilities.Idle, "idle completed before done sent", lContext);

                            mSendBuffer.Clear();

                            mSendBuffer.AddRange(kDone);
                            mSendBuffer.Add(cASCII.CR);
                            mSendBuffer.Add(cASCII.LF);

                            lContext.TraceVerbose("sending {0}", mSendBuffer);
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
                    await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);

                    await ZIdleProcessResponsesAsync(lTag, false, lContext).ConfigureAwait(false);
                }

                private async Task<Task> ZIdleProcessResponsesAsync(cCommandTag pTag, bool pExpectContinuation, cTrace.cContext pParentContext, params Task[] pMonitorTasks)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdleProcessResponsesAsync));

                    while (true)
                    {
                        lContext.TraceVerbose("waiting");

                        Task lAwaitResponseTask = mConnection.GetAwaitResponseTask(lContext);
                        Task lCompleted = await mBackgroundTerminator.WhenAny(lAwaitResponseTask, pMonitorTasks).ConfigureAwait(false);
                        if (!ReferenceEquals(lCompleted, lAwaitResponseTask)) return lCompleted;

                        var lLines = mConnection.GetResponse(lContext);
                        var lCursor = new cBytesCursor(lLines);

                        if (pExpectContinuation && ZResponseIsContinuation(lCursor, null, lContext)) return null;

                        if (ZProcessData(lCursor, lContext)) continue;

                        var lResult = ZProcessCommandCompletion(lCursor, pTag, null, lContext);

                        if (lResult != null)
                        {
                            if (lResult.ResultType != eCommandResultType.ok) throw new cProtocolErrorException(lResult, fCapabilities.Idle, lContext);
                            if (pExpectContinuation) throw new cUnexpectedServerActionException(fCapabilities.Idle, "idle command completed before continuation received", lContext);
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
                    mResponseTextProcessor.Process(pCursor, eResponseTextType.continuerequest, pTextCodeProcessor, lContext);
                    return true;
                }

                private async Task<bool> ZResponseIsChallengeAsync(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZResponseIsChallengeAsync));

                    if (!pCursor.SkipBytes(kPlusSpace)) return false;

                    lContext.TraceVerbose("got a challenge");

                    if (mAuthenticateState.CancelSent) throw new cUnexpectedServerActionException(0, "authentication cancellation sent but subsequent server challenge received", lContext);

                    IList<byte> lResponse;

                    if (cBase64.TryDecode(pCursor.GetRestAsBytes(), out var lChallenge, out var lError))
                    {
                        try { lResponse = mCurrentItem.Item.Authentication.GetResponse(lChallenge); }
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
                    }
                    else
                    {
                        lContext.TraceVerbose("sending response");
                        cByteList lBytes = cBase64.Encode(lResponse);
                        lBytes.Add(cASCII.CR);
                        lBytes.Add(cASCII.LF);
                        lBuffer = lBytes.ToArray();
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
                        mResponseTextProcessor.Process(pCursor, eResponseTextType.information, mActiveItems, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kNoSpace))
                    {
                        lContext.TraceVerbose("got a warning");
                        mResponseTextProcessor.Process(pCursor, eResponseTextType.warning, mActiveItems, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kBadSpace))
                    {
                        lContext.TraceVerbose("got a protocol error");
                        mResponseTextProcessor.Process(pCursor, eResponseTextType.protocolerror, mActiveItems, lContext);
                        return true;
                    }

                    var lBookmark = pCursor.Position;

                    bool lParsed = false;
                    cResponseData lData = null;

                    foreach (var lParser in mResponseDataParsers)
                    {
                        if (lParser.Process(pCursor, out lData, lContext))
                        {
                            lParsed = true;
                            break;
                        }

                        pCursor.Position = lBookmark;
                    }

                    var lResult = eProcessDataResult.notprocessed;

                    if (mMailboxCache != null)
                    {
                        if (lParsed) ZProcessDataWorker(ref lResult, mMailboxCache.ProcessData(lData, lContext), lContext);
                        else ZProcessDataWorker(ref lResult, mMailboxCache.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    foreach (var lItem in mActiveItems)
                    {
                        if (lParsed) ZProcessDataWorker(ref lResult, lItem.ProcessData(lData, lContext), lContext);
                        else ZProcessDataWorker(ref lResult, lItem.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    foreach (var lDataProcessor in mUnsolicitedDataProcessors)
                    {
                        if (lParsed) ZProcessDataWorker(ref lResult, lDataProcessor.ProcessData(lData, lContext), lContext);
                        else ZProcessDataWorker(ref lResult, lDataProcessor.ProcessData(pCursor, lContext), lContext);
                        pCursor.Position = lBookmark;
                    }

                    if (lResult == eProcessDataResult.notprocessed)
                    {
                        if (pCursor.SkipBytes(kByeSpace))
                        {
                            lContext.TraceVerbose("got a unilateral bye");
                            cResponseText lResponseText = mResponseTextProcessor.Process(pCursor, eResponseTextType.bye, null, lContext);
                            mConnection.Disconnect(lContext);
                            mDisconnect(lContext);
                            throw new cByeException(lResponseText, lContext);
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

                    for (int i = 0; i < mActiveItems.Count; i++)
                    {
                        cItem lItem = mActiveItems[i];

                        if (ZProcessCommandCompletion(pCursor, lItem, lContext))
                        {
                            mActiveItems.RemoveAt(i);
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

                    var lResult = new cCommandResult(lResultType, mResponseTextProcessor.Process(pCursor, lTextType, pTextCodeProcessor, lContext));

                    if (mMailboxCache != null) mMailboxCache.CommandCompletion(lContext);

                    return lResult;
                }

                private bool ZProcessCommandCompletion(cBytesCursor pCursor, cItem pItem, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessCommandCompletion), pItem);
                    var lResult = ZProcessCommandCompletion(pCursor, pItem.Tag, pItem, lContext);
                    if (lResult == null) return false;
                    pItem.SetResult(lResult, mMailboxCache?.SelectedMailboxDetails?.Cache.UIDValidity, lContext);
                    return true;
                }

                public void Stop(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(Stop));

                    if (!mCancellationTokenSource.IsCancellationRequested)
                    {
                        try { mCancellationTokenSource.Cancel(); }
                        catch { }
                    }
                }

                public void Dispose()
                {
                    if (mDisposed) return;

                    if (mCancellationTokenSource != null && !mCancellationTokenSource.IsCancellationRequested)
                    {
                        try { mCancellationTokenSource.Cancel(); }
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

                    if (mBackgroundTerminator != null)
                    {
                        try { mBackgroundTerminator.Dispose(); }
                        catch { }
                    }

                    if (mCancellationTokenSource != null)
                    {
                        try { mCancellationTokenSource.Dispose(); }
                        catch { }
                    }

                    mDisposed = true;
                }
            }
        }
    }
}
