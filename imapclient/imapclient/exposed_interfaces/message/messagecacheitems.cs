using System;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A set of items that can be cached in a message cache.
    /// </summary>
    /// <seealso cref="cIMAPClient.DefaultMessageCacheItems"/>
    /// <seealso cref="cMessage.Fetch(cMessageCacheItems)"/>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Message(cUID, cMessageCacheItems)"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{cUID}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cIMAPClient.Fetch(System.Collections.Generic.IEnumerable{cMessage}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    public class cMessageCacheItems
    {
        /// <summary>
        /// An empty set of items.
        /// </summary>
        public static readonly cMessageCacheItems None = new cMessageCacheItems(0, cHeaderFieldNames.None);

        /// <summary>
        /// The set of IMAP message attributes to cache.
        /// </summary>
        public readonly fMessageCacheAttributes Attributes;

        /// <summary>
        /// The collection of header field names to cache.
        /// </summary>
        public readonly cHeaderFieldNames Names;

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pAttributes"></param>
        /// <param name="pNames">Can't be <see langword="null"/>, may be empty.</param>
        public cMessageCacheItems(fMessageCacheAttributes pAttributes, cHeaderFieldNames pNames)
        {
            Attributes = pAttributes;
            Names = pNames ?? throw new ArgumentNullException(nameof(pNames));
        }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pProperties">The message properties to cache the backing data for.</param>
        public cMessageCacheItems(fMessageProperties pProperties)
        {
            Attributes = 0;

            // see comments elsewhere as to why mdnsent is commented out
            if ((pProperties & (fMessageProperties.flags | fMessageProperties.answered | fMessageProperties.flagged | fMessageProperties.deleted | fMessageProperties.seen | fMessageProperties.draft | fMessageProperties.recent | /* fMessageProperties.mdnsent | */ fMessageProperties.forwarded | fMessageProperties.submitpending | fMessageProperties.submitted)) != 0) Attributes |= fMessageCacheAttributes.flags;
            if ((pProperties & (fMessageProperties.envelope | fMessageProperties.sent | fMessageProperties.subject | fMessageProperties.basesubject | fMessageProperties.from | fMessageProperties.sender | fMessageProperties.replyto | fMessageProperties.to | fMessageProperties.cc | fMessageProperties.bcc | fMessageProperties.inreplyto | fMessageProperties.messageid)) != 0) Attributes |= fMessageCacheAttributes.envelope;
            if ((pProperties & fMessageProperties.received) != 0) Attributes |= fMessageCacheAttributes.received;
            if ((pProperties & fMessageProperties.size) != 0) Attributes |= fMessageCacheAttributes.size;
            if ((pProperties & fMessageProperties.bodystructure | fMessageProperties.attachments | fMessageProperties.plaintextsizeinbytes) != 0) Attributes |= fMessageCacheAttributes.bodystructure;
            if ((pProperties & fMessageProperties.uid) != 0) Attributes |= fMessageCacheAttributes.uid;
            if ((pProperties & fMessageProperties.modseq) != 0) Attributes |= fMessageCacheAttributes.modseq;

            cHeaderFieldNameList lNames = new cHeaderFieldNameList();

            if ((pProperties & fMessageProperties.references) != 0) lNames.Add(kHeaderFieldName.References);
            if ((pProperties & fMessageProperties.importance) != 0) lNames.Add(kHeaderFieldName.Importance);

            Names = new cHeaderFieldNames(lNames);
        }

        /// <summary>
        /// Indicates whether the set is empty.
        /// </summary>
        public bool IsNone => Attributes == 0 && Names.Count == 0;

        /// <inheritdoc/>
        public override bool Equals(object pObject) => this == pObject as cMessageCacheItems;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + Attributes.GetHashCode();
                lHash = lHash * 23 + Names.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cMessageCacheItems)}({Attributes},{Names})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMessageCacheItems pA, cMessageCacheItems pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Attributes == pB.Attributes && pA.Names == pB.Names;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMessageCacheItems pA, cMessageCacheItems pB) => !(pA == pB);

        /// <summary>
        /// Implicit conversion.
        /// </summary>
        /// <param name="pAttributes"></param>
        public static implicit operator cMessageCacheItems(fMessageCacheAttributes pAttributes) => new cMessageCacheItems(pAttributes, cHeaderFieldNames.None);

        /// <summary>
        /// Implicit conversion.
        /// </summary>
        /// <param name="pNames"></param>
        public static implicit operator cMessageCacheItems(cHeaderFieldNames pNames) => new cMessageCacheItems(0, pNames);

        /// <summary>
        /// Implicit conversion.
        /// </summary>
        /// <param name="pProperties"></param>
        public static implicit operator cMessageCacheItems(fMessageProperties pProperties) => new cMessageCacheItems(pProperties);
    }
}
