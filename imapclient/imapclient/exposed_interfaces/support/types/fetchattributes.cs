using System;

namespace work.bacome.imapclient.support
{
    [Flags]
    public enum fFetchAttributes
    {
        flags = 1 << 0,
        envelope = 1 << 1,
        received = 1 << 2,
        size = 1 << 3,
        body = 1 << 4,
        bodystructure = 1 << 5,
        uid = 1 << 6,
        modseq = 1 << 7,
        allmask = 0b11111111,
        // macros from rfc3501
        macrofast = flags | received | size,
        macroall = flags | envelope | received | size,
        macrofull = flags | envelope | received | size | body
    }

    public class cFetchAttributes
    {
        public readonly fFetchAttributes Attributes;
        public readonly cHeaderNames Names;

        public cFetchAttributes(fFetchAttributes pAttributes, cHeaderNames pNames)
        {
            Attributes = pAttributes;
            Names = pNames ?? throw new ArgumentNullException(nameof(pNames));
        }

        public bool IsNone => Attributes == 0 && Names.Count == 0;

        public override bool Equals(object pObject) => this == pObject as cFetchAttributes;

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

        public override string ToString() => $"{nameof(cFetchAttributes)}({Attributes},{Names})";

        public static bool operator ==(cFetchAttributes pA, cFetchAttributes pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Attributes == pB.Attributes && pA.Names == pB.Names;
        }

        public static bool operator !=(cFetchAttributes pA, cFetchAttributes pB) => !(pA == pB);
    }
}
