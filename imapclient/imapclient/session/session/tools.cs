using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static void ZMessagePropertiesChanged(cEventSynchroniser pEventSynchroniser, cMailboxId pMailboxId, iMessageHandle pHandle, fMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZMailboxPropertiesChanged), pMailboxId, pProperties);

                if (pEventSynchroniser == null) throw new ArgumentNullException(nameof(pEventSynchroniser));
                if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

                if (pProperties == 0 || !pEventSynchroniser.AreMessagePropertyChangedSubscriptions) return;

                if ((pProperties & fMessageProperties.isexpunged) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsExpunged), lContext);

                if ((pProperties & fMessageProperties.flags) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.Flags), lContext);
                if ((pProperties & fMessageProperties.isanswered) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsAnswered), lContext);
                if ((pProperties & fMessageProperties.isflagged) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsFlagged), lContext);
                if ((pProperties & fMessageProperties.isdeleted) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsDeleted), lContext);
                if ((pProperties & fMessageProperties.isseen) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsSeen), lContext);
                if ((pProperties & fMessageProperties.isdraft) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsDraft), lContext);
                if ((pProperties & fMessageProperties.isrecent) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsRecent), lContext);
                if ((pProperties & fMessageProperties.ismdnsent) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsMDNSent), lContext);
                if ((pProperties & fMessageProperties.isforwarded) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsForwarded), lContext);
                if ((pProperties & fMessageProperties.issubmitpending) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsSubmitPending), lContext);
                if ((pProperties & fMessageProperties.issubmitted) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.IsSubmitted), lContext);

                if ((pProperties & fMessageProperties.modseq) != 0) pEventSynchroniser.MessagePropertyChanged(pMailboxId, pHandle, nameof(cMessage.ModSeq), lContext);
            }

            private static void ZMailboxPropertiesChanged(cEventSynchroniser pEventSynchroniser, cMailboxId pMailboxId, fMailboxProperties pProperties, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZMailboxPropertiesChanged), pMailboxId, pProperties);

                if (pEventSynchroniser == null) throw new ArgumentNullException(nameof(pEventSynchroniser));
                if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

                if (pProperties == 0 || !pEventSynchroniser.AreMailboxPropertyChangedSubscriptions) return;

                if ((pProperties & fMailboxProperties.exists) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.Exists), lContext);

                if ((pProperties & fMailboxProperties.mailboxflags) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.MailboxFlags), lContext);
                if ((pProperties & fMailboxProperties.canhavechildren) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.CanHaveChildren), lContext);
                if ((pProperties & fMailboxProperties.canselect) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.CanSelect), lContext);
                if ((pProperties & fMailboxProperties.ismarked) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsMarked), lContext);
                if ((pProperties & fMailboxProperties.nonexistent) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.NonExistent), lContext);
                if ((pProperties & fMailboxProperties.issubscribed) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSubscribed), lContext);
                if ((pProperties & fMailboxProperties.isremote) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsRemote), lContext);
                if ((pProperties & fMailboxProperties.haschildren) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasChildren), lContext);
                if ((pProperties & fMailboxProperties.hassubscribedchildren) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasSubscribedChildren), lContext);
                if ((pProperties & fMailboxProperties.containsall) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsAll), lContext);
                if ((pProperties & fMailboxProperties.isarchive) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsArchive), lContext);
                if ((pProperties & fMailboxProperties.containsdrafts) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsDrafts), lContext);
                if ((pProperties & fMailboxProperties.containsflagged) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsFlagged), lContext);
                if ((pProperties & fMailboxProperties.containsjunk) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsJunk), lContext);
                if ((pProperties & fMailboxProperties.containssent) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsSent), lContext);
                if ((pProperties & fMailboxProperties.containstrash) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.ContainsTrash), lContext);

                if ((pProperties & fMailboxProperties.mailboxstatus) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.MailboxStatus), lContext);
                if ((pProperties & fMailboxProperties.messagecount) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.MessageCount), lContext);
                if ((pProperties & fMailboxProperties.recentcount) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.RecentCount), lContext);
                if ((pProperties & fMailboxProperties.uidnext) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.UIDNext), lContext);
                if ((pProperties & fMailboxProperties.newunknownuidcount) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.NewUnknownUIDCount), lContext);
                if ((pProperties & fMailboxProperties.uidvalidity) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.UIDValidity), lContext);
                if ((pProperties & fMailboxProperties.unseencount) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.UnseenCount), lContext);
                if ((pProperties & fMailboxProperties.unseenunknowncount) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.UnseenUnknownCount), lContext);
                if ((pProperties & fMailboxProperties.highestmodseq) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.HighestModSeq), lContext);

                if ((pProperties & fMailboxProperties.mailboxbeenselected) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.MailboxBeenSelected), lContext);
                if ((pProperties & fMailboxProperties.hasbeenselected) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasBeenSelected), lContext);
                if ((pProperties & fMailboxProperties.hasbeenselectedforupdate) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasBeenSelectedForUpdate), lContext);
                if ((pProperties & fMailboxProperties.hasbeenselectedreadonly) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.HasBeenSelectedReadOnly), lContext);
                if ((pProperties & fMailboxProperties.messageflags) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.MessageFlags), lContext);
                if ((pProperties & fMailboxProperties.forupdatepermanentflags) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.ForUpdatePermanentFlags), lContext);
                if ((pProperties & fMailboxProperties.readonlypermanentflags) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.ReadOnlyPermanentFlags), lContext);

                if ((pProperties & fMailboxProperties.mailboxselected) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.MailboxSelected), lContext);
                if ((pProperties & fMailboxProperties.isselected) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSelected), lContext);
                if ((pProperties & fMailboxProperties.isselectedforupdate) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsSelectedForUpdate), lContext);
                if ((pProperties & fMailboxProperties.isaccessreadonly) != 0) pEventSynchroniser.MailboxPropertyChanged(pMailboxId, nameof(cMailbox.IsAccessReadOnly), lContext);
            }
        }
    }
}