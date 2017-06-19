using System;

namespace work.bacome.imapclient
{
    public class cMailboxListItem
    {
        public readonly cMailbox Mailbox;
        public readonly cMailboxFlags Flags;
        public readonly cMailboxStatus Status; // may be null

        public cMailboxListItem(cMailbox pMailbox, fMailboxFlags pFlags, cMailboxStatus pStatus)
        {
            Mailbox = pMailbox ?? throw new ArgumentNullException(nameof(pMailbox));
            Flags = new cMailboxFlags(pFlags);
            Status = pStatus;
        }

        public string Name => Mailbox.MailboxId.MailboxName.Name;

        public bool? CanHaveChildren => Flags.CanHaveChildren;
        public bool? HasChildren => Flags.HasChildren;
        public bool? CanSelect => Flags.CanSelect;
        public bool? IsMarked => Flags.IsMarked;
        public bool? IsSubscribed => Flags.IsSubscribed;
        public bool? HasSubscribedChildren => Flags.HasSubscribedChildren;
        public bool? IsRemote => Flags.IsRemote;
        public bool ContainsAll => Flags.ContainsAll;
        public bool ContainsArchived => Flags.ContainsArchived;
        public bool ContainsDrafts => Flags.ContainsDrafts;
        public bool ContainsFlagged => Flags.ContainsFlagged;
        public bool ContainsJunk => Flags.ContainsJunk;
        public bool ContainsSent => Flags.ContainsSent;
        public bool ContainsTrash => Flags.ContainsTrash;

        public int? Messages => Status.Messages;
        public int? Recent => Status.Recent;
        public uint? UIDNext => Status.UIDNext;
        public uint? UIDValidity => Status.UIDValidity;
        public int? Unseen => Status.Unseen;

        public override string ToString() => $"{nameof(cMailboxListItem)}({Mailbox},{Flags},{Status})";
    }
}