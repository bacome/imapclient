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

                            lock (mPipelineLock)
                            {
                                if (lBackgroundSendTask == null &&
                                    ((mCurrentCommand == null && mQueuedCommands.Count > 0) ||
                                     (mCurrentCommand != null && mCurrentCommand.State == eCommandState.sending && !mCurrentCommand.AwaitingContinuation && !mCurrentCommand.IsAuthentication)
                                    )
                                   ) lBackgroundSendTask = ZBackgroundSendAsync(lContext);
                            }

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
                        mBackgroundTaskException = new cPipelineStoppedException(cTools.Flatten(e), lContext);
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
                        lContext.TraceVerbose("send is complete");
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

                private Task ZBackgroundTaskProcessResponseAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundTaskProcessResponseAsync));

                    var lResponse = mConnection.GetResponse(lContext);
                    mSynchroniser.InvokeNetworkReceive(lResponse, lContext);
                    var lCursor = new cBytesCursor(lResponse);

                    if (lCursor.SkipBytes(kPlusSpace))
                    {
                        lock (mPipelineLock)
                        {
                            if (mCurrentCommand == null || !mCurrentCommand.AwaitingContinuation) throw new cUnexpectedIMAPServerActionException(null, kUnexpectedIMAPServerActionMessage.UnexpectedContinuationRequest, 0, lContext);

                            if (!mCurrentCommand.IsAuthentication) 
                            {
                                mResponseTextProcessor.Process(eIMAPResponseTextContext.continuerequest, lCursor, mCurrentCommand.Hook, lContext);
                                mCurrentCommand.ResetAwaitingContinuation(lContext);
                                return Task.WhenAll();
                            }
                        }

                        // we are doing authentication
                        return ZProcessChallengeAsync(lCursor, lContext);
                    }

                    if (ZProcessDataResponse(lCursor, lContext)) return Task.WhenAll();

                    lock (mPipelineLock)
                    {
                        if (ZBackgroundTaskProcessActiveCommandCompletion(lCursor, lContext)) return Task.WhenAll();

                        if (mCurrentCommand != null && ZBackgroundTaskProcessCommandCompletion(lCursor, mCurrentCommand, lContext))
                        {
                            if (mCurrentCommand.IsAuthentication || mCurrentCommand.AwaitingContinuation) mCurrentCommand = null;
                            return Task.WhenAll();
                        }
                    }

                    lContext.TraceError("unrecognised response: {0}", lResponse);
                    return Task.WhenAll();
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

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        // if the UID validity changed while the command was running and the command depends on the UID not changing, something bad could have happened
                        //  (like updating the wrong message)
                        //
                        if (pCommand.UIDValidity != null && pCommand.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.MessageCache.UIDValidity)
                        {
                            pCommand.SetException(new cUIDValidityException(lResult, lContext), lContext);
                            return true;
                        }

                        // gmail responds with an OK [CANNOT] to a CREATE with a name it doesn't like
                        //  OK means command success, CANNOT means the operation can never succeed
                        //  what should the library do?
                        //
                        if (lResult.ResponseText.CodeIsAlwaysAnError)
                        {
                            pCommand.SetException(new cUnexpectedIMAPServerActionException(lResult, kUnexpectedIMAPServerActionMessage.OKAndError, 0, lContext), lContext);
                            return true;
                        }
                    }

                    pCommand.SetResult(lResult, lContext);
                    return true;
                }
            }
        }
    }
}