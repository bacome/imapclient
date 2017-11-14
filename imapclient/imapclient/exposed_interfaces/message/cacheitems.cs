using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A set of items that can be cached in the internal message cache. Note that the class has three implicit conversions.
    /// </summary>
    /// <seealso cref="cMessage.Fetch(cCacheItems)"/>
    /// <seealso cref="cMailbox.Message(cUID, cCacheItems)"/>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cCacheItems, cMessageFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{cUID}, cCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cIMAPClient.Fetch(System.Collections.Generic.IEnumerable{cMessage}, cCacheItems, cCacheItemFetchConfiguration)"/>
    public class cCacheItems
    {
        /// <summary>
        /// An empty set of items.
        /// </summary>
        public static readonly cCacheItems None = new cCacheItems(0, cHeaderFieldNames.None);

        /// <summary>
        /// A set of IMAP message attributes to cache.
        /// </summary>
        public readonly fCacheAttributes Attributes;

        /// <summary>
        /// A collection of header field names to cache.
        /// </summary>
        public readonly cHeaderFieldNames Names;

        /// <summary>
        /// Initialises a new instance with the specified attributes and header field names.
        /// </summary>
        /// <param name="pAttributes"></param>
        /// <param name="pNames">Can't be null, may be empty.</param>
        public cCacheItems(fCacheAttributes pAttributes, cHeaderFieldNames pNames)
        {
            Attributes = pAttributes;
            Names = pNames ?? throw new ArgumentNullException(nameof(pNames));
        }

        /// <summary>
        /// Initialises a new instance with the attributes and header field names required for the specified properties of <see cref="cMessage"/>.
        /// </summary>
        /// <param name="pProperties"></param>
        public cCacheItems(fMessageProperties pProperties)
        {
            Attributes = 0;

            // see comments elsewhere as to why mdnsent is commented out
            if ((pProperties & (fMessageProperties.flags | fMessageProperties.answered | fMessageProperties.flagged | fMessageProperties.deleted | fMessageProperties.seen | fMessageProperties.draft | fMessageProperties.recent | /* fMessageProperties.mdnsent | */ fMessageProperties.forwarded | fMessageProperties.submitpending | fMessageProperties.submitted)) != 0) Attributes |= fCacheAttributes.flags;
            if ((pProperties & (fMessageProperties.envelope | fMessageProperties.sent | fMessageProperties.subject | fMessageProperties.basesubject | fMessageProperties.from | fMessageProperties.sender | fMessageProperties.replyto | fMessageProperties.to | fMessageProperties.cc | fMessageProperties.bcc | fMessageProperties.inreplyto | fMessageProperties.messageid)) != 0) Attributes |= fCacheAttributes.envelope;
            if ((pProperties & fMessageProperties.received) != 0) Attributes |= fCacheAttributes.received;
            if ((pProperties & fMessageProperties.size) != 0) Attributes |= fCacheAttributes.size;
            if ((pProperties & fMessageProperties.bodystructure | fMessageProperties.attachments | fMessageProperties.plaintextsizeinbytes) != 0) Attributes |= fCacheAttributes.bodystructure;
            if ((pProperties & fMessageProperties.uid) != 0) Attributes |= fCacheAttributes.uid;
            if ((pProperties & fMessageProperties.modseq) != 0) Attributes |= fCacheAttributes.modseq;

            cHeaderFieldNameList lNames = new cHeaderFieldNameList();

            if ((pProperties & fMessageProperties.references) != 0) lNames.Add(kHeaderFieldName.References);
            if ((pProperties & fMessageProperties.importance) != 0) lNames.Add(kHeaderFieldName.Importance);

            Names = new cHeaderFieldNames(lNames);
        }

        /// <summary>
        /// Indicates if the set is empty.
        /// </summary>
        public bool IsNone => Attributes == 0 && Names.Count == 0;

        /// <summary>
        /// Determines whether this instance and the specified object have the same values.
        /// </summary>
        /// <param name="pObject"></param>
        /// <returns></returns>
        public override bool Equals(object pObject) => this == pObject as cCacheItems;

        /// <summary>
        /// Returns the hash code for this set.
        /// </summary>
        /// <returns></returns>
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

        /**<summary>Returns a string that represents the set.</summary>*/
        public override string ToString() => $"{nameof(cCacheItems)}({Attributes},{Names})";

        /// <summary>
        /// Determines whether two instances have the same values.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator ==(cCacheItems pA, cCacheItems pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Attributes == pB.Attributes && pA.Names == pB.Names;
        }

        /// <summary>
        /// Determines whether two instances have different values.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator !=(cCacheItems pA, cCacheItems pB) => !(pA == pB);

        /// <summary>
        /// Implicit conversion. See <see cref="cCacheItems(fCacheAttributes, cHeaderFieldNames)"/>.
        /// </summary>
        /// <param name="pAttributes"></param>
        public static implicit operator cCacheItems(fCacheAttributes pAttributes) => new cCacheItems(pAttributes, cHeaderFieldNames.None);

        /// <summary>
        /// Implicit conversion. See <see cref="cCacheItems(fCacheAttributes, cHeaderFieldNames)"/>.
        /// </summary>
        /// <param name="pNames"></param>
        public static implicit operator cCacheItems(cHeaderFieldNames pNames) => new cCacheItems(0, pNames);

        /// <summary>
        /// Implicit conversion. See <see cref="cCacheItems(fMessageProperties)"/>.
        /// </summary>
        /// <param name="pProperties"></param>
        public static implicit operator cCacheItems(fMessageProperties pProperties) => new cCacheItems(pProperties);
    }
}
