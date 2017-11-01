﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private sealed class cCallbackSynchroniser : IDisposable
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public event EventHandler<cResponseTextEventArgs> ResponseText;
            public event EventHandler<cNetworkReceiveEventArgs> NetworkReceive;
            public event EventHandler<cNetworkSendEventArgs> NetworkSend;
            public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged;
            public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery;
            public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged;
            public event EventHandler<cCallbackExceptionEventArgs> CallbackException;

            private bool mDisposed = false;

            private readonly object mSender;
            private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource(); // for use when disposing
            private readonly cReleaser mForegroundReleaser;
            private readonly cReleaser mBackgroundReleaser;
            private readonly Task mBackgroundTask;
            private readonly ConcurrentQueue<sInvoke> mInvokes = new ConcurrentQueue<sInvoke>();
            private volatile bool mOutstandingPost = false;

            public cCallbackSynchroniser(object pSender, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewObject(nameof(cCallbackSynchroniser));
                mSender = pSender;
                mForegroundReleaser = new cReleaser("callbacksynchroniser_foreground", mCancellationTokenSource.Token);
                mBackgroundReleaser = new cReleaser("callbacksynchroniser_background", mCancellationTokenSource.Token);
                mBackgroundTask = ZBackgroundTaskAsync(lContext);
            }

            public SynchronizationContext SynchronizationContext { get; set; } = SynchronizationContext.Current; // the context on which events should be delivered

            public void Wait(Task pAsyncTask, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(Wait));

                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));

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

            public sEventSubscriptionCounts EventSubscriptionCounts
            {
                get
                {
                    sEventSubscriptionCounts lResult = new sEventSubscriptionCounts();
                    lResult.PropertyChanged = PropertyChanged?.GetInvocationList().Length ?? 0;
                    lResult.ResponseText = ResponseText?.GetInvocationList().Length ?? 0;
                    lResult.NetworkReceive = NetworkReceive?.GetInvocationList().Length ?? 0;
                    lResult.NetworkSend = NetworkSend?.GetInvocationList().Length ?? 0;
                    lResult.MailboxPropertyChanged = MailboxPropertyChanged?.GetInvocationList().Length ?? 0;
                    lResult.MailboxMessageDelivery = MailboxMessageDelivery?.GetInvocationList().Length ?? 0;
                    lResult.MessagePropertyChanged = MessagePropertyChanged?.GetInvocationList().Length ?? 0;
                    lResult.CallbackException = CallbackException?.GetInvocationList().Length ?? 0;
                    return lResult;

                }
            }

            public void InvokePropertyChanged(string pPropertyName, cTrace.cContext pParentContext)
            {
                if (PropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokePropertyChanged), pPropertyName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new PropertyChangedEventArgs(pPropertyName), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeCancellableCountChanged(cTrace.cContext pParentContext)
            {
                if (PropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeCancellableCountChanged));
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new PropertyChangedEventArgs(nameof(cIMAPClient.CancellableCount)), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeResponseText(eResponseTextType pTextType, cResponseText pResponseText, cTrace.cContext pParentContext)
            {
                if (ResponseText == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeResponseText), pTextType, pResponseText);
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new cResponseTextEventArgs(pTextType, pResponseText), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeNetworkReceive(cBytesLines pLines, cTrace.cContext pParentContext)
            {
                if (NetworkReceive == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(NetworkReceive));
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new cNetworkReceiveEventArgs(pLines), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public int NetworkSendSubscriptionCount => NetworkSend?.GetInvocationList().Length ?? 0;

            public void InvokeNetworkSend(cBytes pBuffer, cTrace.cContext pParentContext)
            {
                if (NetworkSend == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeNetworkSend));
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new cNetworkSendEventArgs(pBuffer), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeNetworkSend(IEnumerable<byte> pBuffer, cTrace.cContext pParentContext)
            {
                if (NetworkSend == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeNetworkSend));
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new cNetworkSendEventArgs(pBuffer), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeNetworkSend(int? pBytes, IEnumerable<cBytes> pBuffers, cTrace.cContext pParentContext)
            {
                if (NetworkSend == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeNetworkSend));
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new cNetworkSendEventArgs(pBytes, pBuffers), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeMailboxPropertiesChanged(iMailboxHandle pHandle, fMailboxProperties pProperties, cTrace.cContext pParentContext)
            {
                if (MailboxPropertyChanged == null || pProperties == 0) return; // pre-checks for efficiency

                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeMailboxPropertiesChanged), pHandle, pProperties);

                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));

                if ((pProperties & fMailboxProperties.exists) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.Exists)));

                if ((pProperties & fMailboxProperties.canhavechildren) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.CanHaveChildren)));
                if ((pProperties & fMailboxProperties.canselect) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.CanSelect)));
                if ((pProperties & fMailboxProperties.ismarked) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsMarked)));
                if ((pProperties & fMailboxProperties.isremote) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsRemote)));
                if ((pProperties & fMailboxProperties.haschildren) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HasChildren)));
                if ((pProperties & fMailboxProperties.containsall) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsAll)));
                if ((pProperties & fMailboxProperties.isarchive) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsArchive)));
                if ((pProperties & fMailboxProperties.containsdrafts) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsDrafts)));
                if ((pProperties & fMailboxProperties.containsflagged) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsFlagged)));
                if ((pProperties & fMailboxProperties.containsjunk) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsJunk)));
                if ((pProperties & fMailboxProperties.containssent) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsSent)));
                if ((pProperties & fMailboxProperties.containstrash) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsTrash)));

                if ((pProperties & fMailboxProperties.issubscribed) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsSubscribed)));

                if ((pProperties & fMailboxProperties.messagecount) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.MessageCount)));
                if ((pProperties & fMailboxProperties.recentcount) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.RecentCount)));
                if ((pProperties & fMailboxProperties.uidnext) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UIDNext)));
                if ((pProperties & fMailboxProperties.uidnextunknowncount) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UIDNextUnknownCount)));
                if ((pProperties & fMailboxProperties.uidvalidity) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UIDValidity)));
                if ((pProperties & fMailboxProperties.unseencount) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UnseenCount)));
                if ((pProperties & fMailboxProperties.unseenunknowncount) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UnseenUnknownCount)));
                if ((pProperties & fMailboxProperties.highestmodseq) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HighestModSeq)));

                if ((pProperties & fMailboxProperties.hasbeenselected) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HasBeenSelected)));
                if ((pProperties & fMailboxProperties.hasbeenselectedforupdate) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HasBeenSelectedForUpdate)));
                if ((pProperties & fMailboxProperties.hasbeenselectedreadonly) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HasBeenSelectedReadOnly)));
                if ((pProperties & fMailboxProperties.uidnotsticky) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UIDNotSticky)));
                if ((pProperties & fMailboxProperties.messageflags) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.MessageFlags)));
                if ((pProperties & fMailboxProperties.forupdatepermanentflags) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ForUpdatePermanentFlags)));
                if ((pProperties & fMailboxProperties.readonlypermanentflags) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ReadOnlyPermanentFlags)));

                if ((pProperties & fMailboxProperties.isselected) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsSelected)));
                if ((pProperties & fMailboxProperties.isselectedforupdate) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsSelectedForUpdate)));
                if ((pProperties & fMailboxProperties.isaccessreadonly) != 0) ZInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsAccessReadOnly)));

                ZInvokeAndForget(lContext);
            }

            public void InvokeMailboxMessageDelivery(iMailboxHandle pHandle, cMessageHandleList pHandles, cTrace.cContext pParentContext)
            {
                if (MailboxMessageDelivery == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeMailboxMessageDelivery), pHandle, pHandles);
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new cMailboxMessageDeliveryEventArgs(pHandle, pHandles), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeMessagePropertyChanged(iMessageHandle pHandle, string pPropertyName, cTrace.cContext pParentContext)
            {
                if (MessagePropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeMessagePropertyChanged), pHandle, pPropertyName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new cMessagePropertyChangedEventArgs(pHandle, pPropertyName), lContext);
            }

            public void InvokeMessagePropertiesChanged(iMessageHandle pHandle, fMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                if (MessagePropertyChanged == null || pProperties == 0) return; // pre-checks for efficiency

                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeMessagePropertiesChanged), pHandle, pProperties);

                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));

                if ((pProperties & fMessageProperties.flags) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Flags)));
                if ((pProperties & fMessageProperties.answered) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Answered)));
                if ((pProperties & fMessageProperties.flagged) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Flagged)));
                if ((pProperties & fMessageProperties.deleted) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Deleted)));
                if ((pProperties & fMessageProperties.seen) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Seen)));
                if ((pProperties & fMessageProperties.draft) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Draft)));
                if ((pProperties & fMessageProperties.recent) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Recent)));
                // see comments elsewhere as to why this is commented out
                //if ((pProperties & fMessageProperties.mdnsent) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.MDNSent)));
                if ((pProperties & fMessageProperties.forwarded) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Forwarded)));
                if ((pProperties & fMessageProperties.submitpending) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.SubmitPending)));
                if ((pProperties & fMessageProperties.submitted) != 0) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Submitted)));
                if (((pProperties & fMessageProperties.modseq) != 0)) ZInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.ModSeq)));

                ZInvokeAndForget(lContext);
            }

            public void InvokeActionInt(Action<int> pAction, int pInt, cTrace.cContext pParentContext)
            {
                if (pAction == null) return;
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeActionInt), pInt);
                if (mDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                ZInvokeAndForget(new cActionIntEventArgs(pAction, pInt), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            private async Task ZInvokeAndWaitAsync(EventArgs pEventArgs, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(ZInvokeAndWaitAsync));

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

            private void ZInvokeAndForget(EventArgs pEventArgs, cTrace.cContext pParentContext)
            {
                mInvokes.Enqueue(new sInvoke(pEventArgs));
                ZInvokeAndForget(pParentContext);
            }

            private void ZInvokeAndForgetEnqueue(EventArgs pEventArgs) => mInvokes.Enqueue(new sInvoke(pEventArgs));

            private void ZInvokeAndForget(cTrace.cContext pParentContext)
            { 
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(ZInvokeAndForget));

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
                            case cMessagePropertyChangedEventArgs lEventArgs:

                                MessagePropertyChanged?.Invoke(mSender, lEventArgs);
                                break;

                            case cMailboxPropertyChangedEventArgs lEventArgs:

                                MailboxPropertyChanged?.Invoke(mSender, lEventArgs);
                                break;

                            case PropertyChangedEventArgs lEventArgs:

                                PropertyChanged?.Invoke(mSender, lEventArgs);
                                break;

                            case cResponseTextEventArgs lEventArgs:

                                ResponseText?.Invoke(mSender, lEventArgs);
                                break;

                            case cNetworkReceiveEventArgs lEventArgs:

                                NetworkReceive?.Invoke(mSender, lEventArgs);
                                break;

                            case cNetworkSendEventArgs lEventArgs:

                                NetworkSend?.Invoke(mSender, lEventArgs);
                                break;

                            case cMailboxMessageDeliveryEventArgs lEventArgs:

                                MailboxMessageDelivery?.Invoke(mSender, lEventArgs);
                                break;

                            case cActionIntEventArgs lEventArgs:

                                lEventArgs.Action(lEventArgs.Int);
                                break;

                            default:

                                lContext.TraceError("unknown event type", lInvoke.EventArgs);
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
        }

        public struct sEventSubscriptionCounts
        {
            public int PropertyChanged;
            public int ResponseText;
            public int NetworkReceive;
            public int NetworkSend;
            public int MailboxPropertyChanged;
            public int MailboxMessageDelivery;
            public int MessagePropertyChanged;
            public int CallbackException;

            public override string ToString() => $"{nameof(PropertyChanged)}:{PropertyChanged} {nameof(ResponseText)}:{ResponseText} {nameof(NetworkReceive)}:{NetworkReceive} {nameof(NetworkSend)}:{NetworkSend} {nameof(MailboxPropertyChanged)}:{MailboxPropertyChanged} {nameof(MailboxMessageDelivery)}:{MailboxMessageDelivery} {nameof(MessagePropertyChanged)}:{MessagePropertyChanged} {nameof(CallbackException)}:{CallbackException}";
        }
    }
}