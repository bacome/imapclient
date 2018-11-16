using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    public partial class cSequenceSet
    {
        private static int ZASCIILength(uint pUInt)
        {
            if (pUInt < 10) return 1;
            if (pUInt < 100) return 2;
            if (pUInt < 1000) return 3;
            if (pUInt < 10000) return 4;
            if (pUInt < 100000) return 5;
            if (pUInt < 1000000) return 6;
            if (pUInt < 10000000) return 7;
            if (pUInt < 100000000) return 8;
            if (pUInt < 1000000000) return 9;
            return 10;
        }
    }
}