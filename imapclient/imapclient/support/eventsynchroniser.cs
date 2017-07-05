using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private sealed class cEventSynchroniser : IDisposable
        {
            private bool mDisposed = false;

            private readonly cIMAPClient mClient;
            private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource(); // for use when disposing
            private readonly cReleaser mForegroundReleaser;
            private readonly cReleaser mBackgroundReleaser;
            private readonly Task mBackgroundTask;
            private readonly ConcurrentQueue<sEvent> mEvents = new ConcurrentQueue<sEvent>();
            private volatile bool mOutstandingPost = false;

            public cEventSynchroniser(cIMAPClient pClient, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewObject(nameof(cEventSynchroniser));
                mClient = pClient;
                mForegroundReleaser = new cReleaser("eventsynchroniser_foreground", mCancellationTokenSource.Token);
                mBackgroundReleaser = new cReleaser("eventsynchroniser_background", mCancellationTokenSource.Token);
                mBackgroundTask = ZBackgroundTaskAsync(lContext);
            }

            public SynchronizationContext SynchronizationContext { get; set; } = SynchronizationContext.Current; // the context on which events should be delivered

            public void Wait(Task pAsyncTask, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(Wait));

                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));

                while (true)
                {
                    lContext.TraceVerbose("waiting");
                    int lTask = Task.WaitAny(pAsyncTask, mForegroundReleaser.GetAwaitReleaseTask(lContext));

                    if (mCancellationTokenSource.IsCancellationRequested)
                    {
                        lContext.TraceError("this object is disposing");
                        throw new OperationCanceledException();
                    }

                    mForegroundReleaser.Reset(lContext);

                    var lSynchronizationContext = SynchronizationContext;

                    if (lSynchronizationContext == null || ReferenceEquals(SynchronizationContext.Current, lSynchronizationContext))
                    {
                        lContext.TraceVerbose("on the correct sc");
                        ZInvokeEvents(lContext);
                    }

                    if (lTask == 0) break; // pAsyncTask is finished
                }

                if (pAsyncTask.Exception == null)
                {
                    // the operation cancelled exception, if thrown in the task, is not stored in the task's exception property
                    //
                    if (pAsyncTask.IsCanceled)
                    {
                        lContext.TraceVerbose("task was cancelled");
                        throw new OperationCanceledException();
                    }

                    lContext.TraceVerbose("task completed successfully");
                    return;
                }

                lContext.TraceException(TraceEventType.Verbose, "task completed with exception", pAsyncTask.Exception);

                var lException = pAsyncTask.Exception.Flatten();
                if (lException.InnerExceptions.Count == 1) throw lException.InnerExceptions[0];
                throw pAsyncTask.Exception;
            }

            public void PropertyChanged(string pPropertyName, cTrace.cContext pParentContext)
            {
                if (mClient.PropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(PropertyChanged), pPropertyName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new PropertyChangedEventArgs(pPropertyName), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void ResponseText(eResponseTextType pResponseTextType, cResponseText pResponseText, cTrace.cContext pParentContext)
            {
                if (mClient.ResponseText == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(ResponseText), pResponseTextType, pResponseText);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new cResponseTextEventArgs(pResponseTextType, pResponseText), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void MailboxPropertyChanged(cMailboxId pMailboxId, string pPropertyName, cTrace.cContext pParentContext)
            {
                if (mClient.MailboxPropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(MailboxPropertyChanged), pMailboxId, pPropertyName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new cMailboxPropertyChangedEventArgs(pMailboxId, pPropertyName), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void MailboxMessageDelivery(cMailboxId pMailboxId, cHandleList pHandles, cTrace.cContext pParentContext)
            {
                if (mClient.MailboxMessageDelivery == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(MailboxMessageDelivery), pMailboxId, pHandles);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new cMailboxMessageDeliveryEventArgs(pMailboxId, pHandles), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void MessagePropertyChanged(cMailboxId pMailboxId, iMessageHandle pHandle, string pPropertyName, cTrace.cContext pParentContext)
            {
                if (mClient.MessagePropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(MessagePropertyChanged), pMailboxId, pHandle, pPropertyName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new cMessagePropertyChangedEventArgs(pMailboxId, pHandle, pPropertyName), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void IncrementProgress(Action<int> pIncrementProgress, int pValue, cTrace.cContext pParentContext)
            {
                if (pIncrementProgress == null) return;
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(IncrementProgress), pValue);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new cIncrementProgressEventArgs(pIncrementProgress, pValue), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            private async Task ZFireAndWaitAsync(EventArgs pEventArgs, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(ZFireAndWaitAsync));

                var lSynchronizationContext = SynchronizationContext;

                if (lSynchronizationContext == null || ReferenceEquals(SynchronizationContext.Current, lSynchronizationContext))
                {
                    lContext.TraceVerbose("on the correct sc");
                    mEvents.Enqueue(new sEvent(pEventArgs));
                    ZInvokeEvents(lContext);
                    return;
                }

                lContext.TraceVerbose("not on the correct sc");

                using (var lReleaser = new cReleaser("eventsynchroniser_fireandwait", mCancellationTokenSource.Token))
                {
                    mEvents.Enqueue(new sEvent(pEventArgs, lReleaser));

                    // in case this object is blocking the SC (in 'wait' above), do a release to let it invoke the event handlers
                    mForegroundReleaser.Release(lContext);

                    // we use a background task to post to the sc in case;
                    //  1) the sc doesn't implement an async post method and
                    //  2) it is the sc that we are blocking OR ARE ABOUT TO BLOCK
                    //  note the words in capitals - at this very moment we may be being entered by the SC and are about to block it
                    //   (and in that case the 'wait' will do the event delivery because of the 'release' we just did above)
                    //
                    mBackgroundReleaser.Release(lContext);

                    lContext.TraceVerbose("waiting for the invoke to be done");
                    await lReleaser.GetAwaitReleaseTask(lContext).ConfigureAwait(false);
                }
            }

            private void ZFireAndForget(EventArgs pEventArgs, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(ZFireAndForget));

                mEvents.Enqueue(new sEvent(pEventArgs));

                var lSynchronizationContext = SynchronizationContext;

                if (lSynchronizationContext == null || ReferenceEquals(SynchronizationContext.Current, lSynchronizationContext))
                {
                    lContext.TraceVerbose("on the correct sc");
                    ZInvokeEvents(lContext);
                    return;
                }

                lContext.TraceVerbose("not on the correct sc");

                // in case this object is blocking the SC (in 'wait' above), do a release to let it invoke the event handlers
                mForegroundReleaser.Release(lContext);

                // we use a background task to post to the sc in case;
                //  1) the sc doesn't implement an async post method and
                //  2) it is the sc that we are blocking OR ARE ABOUT TO BLOCK
                //  note the words in capitals - at this very moment we may be being entered by the SC and are about to block it
                //   (and in that case the 'wait' will do the event delivery because of the 'release' we just did above)
                //
                mBackgroundReleaser.Release(lContext);
            }

            private void ZInvokeEvents(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(ZInvokeEvents));

                while (mEvents.TryDequeue(out var lEvent))
                {
                    try
                    {
                        switch (lEvent.EventArgs)
                        {
                            case cMessagePropertyChangedEventArgs lEventArgs:

                                mClient.MessagePropertyChanged?.Invoke(mClient, lEventArgs);
                                break;

                            case cMailboxPropertyChangedEventArgs lEventArgs:

                                mClient.MailboxPropertyChanged?.Invoke(mClient, lEventArgs);
                                break;

                            case PropertyChangedEventArgs lEventArgs:

                                mClient.PropertyChanged?.Invoke(mClient, lEventArgs);
                                break;

                            case cResponseTextEventArgs lEventArgs:

                                mClient.ResponseText?.Invoke(mClient, lEventArgs);
                                break;

                            case cMailboxMessageDeliveryEventArgs lEventArgs:

                                mClient.MailboxMessageDelivery?.Invoke(mClient, lEventArgs);
                                break;

                            case cIncrementProgressEventArgs lEventArgs:

                                lEventArgs.IncrementProgress(lEventArgs.Value);
                                break;

                            default:

                                lContext.TraceError("unknown event type", lEvent.EventArgs);
                                break;
                        }
                    }
                    catch (Exception e) { lContext.TraceException("error when invoking event handler", e); }

                    if (lEvent.Releaser != null) lEvent.Releaser.Release(lContext);
                }
            }

            private async Task ZBackgroundTaskAsync(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewRootMethod(nameof(cEventSynchroniser), nameof(ZBackgroundTaskAsync));

                while (true)
                {
                    lContext.TraceVerbose("waiting");
                    await mBackgroundReleaser.GetAwaitReleaseTask(lContext).ConfigureAwait(false);
                    mBackgroundReleaser.Reset(lContext);

                    var lSynchronizationContext = SynchronizationContext;

                    if (lSynchronizationContext == null || ReferenceEquals(SynchronizationContext.Current, lSynchronizationContext))
                    {
                        lContext.TraceVerbose("on the correct sc");
                        ZInvokeEvents(lContext);
                    }
                    else
                    {
                        if (mOutstandingPost) lContext.TraceVerbose("not on the correct sc: but there is an outstanding post");
                        else
                        {
                            lContext.TraceVerbose("not on the correct sc: posting an invoke to the correct one");

                            mOutstandingPost = true;

                            // this is the reason the background task exists: if the SC doesn't implement an async post method then this could block
                            lSynchronizationContext.Post(
                                (p) => 
                                    {
                                        mOutstandingPost = false;
                                        ZInvokeEvents(lContext);
                                    },
                                null); 
                        }
                    }
                }
            }

            public void Dispose()
            {
                if (mDisposed) return;

                if (mCancellationTokenSource != null && !mCancellationTokenSource.IsCancellationRequested) mCancellationTokenSource.Cancel();

                if (mBackgroundTask != null)
                {
                    try { mBackgroundTask.Wait(); }
                    catch { }

                    try { mBackgroundTask.Dispose(); }
                    catch { }
                }

                if (mBackgroundReleaser != null)
                {
                    try { mBackgroundReleaser.Dispose(); }
                    catch { }
                }

                if (mForegroundReleaser != null)
                {
                    try { mForegroundReleaser.Dispose(); }
                    catch { }
                }

                if (mCancellationTokenSource != null)
                {
                    try { mCancellationTokenSource.Dispose(); }
                    catch { }
                }

                mDisposed = true;
            }

            private struct sEvent
            {
                public readonly EventArgs EventArgs;
                public readonly cReleaser Releaser;

                public sEvent(EventArgs pEventArgs)
                {
                    EventArgs = pEventArgs;
                    Releaser = null;
                }

                public sEvent(EventArgs pEventArgs, cReleaser pReleaser)
                {
                    EventArgs = pEventArgs;
                    Releaser = pReleaser;
                }
            }

            private class cIncrementProgressEventArgs : EventArgs
            {
                public readonly Action<int> IncrementProgress;
                public readonly int Value;

                public cIncrementProgressEventArgs(Action<int> pIncrementProgress, int pValue)
                {
                    IncrementProgress = pIncrementProgress ?? throw new ArgumentNullException(nameof(pIncrementProgress));
                    Value = pValue;
                }

                public override string ToString() => $"{nameof(cIncrementProgressEventArgs)}({Value})";
            }
        }
    }
}
