using System;
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
                        return (await ZIdleProcessResponsesAsync(true, lCountdownTimer.GetAwaitCountdownTask(), null, false, lContext).ConfigureAwait(false) == eIdleProcessResponsesTerminatedBy.countdowntask);
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
                            mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, mIdleBuffer, lContext);
                            await mConnection.WriteAsync(mIdleBuffer.ToArray(), mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);

                            if (await ZIdleProcessResponsesAsync(false, null, lTag, true, lContext).ConfigureAwait(false) != eIdleProcessResponsesTerminatedBy.continuerequest) throw new cUnexpectedServerActionException(fCapabilities.idle, "idle completed before done sent", lContext);

                            var lProcessResponsesTerminatedBy = await ZIdleProcessResponsesAsync(true, lCountdownTimer.GetAwaitCountdownTask(), lTag, false, lContext).ConfigureAwait(false);

                            if (lProcessResponsesTerminatedBy == eIdleProcessResponsesTerminatedBy.commandcompletion) throw new cUnexpectedServerActionException(fCapabilities.idle, "idle completed before done sent", lContext);

                            mIdleBuffer.Clear();

                            mIdleBuffer.AddRange(kIdleDone);
                            mIdleBuffer.Add(cASCII.CR);
                            mIdleBuffer.Add(cASCII.LF);

                            lContext.TraceVerbose("sending {0}", mIdleBuffer);
                            mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, mIdleBuffer, lContext);
                            await mConnection.WriteAsync(mIdleBuffer.ToArray(), mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);

                            await ZIdleProcessResponsesAsync(false, null, lTag, false, lContext).ConfigureAwait(false);

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
                            if (mMailboxCache?.SelectedMailboxDetails != null)
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

                            if (await ZIdleProcessResponsesAsync(true, lCountdownTimer.GetAwaitCountdownTask(), null, false, lContext).ConfigureAwait(false) == eIdleProcessResponsesTerminatedBy.backgroundreleaser)
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

                    mIdleBuffer.Clear();

                    mIdleBuffer.AddRange(lTag);
                    mIdleBuffer.Add(cASCII.SPACE);
                    mIdleBuffer.AddRange(pCommand);
                    mIdleBuffer.Add(cASCII.CR);
                    mIdleBuffer.Add(cASCII.LF);

                    lContext.TraceVerbose("sending {0}", mIdleBuffer);
                    mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, mIdleBuffer, lContext);
                    await mConnection.WriteAsync(mIdleBuffer.ToArray(), mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);

                    await ZIdleProcessResponsesAsync(false, null, lTag, false, lContext).ConfigureAwait(false);
                }

                private enum eIdleProcessResponsesTerminatedBy { backgroundreleaser, countdowntask, commandcompletion, continuerequest }

                private async Task<eIdleProcessResponsesTerminatedBy> ZIdleProcessResponsesAsync(bool pMonitorBackgroundReleaser, Task pCountdownTask, cCommandTag pTag, bool pExpectContinueRequest, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZIdleProcessResponsesAsync), pMonitorBackgroundReleaser, pTag, pExpectContinueRequest);

                    Task lBackgroundReleaserTask;
                    if (pMonitorBackgroundReleaser) lBackgroundReleaserTask = mBackgroundReleaser.GetAwaitReleaseTask(lContext);
                    else lBackgroundReleaserTask = null;

                    while (true)
                    {
                        Task lBuildResponseTask = mConnection.GetBuildResponseTask(lContext);

                        Task lTask = await mBackgroundAwaiter.AwaitAny(lBuildResponseTask, lBackgroundReleaserTask, pCountdownTask).ConfigureAwait(false);

                        if (ReferenceEquals(lTask, lBackgroundReleaserTask)) return eIdleProcessResponsesTerminatedBy.backgroundreleaser;
                        if (ReferenceEquals(lTask, pCountdownTask)) return eIdleProcessResponsesTerminatedBy.countdowntask;

                        var lLines = mConnection.GetResponse(lContext);
                        mSynchroniser.InvokeNetworkActivity(lLines, lContext);
                        var lCursor = new cBytesCursor(lLines);

                        if (lCursor.SkipBytes(kPlusSpace))
                        {
                            if (!pExpectContinueRequest) throw new cUnexpectedServerActionException(fCapabilities.idle, "unexpected continuation request", lContext);
                            mResponseTextProcessor.Process(eResponseTextType.continuerequest, lCursor, null, lContext);
                            return eIdleProcessResponsesTerminatedBy.continuerequest;
                        }

                        if (ZProcessDataResponse(lCursor, lContext)) continue;

                        if (pTag != null && ZProcessCommandCompletionResponse(lCursor, pTag, false, null, lContext) != null) return eIdleProcessResponsesTerminatedBy.commandcompletion;

                        lContext.TraceError("unrecognised response: {0}", lLines);
                    }
                }
            }
        }
    }
}