﻿using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cIMAPCallbackSynchroniser : cCallbackSynchroniser
        {
            public event EventHandler<cResponseTextEventArgs> ResponseText;
            public event EventHandler<cMailboxPropertyChangedEventArgs> MailboxPropertyChanged;
            public event EventHandler<cMailboxMessageDeliveryEventArgs> MailboxMessageDelivery;
            public event EventHandler<cMessagePropertyChangedEventArgs> MessagePropertyChanged;

            public cIMAPCallbackSynchroniser(object pSender, cTrace.cContext pParentContext) : base(pSender, pParentContext) { }

            public void InvokeResponseText(eResponseTextContext pTextContext, cResponseText pResponseText, cTrace.cContext pParentContext)
            {
                if (ResponseText == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeResponseText), pTextContext, pResponseText);
                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cResponseTextEventArgs(pTextContext, pResponseText), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeMailboxPropertiesChanged(iMailboxHandle pMailboxHandle, fMailboxProperties pProperties, cTrace.cContext pParentContext)
            {
                if (MailboxPropertyChanged == null || pProperties == 0) return; // pre-checks for efficiency

                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeMailboxPropertiesChanged), pMailboxHandle, pProperties);

                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));

                if ((pProperties & fMailboxProperties.exists) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.Exists)));

                if ((pProperties & fMailboxProperties.canhavechildren) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.CanHaveChildren)));
                if ((pProperties & fMailboxProperties.canselect) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.CanSelect)));
                if ((pProperties & fMailboxProperties.ismarked) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.IsMarked)));
                if ((pProperties & fMailboxProperties.isremote) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.IsRemote)));
                if ((pProperties & fMailboxProperties.haschildren) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.HasChildren)));
                if ((pProperties & fMailboxProperties.containsall) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.ContainsAll)));
                if ((pProperties & fMailboxProperties.isarchive) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.IsArchive)));
                if ((pProperties & fMailboxProperties.containsdrafts) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.ContainsDrafts)));
                if ((pProperties & fMailboxProperties.containsflagged) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.ContainsFlagged)));
                if ((pProperties & fMailboxProperties.containsjunk) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.ContainsJunk)));
                if ((pProperties & fMailboxProperties.containssent) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.ContainsSent)));
                if ((pProperties & fMailboxProperties.containstrash) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.ContainsTrash)));

                if ((pProperties & fMailboxProperties.issubscribed) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.IsSubscribed)));

                if ((pProperties & fMailboxProperties.messagecount) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.MessageCount)));
                if ((pProperties & fMailboxProperties.recentcount) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.RecentCount)));
                if ((pProperties & fMailboxProperties.uidnext) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.UIDNext)));
                if ((pProperties & fMailboxProperties.uidnextunknowncount) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.UIDNextUnknownCount)));
                if ((pProperties & fMailboxProperties.uidvalidity) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.UIDValidity)));
                if ((pProperties & fMailboxProperties.unseencount) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.UnseenCount)));
                if ((pProperties & fMailboxProperties.unseenunknowncount) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.UnseenUnknownCount)));
                if ((pProperties & fMailboxProperties.highestmodseq) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.HighestModSeq)));

                if ((pProperties & fMailboxProperties.hasbeenselected) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.HasBeenSelected)));
                if ((pProperties & fMailboxProperties.hasbeenselectedforupdate) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.HasBeenSelectedForUpdate)));
                if ((pProperties & fMailboxProperties.hasbeenselectedreadonly) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.HasBeenSelectedReadOnly)));
                if ((pProperties & fMailboxProperties.uidnotsticky) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.UIDNotSticky)));
                if ((pProperties & fMailboxProperties.messageflags) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.MessageFlags)));
                if ((pProperties & fMailboxProperties.forupdatepermanentflags) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.ForUpdatePermanentFlags)));
                if ((pProperties & fMailboxProperties.readonlypermanentflags) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.ReadOnlyPermanentFlags)));

                if ((pProperties & fMailboxProperties.isselected) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.IsSelected)));
                if ((pProperties & fMailboxProperties.isselectedforupdate) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.IsSelectedForUpdate)));
                if ((pProperties & fMailboxProperties.isaccessreadonly) != 0) YInvokeAndForgetEnqueue(new cMailboxPropertyChangedEventArgs(pMailboxHandle, nameof(cMailbox.IsAccessReadOnly)));

                YInvokeAndForget(lContext);
            }

            public void InvokeMailboxMessageDelivery(iMailboxHandle pMailboxHandle, cMessageHandleList pMessageHandles, cTrace.cContext pParentContext)
            {
                if (MailboxMessageDelivery == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeMailboxMessageDelivery), pMailboxHandle, pMessageHandles);
                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cMailboxMessageDeliveryEventArgs(pMailboxHandle, pMessageHandles), lContext);
                // NOTE the event is fired by parallel code in the ZInvokeEvents routine: when adding an event you must put code there also
            }

            public void InvokeMessagePropertyChanged(iMessageHandle pMessageHandle, string pPropertyName, cTrace.cContext pParentContext)
            {
                if (MessagePropertyChanged == null) return; // pre-check for efficiency only
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeMessagePropertyChanged), pMessageHandle, pPropertyName);
                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));
                YInvokeAndForget(new cMessagePropertyChangedEventArgs(pMessageHandle, pPropertyName), lContext);
            }

            public void InvokeMessagePropertiesChanged(iMessageHandle pMessageHandle, fIMAPMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                if (MessagePropertyChanged == null || pProperties == 0) return; // pre-checks for efficiency

                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(InvokeMessagePropertiesChanged), pMessageHandle, pProperties);

                if (IsDisposed) throw new ObjectDisposedException(nameof(cCallbackSynchroniser));

                if ((pProperties & fIMAPMessageProperties.flags) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Flags)));
                if ((pProperties & fIMAPMessageProperties.answered) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Answered)));
                if ((pProperties & fIMAPMessageProperties.flagged) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Flagged)));
                if ((pProperties & fIMAPMessageProperties.deleted) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Deleted)));
                if ((pProperties & fIMAPMessageProperties.seen) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Seen)));
                if ((pProperties & fIMAPMessageProperties.draft) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Draft)));
                if ((pProperties & fIMAPMessageProperties.recent) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Recent)));
                if ((pProperties & fIMAPMessageProperties.forwarded) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Forwarded)));
                if ((pProperties & fIMAPMessageProperties.submitpending) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.SubmitPending)));
                if ((pProperties & fIMAPMessageProperties.submitted) != 0) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.Submitted)));
                if (((pProperties & fIMAPMessageProperties.modseq) != 0)) YInvokeAndForgetEnqueue(new cMessagePropertyChangedEventArgs(pMessageHandle, nameof(cIMAPMessage.ModSeq)));

                YInvokeAndForget(lContext);
            }

            protected override void YInvoke(object pSender, EventArgs pEventArgs, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cCallbackSynchroniser), nameof(YInvoke));

                switch (pEventArgs)
                {
                    case cResponseTextEventArgs lEventArgs:

                        ResponseText?.Invoke(pSender, lEventArgs);
                        break;

                    case cMailboxPropertyChangedEventArgs lEventArgs:

                        MailboxPropertyChanged?.Invoke(pSender, lEventArgs);
                        break;

                    case cMailboxMessageDeliveryEventArgs lEventArgs:

                        MailboxMessageDelivery?.Invoke(pSender, lEventArgs);
                        break;

                    case cMessagePropertyChangedEventArgs lEventArgs:

                        MessagePropertyChanged?.Invoke(pSender, lEventArgs);
                        break;

                    default:

                        lContext.TraceError("unknown event type", pEventArgs);
                        break;
                }
            }
        }
    }
}