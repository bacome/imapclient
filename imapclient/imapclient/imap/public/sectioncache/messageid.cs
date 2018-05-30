using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public class cSectionCacheMessageId : IEquatable<cSectionCacheMessageId>
    {
        public readonly cAccountId AccountId;
        public readonly cMailboxName MailboxName;
        public readonly cUID UID;

        ;?; // hashcode etc

        internal cSectionCacheMessageId(iMessageHandle pMessageHandle)
        {
            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pMessageHandle.UID == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
            AccountId = pMessageHandle.MessageCache.MailboxHandle.MailboxCache.AccountId;
            MailboxName = pMessageHandle.MessageCache.MailboxHandle.MailboxName;
            UID = pMessageHandle.UID;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionCacheMessageId)}({AccountId},{MailboxName},{UID})";
    }
}