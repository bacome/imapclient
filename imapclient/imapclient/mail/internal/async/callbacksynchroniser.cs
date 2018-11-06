using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public abstract partial class cMailClient
    {
        protected internal class cCallbackSynchroniser : IDisposable
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public event EventHandler<cNetworkReceiveEventArgs> NetworkReceive;
            public event EventHandler<cNetworkSendEventArgs> NetworkSend;
            public event EventHandler<cCallbackExceptionEventArgs> CallbackException;

            private bool mDisposed = false;

            private readonly int mActionInvokeDelayMilliseconds;

            private readonly ConcurrentQueue<sInvoke> mInvokes = new ConcurrentQueue<sInvoke>();

            private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource(); // for use when disposing
            private readonly cReleaser mForegroundReleaser;
            private readonly cReleaser mBackgroundReleaser;

            private object mSender = null;
            private Task mBackgroundTask = null;

            private volatile bool mOutstandingPost = false;

            public cCallbackSynchroniser(int pActionInvokeDelayMilliseconds)
            {
                if (pActionInvokeDelayMilliseconds < -1) throw new ArgumentOutOfRangeException(nameof(pActionInvokeDelayMilliseconds));
                mActionInvokeDelayMilliseconds = pActionInvokeDelayMilliseconds;
                mForegroundReleaser = new cReleaser("callbacksynchroniser_foreground", mCancellationTokenSource.Token);
                mBackgroundReleaser = new cReleaser("callbacksynchroniser_background", mCancellationTokenSource.Token);
            }

            public void Start(object pSender, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(Start));
                if (mSender != null) throw new InvalidOperationException();
                mSender = pSender;
                mBackgroundTask = ZBackgroundTaskAsync(lContext);
            }

            public SynchronizationContext SynchronizationContext { get; set; } = SynchronizationContext.Current; // the context on which events should be delivered

            public void Wait(Task pAsyncTask, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(Wait));

                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));

                if (!pAsyncTask.IsCompleted) // for efficiency
                {
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
                            ZInvokeWorker(lContext);
                        }

                        if (lTask == 0) break; // pAsyncTask is finished
                    }
                }

                if (pAsyncTask.IsFaulted)
                {
                    lContext.TraceException(TraceEventType.Verbose, "task completed with exception", pAsyncTask.Exception);
                    ExceptionDispatchInfo.Capture(cMailTools.Flatten(pAsyncTask.Exception)).Throw();
                }

                if (pAsyncTask.IsCanceled)
                {
                    lContext.TraceVerbose("task was cancelled");
                    throw new OperationCanceledException();
                }

                lContext.TraceVerbose("task completed successfully");
            }

            public void InvokePropertyChanged(string pPropertyName, cTrace.cContext pParentContext)
            {
                if (PropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokePropertyChanged), pPropertyName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new PropertyChangedEventArgs(pPropertyName), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeCancellableCountChanged(cTrace.cContext pParentContext)
            {
                if (PropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeCancellableCountChanged));
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new PropertyChangedEventArgs(nameof(cMailClient.CancellableCount)), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeNetworkReceive(cResponse pResponse, cTrace.cContext pParentContext)
            {
                if (NetworkReceive == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(NetworkReceive));
                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cNetworkReceiveEventArgs(pResponse), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public int NetworkSendSubscriptionCount => NetworkSend?.GetInvocationList().Length ?? 0;

            public void InvokeNetworkSend(cBytes pBuffer, cTrace.cContext pParentContext)
            {
                if (NetworkSend == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeNetworkSend));
                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cNetworkSendEventArgs(pBuffer), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeNetworkSend(IEnumerable<byte> pBuffer, cTrace.cContext pParentContext)
            {
                if (NetworkSend == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeNetworkSend));
                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cNetworkSendEventArgs(pBuffer), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeNetworkSend(int? pBytes, IEnumerable<cBytes> pBuffers, cTrace.cContext pParentContext)
            {
                if (NetworkSend == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeNetworkSend));
                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cNetworkSendEventArgs(pBytes, pBuffers), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeActionLong(Action<long> pAction, long pLong, cTrace.cContext pParentContext)
            {
                if (pAction == null) return;
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeActionLong), pLong);
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cActionLongEventArgs(pAction, pLong), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            private void ZInvokeActionInt(Action<int> pAction, int pInt, cTrace.cContext pParentContext)
            {
                if (pAction == null) return;
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeActionInt), pInt);
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cActionIntEventArgs(pAction, pInt), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public cIncrementInvoker GetNewIncrementInvoker(Action<int> pIncrement, cTrace.cContext pContextForInvoke) => new cIncrementInvoker(this, pIncrement, pContextForInvoke);

            public bool IsDisposed => mDisposed;

            protected virtual void YInvoke(object pSender, EventArgs pEventArgs, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(YInvoke));
                lContext.TraceError("unknown event type", pEventArgs);
            }

            protected async Task YInvokeAndWaitAsync(EventArgs pEventArgs, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(YInvokeAndWaitAsync));

                var lSynchronizationContext = SynchronizationContext;

                if (lSynchronizationContext == null || ReferenceEquals(SynchronizationContext.Current, lSynchronizationContext))
                {
                    lContext.TraceVerbose("on the correct sc");
                    mInvokes.Enqueue(new sInvoke(pEventArgs));
                    ZInvokeWorker(lContext);
                    return;
                }

                lContext.TraceVerbose("not on the correct sc");

                using (var lReleaser = new cReleaser("invokesynchroniser_fireandwait", mCancellationTokenSource.Token))
                {
                    mInvokes.Enqueue(new sInvoke(pEventArgs, lReleaser));

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

            protected void YInvokeAndForget(EventArgs pEventArgs, cTrace.cContext pParentContext)
            {
                mInvokes.Enqueue(new sInvoke(pEventArgs));
                YInvokeAndForget(pParentContext);
            }

            protected void YInvokeAndForgetEnqueue(EventArgs pEventArgs) => mInvokes.Enqueue(new sInvoke(pEventArgs));

            protected void YInvokeAndForget(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(YInvokeAndForget));

                var lSynchronizationContext = SynchronizationContext;

                if (lSynchronizationContext == null || ReferenceEquals(SynchronizationContext.Current, lSynchronizationContext))
                {
                    lContext.TraceVerbose("on the correct sc");
                    ZInvokeWorker(lContext);
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

            private void ZInvokeWorker(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(ZInvokeWorker));

                while (mInvokes.TryDequeue(out var lInvoke))
                {
                    try
                    {
                        switch (lInvoke.EventArgs)
                        {
                            case PropertyChangedEventArgs lEventArgs:

                                PropertyChanged?.Invoke(mSender, lEventArgs);
                                break;

                            case cNetworkReceiveEventArgs lEventArgs:

                                NetworkReceive?.Invoke(mSender, lEventArgs);
                                break;

                            case cNetworkSendEventArgs lEventArgs:

                                NetworkSend?.Invoke(mSender, lEventArgs);
                                break;

                            case cActionIntEventArgs lEventArgs:

                                lEventArgs.Action(lEventArgs.Int);
                                break;

                            case cActionLongEventArgs lEventArgs:

                                lEventArgs.Action(lEventArgs.Long);
                                break;

                            default:

                                YInvoke(mSender, lInvoke.EventArgs, lContext);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException("error when invoking event handler", e);
                        try { CallbackException?.Invoke(mSender, new cCallbackExceptionEventArgs(e)); }
                        catch { }
                    }

                    if (lInvoke.Releaser != null) lInvoke.Releaser.Release(lContext);
                }
            }

            private async Task ZBackgroundTaskAsync(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewRootMethod(nameof(cCallbackSynchroniser), nameof(ZBackgroundTaskAsync));

                while (true)
                {
                    lContext.TraceVerbose("waiting");
                    await mBackgroundReleaser.GetAwaitReleaseTask(lContext).ConfigureAwait(false);
                    mBackgroundReleaser.Reset(lContext);

                    var lSynchronizationContext = SynchronizationContext;

                    if (lSynchronizationContext == null || ReferenceEquals(SynchronizationContext.Current, lSynchronizationContext))
                    {
                        lContext.TraceVerbose("on the correct sc");
                        ZInvokeWorker(lContext);
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
                                        ZInvokeWorker(lContext);
                                    },
                                null);
                        }
                    }
                }
            }

            public sealed class cIncrementInvoker : IDisposable
            {
                private bool mDisposed = false;

                private readonly object mLock = new object();
                private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();

                private readonly cCallbackSynchroniser mSynchroniser;
                private readonly Action<int> mIncrement;
                private readonly cTrace.cContext mContextForInvoke;

                private long mValue = 0;
                private Task mBackgroundTask = null;

                public cIncrementInvoker(cCallbackSynchroniser pSynchroniser, Action<int> pIncrement, cTrace.cContext pContextForInvoke)
                {
                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mIncrement = pIncrement ?? throw new ArgumentNullException(nameof(pIncrement));
                    mContextForInvoke = pContextForInvoke ?? throw new ArgumentNullException(nameof(pContextForInvoke));
                }

                public void Increment(int pValue)
                {
                    if (mDisposed) throw new ObjectDisposedException(nameof(cIncrementInvoker));

                    if (pValue < 0) throw new ArgumentOutOfRangeException(nameof(pValue));
                    if (pValue == 0) return;

                    long lValue;

                    lock (mLock)
                    {
                        lValue = mValue;
                        mValue += pValue;
                    }

                    if (lValue == 0)
                    {
                        if (mBackgroundTask != null)
                        {
                            mBackgroundTask.Wait();
                            mBackgroundTask.Dispose();
                        }

                        mBackgroundTask = ZBackgroundTask();
                    }
                }

                private async Task ZBackgroundTask()
                {
                    try { await Task.Delay(mSynchroniser.mActionInvokeDelayMilliseconds, mCancellationTokenSource.Token).ConfigureAwait(false); }
                    catch { }

                    long lValue;

                    lock (mLock)
                    {
                        lValue = mValue;
                        mValue = 0;
                    }

                    while (true)
                    {
                        if (lValue > int.MaxValue)
                        {
                            mSynchroniser.ZInvokeActionInt(mIncrement, int.MaxValue, mContextForInvoke);
                            lValue -= int.MaxValue;
                        }
                        else
                        {
                            mSynchroniser.ZInvokeActionInt(mIncrement, (int)lValue, mContextForInvoke);
                            return;
                        }
                    }
                }

                public void Dispose()
                {
                    if (mCancellationTokenSource != null && !mCancellationTokenSource.IsCancellationRequested) mCancellationTokenSource.Cancel();

                    if (mBackgroundTask != null)
                    {
                        try { mBackgroundTask.Wait(); }
                        catch { }

                        try { mBackgroundTask.Dispose(); }
                        catch { }
                    }

                    if (mCancellationTokenSource != null)
                    {
                        try { mCancellationTokenSource.Dispose(); }
                        catch { }
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool pDisposing)
            {
                if (mDisposed) return;

                if (pDisposing)
                {
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
                }

                mDisposed = true;
            }

            private struct sInvoke
            {
                public readonly EventArgs EventArgs;
                public readonly cReleaser Releaser;

                public sInvoke(EventArgs pEventArgs)
                {
                    EventArgs = pEventArgs;
                    Releaser = null;
                }

                public sInvoke(EventArgs pEventArgs, cReleaser pReleaser)
                {
                    EventArgs = pEventArgs;
                    Releaser = pReleaser;
                }
            }

            private class cActionIntEventArgs : EventArgs
            {
                public readonly Action<int> Action;
                public readonly int Int;

                public cActionIntEventArgs(Action<int> pAction, int pInt)
                {
                    Action = pAction ?? throw new ArgumentNullException(nameof(pAction));
                    Int = pInt;
                }

                public override string ToString() => $"{nameof(cActionIntEventArgs)}({Int})";
            }

            private class cActionLongEventArgs : EventArgs
            {
                public readonly Action<long> Action;
                public readonly long Long;

                public cActionLongEventArgs(Action<long> pAction, long pLong)
                {
                    Action = pAction ?? throw new ArgumentNullException(nameof(pAction));
                    Long = pLong;
                }

                public override string ToString() => $"{nameof(cActionLongEventArgs)}({Long})";
            }
        }
    }
}
