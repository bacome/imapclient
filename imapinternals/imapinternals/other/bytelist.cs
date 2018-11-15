using System;
using System.Collections.Generic;

namespace work.bacome.imapinternals
{
    public class cByteList : List<byte>
    {
        public cByteList() { }
        public cByteList(IList<byte> pBytes) : base(pBytes) { }
        public cByteList(int pInitialCapacity) : base(pInitialCapacity) { }

        public bool Equals(IList<byte> pBytes, bool pCaseSensitive = false)
        {
            if (pBytes.Count != Count) return false;
            for (int i = 0; i < Count; i++) if (!cASCII.Compare(this[i], pBytes[i], pCaseSensitive)) return false;
            return true;
        }

        public override string ToString() => cTools.BytesToLoggableString(nameof(cByteList), this, 1000);
        public string ToString(int pMaxLength) => cTools.BytesToLoggableString(nameof(cByteList), this, pMaxLength);
    }
}