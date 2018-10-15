using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of items that can be cached in a <see cref="cIMAPClient"/> message cache.
    /// </summary>
    public class cMessageCacheItems : IEquatable<cMessageCacheItems>
    {
        /// <summary>
        /// An empty set of items.
        /// </summary>
        public static readonly cMessageCacheItems Empty = new cMessageCacheItems(0, cHeaderFieldNames.Empty);

        public static readonly cMessageCacheItems UID = new cMessageCacheItems(fMessageCacheAttributes.uid, cHeaderFieldNames.Empty);
        public static readonly cMessageCacheItems Size = new cMessageCacheItems(fMessageCacheAttributes.size, cHeaderFieldNames.Empty);
        public static readonly cMessageCacheItems BodyStructure = new cMessageCacheItems(fMessageCacheAttributes.bodystructure, cHeaderFieldNames.Empty);

        /// <summary>
        /// The IMAP message attributes to cache.
        /// </summary>
        public readonly fMessageCacheAttributes Attributes;

        /// <summary>
        /// The header field names to cache.
        /// </summary>
        public readonly cHeaderFieldNames Names;

        /// <summary>
        /// Initialises a new instance using the specified IMAP message attributes and header field names.
        /// </summary>
        /// <param name="pAttributes"></param>
        /// <param name="pNames">Can't be <see langword="null"/>, may be empty.</param>
        public cMessageCacheItems(fMessageCacheAttributes pAttributes, cHeaderFieldNames pNames)
        {
            Attributes = pAttributes;
            Names = pNames ?? throw new ArgumentNullException(nameof(pNames));
        }

        /// <summary>
        /// Initialises a new instance using the specified <see cref="cIMAPMessage"/> properties.
        /// </summary>
        /// <param name="pProperties"></param>
        public cMessageCacheItems(fIMAPMessageProperties pProperties)
        {
            Attributes = 0;

            if ((pProperties & (fIMAPMessageProperties.messageuid | fIMAPMessageProperties.uid)) != 0) Attributes |= fMessageCacheAttributes.uid;
            if ((pProperties & (fIMAPMessageProperties.modseqflags | fIMAPMessageProperties.flags | fIMAPMessageProperties.answered | fIMAPMessageProperties.flagged | fIMAPMessageProperties.deleted | fIMAPMessageProperties.seen | fIMAPMessageProperties.draft | fIMAPMessageProperties.recent | fIMAPMessageProperties.forwarded | fIMAPMessageProperties.submitpending | fIMAPMessageProperties.submitted)) != 0) Attributes |= fMessageCacheAttributes.modseqflags;
            if ((pProperties & (fIMAPMessageProperties.envelope | fIMAPMessageProperties.sent | fIMAPMessageProperties.subject | fIMAPMessageProperties.basesubject | fIMAPMessageProperties.from | fIMAPMessageProperties.sender | fIMAPMessageProperties.replyto | fIMAPMessageProperties.to | fIMAPMessageProperties.cc | fIMAPMessageProperties.bcc | fIMAPMessageProperties.inreplyto | fIMAPMessageProperties.messageid)) != 0) Attributes |= fMessageCacheAttributes.envelope;
            if ((pProperties & fIMAPMessageProperties.received) != 0) Attributes |= fMessageCacheAttributes.received;
            if ((pProperties & fIMAPMessageProperties.size) != 0) Attributes |= fMessageCacheAttributes.size;
            if ((pProperties & fIMAPMessageProperties.bodystructure | fIMAPMessageProperties.format | fIMAPMessageProperties.plaintextsizeinbytes) != 0) Attributes |= fMessageCacheAttributes.bodystructure;

            cHeaderFieldNameList lNames = new cHeaderFieldNameList();

            if ((pProperties & fIMAPMessageProperties.references) != 0) lNames.Add(kHeaderFieldName.References);
            if ((pProperties & fIMAPMessageProperties.importance) != 0) lNames.Add(kHeaderFieldName.Importance);

            Names = new cHeaderFieldNames(lNames);
        }

        /// <summary>
        /// Indicates whether the set is empty.
        /// </summary>
        public bool IsEmpty => Attributes == 0 && Names.Count == 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cMessageCacheItems pObject) => this == pObject;

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
            return pA.Attributes == pB.Attributes && pA.Names.Equals(pB.Names);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMessageCacheItems pA, cMessageCacheItems pB) => !(pA == pB);

        /// <summary>
        /// Returns a new instance initialised with the specified IMAP message attributes.
        /// </summary>
        /// <param name="pAttributes"></param>
        public static implicit operator cMessageCacheItems(fMessageCacheAttributes pAttributes) => new cMessageCacheItems(pAttributes, cHeaderFieldNames.Empty);

        /// <summary>
        /// Returns a new instance initialised with the specified header field names.
        /// </summary>
        /// <param name="pNames"></param>
        public static implicit operator cMessageCacheItems(cHeaderFieldNames pNames) => new cMessageCacheItems(0, pNames);

        /// <summary>
        /// Returns a new instance initialised with the specified <see cref="cIMAPMessage"/> properties.
        /// </summary>
        /// <param name="pProperties"></param>
        public static implicit operator cMessageCacheItems(fIMAPMessageProperties pProperties) => new cMessageCacheItems(pProperties);
    }
}
