using System;

namespace work.bacome.imapclient
{
    public class cCacheItems
    {
        public static readonly cCacheItems None = new cCacheItems(0, cHeaderFieldNames.None);

        public readonly fCacheAttributes Attributes;
        public readonly cHeaderFieldNames Names;

        public cCacheItems(fCacheAttributes pAttributes, cHeaderFieldNames pNames)
        {
            Attributes = pAttributes;
            Names = pNames ?? throw new ArgumentNullException(nameof(pNames));
        }

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

        public bool IsNone => Attributes == 0 && Names.Count == 0;

        public override bool Equals(object pObject) => this == pObject as cCacheItems;

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

        public override string ToString() => $"{nameof(cCacheItems)}({Attributes},{Names})";

        public static bool operator ==(cCacheItems pA, cCacheItems pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Attributes == pB.Attributes && pA.Names == pB.Names;
        }

        public static bool operator !=(cCacheItems pA, cCacheItems pB) => !(pA == pB);

        public static implicit operator cCacheItems(fCacheAttributes pAttributes) => new cCacheItems(pAttributes, cHeaderFieldNames.None);
        public static implicit operator cCacheItems(cHeaderFieldNames pNames) => new cCacheItems(0, pNames);
        public static implicit operator cCacheItems(fMessageProperties pProperties) => new cCacheItems(pProperties);
    }
}
