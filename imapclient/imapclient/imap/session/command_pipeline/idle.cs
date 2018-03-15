using System;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            {
                private readonly cByteList mIdleBuffer = new cByteList();

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
                        return (await ZIdleProcessResponsesAsync(true, false, lCountdownTimer.GetAwaitCountdownTask(), null, false, lContext).ConfigureAwait(false) == eIdleProcessResponsesTerminatedBy.countdowntask);
                    }
                }

                private static readonly cBytes kIdleIdle = new cBytes("IDLE");
                private static readonly cBytes kIdleDone = new cBytes("DONE");

                private async Task ZIdleIdleAsync(int pIdleRestartInterval, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdleIdleAsync), pIdleRestartInterval);

                    using (cCountdownTimer lCountdownTimer = new cCountdownTimer(pIdleRestartInterval, lContext))
                    {
                        while (true)
                        {
                            cCommandTag lTag = new cCommandTag();

                            mIdleBuffer.Clear();

                            mIdleBuffer.AddRange(lTag);
                            mIdleBuffer.Add(cASCII.SPACE);
                            mIdleBuffer.AddRange(kIdleIdle);
                            mIdleBuffer.Add(cASCII.CR);
                            mIdleBuffer.Add(cASCII.LF);

                            lContext.TraceVerbose("sending {0}", mIdleBuffer);
                            mSynchroniser.InvokeNetworkSend(mIdleBuffer, lContext);
                            await mConnection.WriteAsync(mIdleBuffer.ToArray(), mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);

                            if (await ZIdleProcessResponsesAsync(false, false, null, lTag, true, lContext).ConfigureAwait(false) != eIdleProcessResponsesTerminatedBy.continuerequest) throw new cUnexpectedServerActionException(null, "idle completed before done sent", fIMAPCapabilities.idle, lContext);

                            var lProcessResponsesTerminatedBy = await ZIdleProcessResponsesAsync(true, true, lCountdownTimer.GetAwaitCountdownTask(), lTag, false, lContext).ConfigureAwait(false);

                            if (lProcessResponsesTerminatedBy == eIdleProcessResponsesTerminatedBy.commandcompletion) throw new cUnexpectedServerActionException(null, "idle completed before done sent", fIMAPCapabilities.idle, lContext);

                            mIdleBuffer.Clear();

                            mIdleBuffer.AddRange(kIdleDone);
                            mIdleBuffer.Add(cASCII.CR);
                            mIdleBuffer.Add(cASCII.LF);

                            lContext.TraceVerbose("sending {0}", mIdleBuffer);
                            mSynchroniser.InvokeNetworkSend(mIdleBuffer, lContext);
                            await mConnection.WriteAsync(mIdleBuffer.ToArray(), mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);

                            await ZIdleProcessResponsesAsync(false, false, null, lTag, false, lContext).ConfigureAwait(false);

                            if (lProcessResponsesTerminatedBy == eIdleProcessResponsesTerminatedBy.backgroundreleaser) return;

                            lCountdownTimer.Restart(lContext);
                        }
                    }
                }

                private static readonly cBytes kIdleNoOp = new cBytes("NOOP");
                private static readonly cBytes kIdleCheck = new cBytes("CHECK");

                private async Task ZIdlePollAsync(int pPollInterval, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdlePollAsync), pPollInterval);

                    using (cCountdownTimer lCountdownTimer = new cCountdownTimer(pPollInterval, lContext))
                    {
                        while (true)
                        {
                            if (mMailboxCache.SelectedMailboxDetails != null)
                            {
                                await ZIdlePollCommandAsync(kIdleCheck, lContext).ConfigureAwait(false);

                                if (mBackgroundReleaser.IsReleased(lContext))
                                {
                                    lContext.TraceVerbose("idle terminated during check");
                                    return;
                                }
                            }

                            await ZIdlePollCommandAsync(kIdleNoOp, lContext).ConfigureAwait(false);

                            if (mBackgroundReleaser.IsReleased(lContext))
                            {
                                lContext.TraceVerbose("idle terminated during noop");
                                return;
                            }

                            lContext.TraceVerbose("waiting");

                            var lProcessResponsesTerminatedBy = await ZIdleProcessResponsesAsync(true, true, lCountdownTimer.GetAwaitCountdownTask(), null, false, lContext).ConfigureAwait(false);

                            if (lProcessResponsesTerminatedBy == eIdleProcessResponsesTerminatedBy.backgroundreleaser) return;

                            lCountdownTimer.Restart(lContext);
                        }
                    }
                }

                private async Task ZIdlePollCommandAsync(cBytes pCommand, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdlePollCommandAsync), pCommand);

                    cCommandTag lTag = new cCommandTag();

                    mIdleBuffer.Clear();

                    mIdleBuffer.AddRange(lTag);
                    mIdleBuffer.Add(cASCII.SPACE);
                    mIdleBuffer.AddRange(pCommand);
                    mIdleBuffer.Add(cASCII.CR);
                    mIdleBuffer.Add(cASCII.LF);

                    lContext.TraceVerbose("sending {0}", mIdleBuffer);
                    mSynchroniser.InvokeNetworkSend(mIdleBuffer, lContext);
                    await mConnection.WriteAsync(mIdleBuffer.ToArray(), mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);

                    await ZIdleProcessResponsesAsync(false, false, null, lTag, false, lContext).ConfigureAwait(false);
                }

                private enum eIdleProcessResponsesTerminatedBy { backgroundreleaser, countdowntask, commandcompletion, continuerequest, pendingmodseq }

                private async Task<eIdleProcessResponsesTerminatedBy> ZIdleProcessResponsesAsync(bool pMonitorBackgroundReleaser, bool pMonitorPendingModSeq, Task pCountdownTask, cCommandTag pTag, bool pExpectContinueRequest, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdleProcessResponsesAsync), pMonitorBackgroundReleaser, pMonitorPendingModSeq, pTag, pExpectContinueRequest);

                    Task lBackgroundReleaserTask;
                    if (pMonitorBackgroundReleaser) lBackgroundReleaserTask = mBackgroundReleaser.GetAwaitReleaseTask(lContext);
                    else lBackgroundReleaserTask = null;

                    while (true)
                    {
                        if (pMonitorPendingModSeq && mMailboxCache.HasPendingHighestModSeq()) return eIdleProcessResponsesTerminatedBy.pendingmodseq;

                        Task lBuildResponseTask = mConnection.GetBuildResponseTask(lContext);

                        Task lTask = await mBackgroundAwaiter.AwaitAny(lBuildResponseTask, lBackgroundReleaserTask, pCountdownTask).ConfigureAwait(false);

                        if (ReferenceEquals(lTask, lBackgroundReleaserTask)) return eIdleProcessResponsesTerminatedBy.backgroundreleaser;
                        if (ReferenceEquals(lTask, pCountdownTask)) return eIdleProcessResponsesTerminatedBy.countdowntask;

                        var lResponse = mConnection.GetResponse(lContext);
                        mSynchroniser.InvokeNetworkReceive(lResponse, lContext);
                        var lCursor = new cBytesCursor(lResponse);

                        if (lCursor.SkipBytes(kPlusSpace))
                        {
                            if (!pExpectContinueRequest) throw new cUnexpectedServerActionException(null, "unexpected continuation request", fIMAPCapabilities.idle, lContext);
                            mResponseTextProcessor.Process(eResponseTextContext.continuerequest, lCursor, null, lContext);
                            return eIdleProcessResponsesTerminatedBy.continuerequest;
                        }

                        if (ZProcessDataResponse(lCursor, lContext)) continue;

                        if (pTag != null && ZProcessCommandCompletionResponse(lCursor, pTag, false, null, lContext) != null) return eIdleProcessResponsesTerminatedBy.commandcompletion;

                        lContext.TraceError("unrecognised response: {0}", lResponse);
                    }
                }
            }
        }
    }
}