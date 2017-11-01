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
                private async Task ZBackgroundTaskAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewRootMethod(nameof(cCommandPipeline), nameof(ZBackgroundTaskAsync));

                    Task lBackgroundSendTask = null;

                    try
                    {
                        while (true)
                        {
                            mBackgroundReleaser.Reset(lContext);

                            if (lBackgroundSendTask != null && lBackgroundSendTask.IsCompleted)
                            {
                                lContext.TraceVerbose("send is complete");
                                lBackgroundSendTask.Wait(); // will throw if there was an error
                                lBackgroundSendTask.Dispose();
                                lBackgroundSendTask = null;
                            }

                            if (lBackgroundSendTask == null &&
                                ((mCurrentCommand == null && mQueuedCommands.Count > 0) ||
                                 (mCurrentCommand != null && !mCurrentCommand.WaitingForContinuationRequest)
                                )
                               ) lBackgroundSendTask = ZBackgroundSendAsync(lContext);

                            if (lBackgroundSendTask == null && mCurrentCommand == null && mActiveCommands.Count == 0)
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

                                Task lCompleted = await mBackgroundAwaiter.AwaitAny(lBuildResponseTask, lBackgroundReleaserTask, lBackgroundSendTask).ConfigureAwait(false);

                                if (ReferenceEquals(lCompleted, lBuildResponseTask))
                                {
                                    await ZBackgroundTaskProcessResponseAsync(lContext).ConfigureAwait(false);

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

                    // if the send is still going this will kill it 
                    mConnection.Disconnect(lContext);

                    if (lBackgroundSendTask != null)
                    {
                        lContext.TraceVerbose("waiting for send to complete");
                        try { await lBackgroundSendTask.ConfigureAwait(false); }
                        catch (Exception e) { lContext.TraceException("send reported an exception at pipeline stop", e); }
                        lBackgroundSendTask.Dispose();
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

                private async Task ZBackgroundTaskProcessResponseAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundTaskProcessResponseAsync));

                    var lLines = mConnection.GetResponse(lContext);
                    mSynchroniser.InvokeNetworkReceive(lLines, lContext);
                    var lCursor = new cBytesCursor(lLines);

                    if (lCursor.SkipBytes(kPlusSpace))
                    {
                        var lCurrentCommand = mCurrentCommand;

                        if (lCurrentCommand == null || !lCurrentCommand.WaitingForContinuationRequest) throw new cUnexpectedServerActionException(0, "unexpected continuation request", lContext);

                        if (lCurrentCommand.IsAuthentication) await ZProcessChallengeAsync(lCursor, lContext).ConfigureAwait(false);
                        else
                        {
                            mResponseTextProcessor.Process(eResponseTextType.continuerequest, lCursor, lCurrentCommand.Hook, lContext);
                            lCurrentCommand.WaitingForContinuationRequest = false;
                        }

                        return;
                    }

                    lock (mPipelineLock)
                    {
                        if (ZProcessDataResponse(lCursor, lContext)) return;

                        if (ZBackgroundTaskProcessActiveCommandCompletion(lCursor, lContext)) return;

                        if (mCurrentCommand != null && ZBackgroundTaskProcessCommandCompletion(lCursor, mCurrentCommand, lContext))
                        {
                            if (mCurrentCommand.IsAuthentication) mCurrentCommand = null;
                            return;
                        }
                    }

                    lContext.TraceError("unrecognised response: {0}", lLines);
                }

                private bool ZBackgroundTaskProcessActiveCommandCompletion(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundTaskProcessActiveCommandCompletion));

                    for (int i = 0; i < mActiveCommands.Count; i++)
                    {
                        var lCommand = mActiveCommands[i];

                        if (ZBackgroundTaskProcessCommandCompletion(pCursor, lCommand, lContext))
                        {
                            mActiveCommands.RemoveAt(i);
                            return true;
                        }
                    }

                    return false;
                }

                private bool ZBackgroundTaskProcessCommandCompletion(cBytesCursor pCursor, cCommand pCommand, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundTaskProcessCommandCompletion), pCommand);

                    var lResult = ZProcessCommandCompletionResponse(pCursor, pCommand.Tag, pCommand.IsAuthentication, pCommand.Hook, lContext);
                    if (lResult == null) return false;

                    if (pCommand.UIDValidity != null && pCommand.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.Cache.UIDValidity) pCommand.SetException(new cUIDValidityChangedException(lContext), lContext);
                    else pCommand.SetResult(lResult, lContext);

                    return true;
                }
            }
        }
    }
}