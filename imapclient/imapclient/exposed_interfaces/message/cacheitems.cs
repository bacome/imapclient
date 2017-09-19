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

        public static cCacheItems operator |(cCacheItems pItems, fCacheAttributes pAttributes)
        {
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));
            fCacheAttributes lAttributes = pItems.Attributes | pAttributes;
            if (lAttributes == pItems.Attributes) return pItems;
            return new cCacheItems(lAttributes, pItems.Names);
        }

        public static cCacheItems operator |(cCacheItems pItems, string pName)
        {
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            cHeaderFieldNames lNames = pItems.Names | pName;
            if (lNames == pItems.Names) return pItems;
            return new cCacheItems(pItems.Attributes, lNames);
        }

        public static cCacheItems operator |(cCacheItems pItems, cHeaderFieldNames pNames)
        {
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));
            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            cHeaderFieldNames lNames = pItems.Names | pNames;
            if (lNames == pItems.Names) return pItems;
            return new cCacheItems(pItems.Attributes, lNames);
        }

        public static implicit operator cCacheItems(fCacheAttributes pAttributes) => new cCacheItems(pAttributes, cHeaderFieldNames.None);
        public static implicit operator cCacheItems(cHeaderFieldNames pNames) => new cCacheItems(0, pNames);
    }
}
