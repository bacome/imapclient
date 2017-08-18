using System;
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
        private sealed class cEventSynchroniser : IDisposable
        {
            public event PropertyChangedEventHandler PropertyChanged;
            public event EventHandler<cResponseTextEventArgs> ResponseText;
            public event EventHandler<cNetworkActivityEventArgs> NetworkActivity;
            public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged;
            public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery;
            public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged;

            private bool mDisposed = false;

            private readonly object mSender;
            private readonly CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource(); // for use when disposing
            private readonly cReleaser mForegroundReleaser;
            private readonly cReleaser mBackgroundReleaser;
            private readonly Task mBackgroundTask;
            private readonly ConcurrentQueue<sEvent> mEvents = new ConcurrentQueue<sEvent>();
            private volatile bool mOutstandingPost = false;

            public cEventSynchroniser(object pSender, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewObject(nameof(cEventSynchroniser));
                mSender = pSender;
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

            public void FirePropertyChanged(string pPropertyName, cTrace.cContext pParentContext)
            {
                if (PropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(FirePropertyChanged), pPropertyName);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new PropertyChangedEventArgs(pPropertyName), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void FireResponseText(eResponseTextType pTextType, cResponseText pResponseText, cTrace.cContext pParentContext)
            {
                if (ResponseText == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(FireResponseText), pTextType, pResponseText);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new cResponseTextEventArgs(pTextType, pResponseText), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            private const int kNetworkActivityMaxLength = 40;

            public void FireNetworkActivity(cBytesLines pResponse, cTrace.cContext pParentContext)
            {
                if (NetworkActivity == null) return; // pre-check for efficiency only

                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(FireNetworkActivity));

                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));

                if (pResponse == null) throw new ArgumentNullException(nameof(pResponse));
                if (pResponse.Count == 0) throw new ArgumentOutOfRangeException(nameof(pResponse));

                char[] lChars = new char[kNetworkActivityMaxLength + 3];
                int lCharCount = 0;

                foreach (var lByte in pResponse[0])
                {
                    if (lByte < cASCII.SPACE || lByte > cASCII.TILDA) break;
                    lChars[lCharCount++] = (char)lByte;
                    if (lCharCount == kNetworkActivityMaxLength) break;
                }

                if (pResponse.Count > 1 || lCharCount < pResponse[0].Count) for (int i = 0; i < 3; i++) lChars[lCharCount++] = '.';

                ZFireAndForget(new cNetworkActivityEventArgs(eNetworkActivitySource.server, new string(lChars, 0, lCharCount)), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void FireNetworkActivity(IList<byte> pSending, cTrace.cContext pParentContext)
            {
                if (NetworkActivity == null) return; // pre-check for efficiency only

                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(FireNetworkActivity));

                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));

                if (pSending == null) throw new ArgumentNullException(nameof(pSending));
                if (pSending.Count == 0) throw new ArgumentOutOfRangeException(nameof(pSending));

                char[] lChars = new char[kNetworkActivityMaxLength + 3];
                int lCharCount = 0;

                foreach (var lByte in pSending)
                {
                    if (lByte < cASCII.SPACE || lByte > cASCII.TILDA) break;
                    lChars[lCharCount++] = (char)lByte;
                    if (lCharCount == kNetworkActivityMaxLength) break;
                }

                if (lCharCount < pSending.Count) for (int i = 0; i < 3; i++) lChars[lCharCount++] = '.';

                ZFireAndForget(new cNetworkActivityEventArgs(eNetworkActivitySource.client, new string(lChars, 0, lCharCount)), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void FireMailboxPropertiesChanged(iMailboxHandle pHandle, fMailboxProperties pProperties, cTrace.cContext pParentContext)
            {
                if (MailboxPropertyChanged == null | pProperties == 0) return; // pre-checks for efficiency

                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(FireMailboxPropertiesChanged), pHandle, pProperties);

                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));

                if ((pProperties & fMailboxProperties.exists) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.Exists)));

                if ((pProperties & fMailboxProperties.canhavechildren) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.CanHaveChildren)));
                if ((pProperties & fMailboxProperties.canselect) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.CanSelect)));
                if ((pProperties & fMailboxProperties.ismarked) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsMarked)));
                if ((pProperties & fMailboxProperties.isremote) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsRemote)));
                if ((pProperties & fMailboxProperties.haschildren) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HasChildren)));
                if ((pProperties & fMailboxProperties.containsall) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsAll)));
                if ((pProperties & fMailboxProperties.isarchive) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsArchive)));
                if ((pProperties & fMailboxProperties.containsdrafts) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsDrafts)));
                if ((pProperties & fMailboxProperties.containsflagged) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsFlagged)));
                if ((pProperties & fMailboxProperties.containsjunk) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsJunk)));
                if ((pProperties & fMailboxProperties.containssent) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsSent)));
                if ((pProperties & fMailboxProperties.containstrash) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ContainsTrash)));

                if ((pProperties & fMailboxProperties.issubscribed) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsSubscribed)));

                if ((pProperties & fMailboxProperties.messagecount) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.MessageCount)));
                if ((pProperties & fMailboxProperties.recentcount) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.RecentCount)));
                if ((pProperties & fMailboxProperties.uidnext) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UIDNext)));
                if ((pProperties & fMailboxProperties.uidnextunknowncount) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UIDNextUnknownCount)));
                if ((pProperties & fMailboxProperties.uidvalidity) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UIDValidity)));
                if ((pProperties & fMailboxProperties.unseencount) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UnseenCount)));
                if ((pProperties & fMailboxProperties.unseenunknowncount) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.UnseenUnknownCount)));
                if ((pProperties & fMailboxProperties.highestmodseq) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HighestModSeq)));

                if ((pProperties & fMailboxProperties.hasbeenselected) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HasBeenSelected)));
                if ((pProperties & fMailboxProperties.hasbeenselectedforupdate) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HasBeenSelectedForUpdate)));
                if ((pProperties & fMailboxProperties.hasbeenselectedreadonly) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.HasBeenSelectedReadOnly)));
                if ((pProperties & fMailboxProperties.messageflags) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.MessageFlags)));
                if ((pProperties & fMailboxProperties.forupdatepermanentflags) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ForUpdatePermanentFlags)));
                if ((pProperties & fMailboxProperties.readonlypermanentflags) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.ReadOnlyPermanentFlags)));

                if ((pProperties & fMailboxProperties.isselected) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsSelected)));
                if ((pProperties & fMailboxProperties.isselectedforupdate) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsSelectedForUpdate)));
                if ((pProperties & fMailboxProperties.isaccessreadonly) != 0) ZFireAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pHandle, nameof(cMailbox.IsAccessReadOnly)));

                ZFireAndForget(lContext);
            }

            public void FireMailboxMessageDelivery(iMailboxHandle pHandle, cMessageHandleList pHandles, cTrace.cContext pParentContext)
            {
                if (MailboxMessageDelivery == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(FireMailboxMessageDelivery), pHandle, pHandles);
                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));
                ZFireAndForget(new cMailboxMessageDeliveryEventArgs(pHandle, pHandles), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void FireMessagePropertiesChanged(iMessageHandle pHandle, fMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                if (MessagePropertyChanged == null | pProperties == 0) return; // pre-checks for efficiency

                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(FireMessagePropertiesChanged), pHandle, pProperties);

                if (mDisposed) throw new ObjectDisposedException(nameof(cEventSynchroniser));

                if ((pProperties & fMessageProperties.isexpunged) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsExpunged)));

                if ((pProperties & fMessageProperties.flags) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.Flags)));
                if ((pProperties & fMessageProperties.isanswered) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsAnswered)));
                if ((pProperties & fMessageProperties.isflagged) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsFlagged)));
                if ((pProperties & fMessageProperties.isdeleted) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsDeleted)));
                if ((pProperties & fMessageProperties.isseen) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsSeen)));
                if ((pProperties & fMessageProperties.isdraft) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsDraft)));
                if ((pProperties & fMessageProperties.isrecent) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsRecent)));
                if ((pProperties & fMessageProperties.ismdnsent) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsMDNSent)));
                if ((pProperties & fMessageProperties.isforwarded) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsForwarded)));
                if ((pProperties & fMessageProperties.issubmitpending) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsSubmitPending)));
                if ((pProperties & fMessageProperties.issubmitted) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.IsSubmitted)));

                if ((pProperties & fMessageProperties.modseq) != 0) ZFireAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pHandle, nameof(cMessage.ModSeq)));

                ZFireAndForget(lContext);
            }

            public void FireIncrementProgress(Action<int> pIncrementProgress, int pValue, cTrace.cContext pParentContext)
            {
                if (pIncrementProgress == null) return;
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(FireIncrementProgress), pValue);
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
                mEvents.Enqueue(new sEvent(pEventArgs));
                ZFireAndForget(pParentContext);
            }

            private void ZFireAndForgetEnqueue(EventArgs pEventArgs) => mEvents.Enqueue(new sEvent(pEventArgs));

            private void ZFireAndForget(cTrace.cContext pParentContext)
            { 
                var lContext = pParentContext.NewMethod(nameof(cEventSynchroniser), nameof(ZFireAndForget));

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

                            case cNetworkActivityEventArgs lEventArgs:

                                NetworkActivity?.Invoke(mSender, lEventArgs);
                                break;

                            case cMailboxMessageDeliveryEventArgs lEventArgs:

                                MailboxMessageDelivery?.Invoke(mSender, lEventArgs);
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
