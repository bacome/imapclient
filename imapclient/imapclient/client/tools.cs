using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private fMailboxProperties ZMailboxPropertiesToSupportedMailboxProperties(cCapability pCapability, fMailboxProperties pProperties)
        {
            fMailboxProperties lProperties = pProperties;
            if (!pCapability.Children) lProperties = lProperties & ~fMailboxProperties.haschildren;
            if (!pCapability.SpecialUse) lProperties = lProperties & ~fMailboxProperties.specialuse;
            return lProperties;
        }

        public static fMailboxFlags MailboxPropertiesToMailboxFlags(fMailboxProperties pProperties)
        {
            fMailboxFlags lResult = 0;

            if ((pProperties & fMailboxProperties.canhavechildren) != 0) lResult |= fMailboxFlags.noinferiors;
            if ((pProperties & fMailboxProperties.haschildren) != 0) lResult |= fMailboxFlags.haschildren | fMailboxFlags.hasnochildren;
            if ((pProperties & fMailboxProperties.canselect) != 0) lResult |= fMailboxFlags.noselect;
            if ((pProperties & fMailboxProperties.ismarked) != 0) lResult |= fMailboxFlags.marked | fMailboxFlags.unmarked;
            if ((pProperties & fMailboxProperties.issubscribed) != 0) lResult |= fMailboxFlags.subscribed;
            if ((pProperties & fMailboxProperties.hassubscribedchildren) != 0) lResult |= fMailboxFlags.hassubscribedchildren;
            if ((pProperties & fMailboxProperties.islocal) != 0) lResult |= fMailboxFlags.local;
            if ((pProperties & fMailboxProperties.containsall) != 0) lResult |= fMailboxFlags.all;
            if ((pProperties & fMailboxProperties.isarchive) != 0) lResult |= fMailboxFlags.archive;
            if ((pProperties & fMailboxProperties.containsdrafts) != 0) lResult |= fMailboxFlags.drafts;
            if ((pProperties & fMailboxProperties.containsflagged) != 0) lResult |= fMailboxFlags.flagged;
            if ((pProperties & fMailboxProperties.containsjunk) != 0) lResult |= fMailboxFlags.junk;
            if ((pProperties & fMailboxProperties.containssent) != 0) lResult |= fMailboxFlags.sent;
            if ((pProperties & fMailboxProperties.containstrash) != 0) lResult |= fMailboxFlags.trash;

            return lResult;
        }
    }
}
