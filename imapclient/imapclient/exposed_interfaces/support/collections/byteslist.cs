using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public class cByteList : List<byte>
    {
        public cByteList() { }
        public cByteList(IList<byte> pBytes) : base(pBytes) { }
        public cByteList(int pInitialCapacity) : base(pInitialCapacity) { }
        public override string ToString() => cTools.BytesToLoggableString(nameof(cByteList), this);
    }
}