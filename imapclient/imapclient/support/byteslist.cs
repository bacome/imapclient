using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    internal class cByteList : List<byte>
    {
        public cByteList() { }
        public cByteList(IList<byte> pBytes) : base(pBytes) { }
        public cByteList(int pInitialCapacity) : base(pInitialCapacity) { }
        public override string ToString() => cTools.BytesToLoggableString(nameof(cByteList), this, 1000);
        public string ToString(int pMaxLength) => cTools.BytesToLoggableString(nameof(cByteList), this, pMaxLength);
    }
}