﻿using System;
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
                private bool mStopped = false;

                // stuff
                private readonly cEventSynchroniser mEventSynchroniser;
                private readonly cConnection mConnection;
                private readonly cResponseTextProcessor mResponseTextProcessor;
                private cIdleConfiguration mIdleConfiguration;
                private readonly Action<cTrace.cContext> mDisconnect;

                // mechanics
                private readonly cExclusiveAccess mIdleBlock = new cExclusiveAccess("idleblock", 100); // used to control idling
                private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource(); // for use when stopping the background task

                // installable components
                private readonly List<iResponseDataParser> mResponseDataParsers = new List<iResponseDataParser>();
                private readonly List<cUnsolicitedDataProcessor> mUnsolicitedDataProcessors = new List<cUnsolicitedDataProcessor>();

                // background task objects
                private readonly cMethodControl mBackgroundMC;
                private readonly cReleaser mBackgroundReleaser;
                private readonly cTerminator mBackgroundTerminator;
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
                private readonly Queue<cPipelineCommand> mQueuedCommands = new Queue<cPipelineCommand>();
                private cCurrentPipelineCommand mCurrentCommand = null;
                private readonly cActivePipelineCommands mActiveCommands = new cActivePipelineCommands();

                // growable buffers
                private cByteList mSendBuffer = new cByteList();
                private cByteList mTraceBuffer = new cByteList();

                public cCommandPipeline(cEventSynchroniser pEventSynchroniser, cConnection pConnection, cResponseTextProcessor pResponseTextProcessor, cIdleConfiguration pIdleConfiguration, Action<cTrace.cContext> pDisconnect, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cCommandPipeline), pIdleConfiguration);

                    mEventSynchroniser = pEventSynchroniser;
                    mConnection = pConnection;
                    mResponseTextProcessor = pResponseTextProcessor;
                    mIdleConfiguration = pIdleConfiguration;
                    mDisconnect = pDisconnect;

                    mIdleBlock.Released += mBackgroundReleaser.Release; // when the idle block is removed, kick the background process

                    mBackgroundMC = new cMethodControl(System.Threading.Timeout.Infinite, mCancellationTokenSource.Token);
                    mBackgroundReleaser = new cReleaser("commandpipeline_background", mCancellationTokenSource.Token);
                    mBackgroundTerminator = new cTerminator(mCancellationTokenSource.Token);
                    mBackgroundTask = ZBackgroundTaskAsync(lContext);
                }

                public void Install(iResponseDataParser pResponseDataParser) => mResponseDataParsers.Add(pResponseDataParser);
                public void Install(cUnsolicitedDataProcessor pUnsolicitedDataProcessor) => mUnsolicitedDataProcessors.Add(pUnsolicitedDataProcessor);

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

                public async Task<cCommandResult> ExecuteAsync(cMethodControl pMC, cCommand pCommand, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ExecuteAsync), pMC, pCommand);

                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));

                    using (var lCommand = new cPipelineCommand(pCommand, mPipelineLock))
                    {
                        lock (mPipelineLock)
                        {
                            if (mStopped) throw mBackgroundTaskException;
                            pCommand.SetManualDispose();
                            mQueuedCommands.Enqueue(lCommand);
                        }

                        mBackgroundReleaser.Release(lContext);

                        return await lCommand.WaitAsync(pMC, lContext).ConfigureAwait(false);
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
                        }
                    }
                    catch (cUnilateralByeException e)
                    {
                        lContext.TraceInformation("the pipeline is stopping due to a unilateral bye");
                        mBackgroundTaskException = e;
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
                    if (mCurrentCommand != null) mCurrentCommand.Command.SetException(mBackgroundTaskException, lContext);
                    foreach (var lCommand in mQueuedCommands) lCommand.SetException(mBackgroundTaskException, lContext);
                }

                private async Task ZSendAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZSendAsync));

                    mSendBuffer.Clear();
                    mTraceBuffer.Clear();

                    if (mCurrentCommand != null) ZSendAppendCurrentPartDataAndMoveNextPart();

                    while (true)
                    {
                        if (mCurrentCommand == null)
                        {
                            ZSendMoveNextCommandAndAppendTag(lContext);
                            if (mCurrentCommand == null) break;
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
                        mEventSynchroniser.FireNetworkActivity(mTraceBuffer, lContext);
                        await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);
                    }
                }

                private bool ZSendAppendCurrentPartDataAndMoveNextPart()
                {
                    // appends the current part of the current command to the buffer to send
                    //  if that was the last part of the current command
                    //   if this is authentication true is returned
                    //   else the command gets added to the active commands and the current command is nulled

                    var lPart = mCurrentCommand.CurrentPart;

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

                            if (lCommand.State == eCommandState.pending)
                            {
                                if (lCommand.UIDValidity != null && lCommand.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.Cache.UIDValidity) lCommand.SetException(new cUIDValidityChangedException(lContext), lContext);
                                else
                                {
                                    lCommand.SetActive(lContext);
                                    mCurrentCommand = new cCurrentPipelineCommand(lCommand);
                                    break;
                                }
                            }
                        }
                    }

                    if (mCurrentCommand == null) return;

                    mSendBuffer.AddRange(mCurrentCommand.Tag);
                    mSendBuffer.Add(cASCII.SPACE);

                    mTraceBuffer.AddRange(mCurrentCommand.Tag);
                    mTraceBuffer.Add(cASCII.SPACE);
                }

                private bool ZSendAppendCurrentPartLiteralHeader()
                {
                    // if the current part is a literal, appends the literal header
                    //  if the current part is a synchronising literal, returns true

                    var lPart = mCurrentCommand.CurrentPart;

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
                        mEventSynchroniser.FireNetworkActivity(lLines, lContext);
                        var lCursor = new cBytesCursor(lLines);

                        if (mCurrentCommand != null)
                        {
                            if (mAuthenticateState == null)
                            {
                                if (ZResponseIsContinuation(lCursor, mCurrentCommand, lContext)) return null;
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
                            if (ZProcessCommandCompletion(lCursor, mCurrentCommand.Command, lContext))
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
                            mEventSynchroniser.FireNetworkActivity(mSendBuffer, lContext);
                            await mConnection.WriteAsync(mBackgroundMC, mSendBuffer.ToArray(), lContext).ConfigureAwait(false);

                            // wait for continuation
                            await ZIdleProcessResponsesAsync(lTag, true, lContext).ConfigureAwait(false);

                            // process responses until (normally) the countdown or backgroundreleaser are signalled
                            Task lCompleted = await ZIdleProcessResponsesAsync(lTag, false, lContext, lCountdownTask, mBackgroundReleaser.GetAwaitReleaseTask(lContext)).ConfigureAwait(false);

                            if (lCompleted == null) throw new cUnexpectedServerActionException(fKnownCapabilities.idle, "idle completed before done sent", lContext);

                            mSendBuffer.Clear();

                            mSendBuffer.AddRange(kDone);
                            mSendBuffer.Add(cASCII.CR);
                            mSendBuffer.Add(cASCII.LF);

                            lContext.TraceVerbose("sending {0}", mSendBuffer);
                            mEventSynchroniser.FireNetworkActivity(mSendBuffer, lContext);
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
                    mEventSynchroniser.FireNetworkActivity(mSendBuffer, lContext);
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
                        mEventSynchroniser.FireNetworkActivity(lLines, lContext);
                        var lCursor = new cBytesCursor(lLines);

                        if (pExpectContinuation && ZResponseIsContinuation(lCursor, null, lContext)) return null;

                        if (ZProcessData(lCursor, lContext)) continue;

                        var lResult = ZProcessCommandCompletion(lCursor, pTag, null, lContext);

                        if (lResult != null)
                        {
                            if (lResult.ResultType != eCommandResultType.ok) throw new cProtocolErrorException(lResult, fKnownCapabilities.idle, lContext);
                            if (pExpectContinuation) throw new cUnexpectedServerActionException(fKnownCapabilities.idle, "idle command completed before continuation received", lContext);
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
                        try { lResponse = mCurrentCommand.GetResponse(lChallenge); }
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
                        mEventSynchroniser.FireNetworkActivity(kAsterisk, lContext);
                    }
                    else
                    {
                        lContext.TraceVerbose("sending response");
                        cByteList lBytes = cBase64.Encode(lResponse);
                        lBytes.Add(cASCII.CR);
                        lBytes.Add(cASCII.LF);
                        lBuffer = lBytes.ToArray();
                        mEventSynchroniser.FireNetworkActivity(kSASLAuthenticationResponse, lContext);
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
                        mResponseTextProcessor.Process(pCursor, eResponseTextType.information, mActiveCommands, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kNoSpace))
                    {
                        lContext.TraceVerbose("got a warning");
                        mResponseTextProcessor.Process(pCursor, eResponseTextType.warning, mActiveCommands, lContext);
                        return true;
                    }

                    if (pCursor.SkipBytes(kBadSpace))
                    {
                        lContext.TraceVerbose("got a protocol error");
                        mResponseTextProcessor.Process(pCursor, eResponseTextType.protocolerror, mActiveCommands, lContext);
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

                    foreach (var lCommand in mActiveCommands)
                    {
                        if (lParsed) ZProcessDataWorker(ref lResult, lCommand.ProcessData(lData, lContext), lContext);
                        else ZProcessDataWorker(ref lResult, lCommand.ProcessData(pCursor, lContext), lContext);
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

                    var lResult = new cCommandResult(lResultType, mResponseTextProcessor.Process(pCursor, lTextType, pTextCodeProcessor, lContext));

                    if (mMailboxCache != null) mMailboxCache.CommandCompletion(lContext);

                    return lResult;
                }

                private bool ZProcessCommandCompletion(cBytesCursor pCursor, cPipelineCommand pCommand, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZProcessCommandCompletion), pCommand);
                    var lResult = ZProcessCommandCompletion(pCursor, pCommand.Tag, pCommand, lContext);
                    if (lResult == null) return false;

                    ;?; // uidvalidity check
                    //mMailboxCache?.SelectedMailboxDetails?.Cache.UIDValidity, 

                    pCommand.SetResult(lResult, lContext);
                    return true;
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
