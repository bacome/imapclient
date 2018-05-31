﻿using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    internal class cSectionCacheNonPersistentKey : IEquatable<cSectionCacheNonPersistentKey>
    {
        public readonly cIMAPClient Client;
        public readonly iMessageHandle MessageHandle;
        public readonly cSectionCacheMailboxId MailboxId;
        public readonly cSection Section;
        public readonly eDecodingRequired Decoding;

        public cSectionCacheNonPersistentKey(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            MailboxId = new cSectionCacheMailboxId(pMessageHandle.MessageCache.MailboxHandle);
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            Decoding = pDecoding;
        }

        /*
        public bool IsValidToCache
        {
            get
            {
                if (MessageHandle.Expunged) return false;
                return ReferenceEquals(Client.SelectedMailboxDetails?.MessageCache, MessageHandle.MessageCache);
            }
        } */

        public bool UIDNotSticky => MessageHandle.MessageCache.MailboxHandle.SelectedProperties.UIDNotSticky ?? true;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCacheNonPersistentKey pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cSectionCachePersistentKey pObject)
        {
            if (pObject == null) return false;
            return pObject.Equals(this);
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cSectionCacheNonPersistentKey;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                lHash = lHash * 23 + MessageHandle.GetHashCode();
                lHash = lHash * 23 + Section.GetHashCode();
                lHash = lHash * 23 + Decoding.GetHashCode();

                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSectionCacheNonPersistentKey)}({MessageHandle},{Section},{Decoding})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cSectionCacheNonPersistentKey pA, cSectionCacheNonPersistentKey pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.MessageHandle == pB.MessageHandle && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cSectionCacheNonPersistentKey pA, cSectionCacheNonPersistentKey pB) => !(pA == pB);
    }
}
