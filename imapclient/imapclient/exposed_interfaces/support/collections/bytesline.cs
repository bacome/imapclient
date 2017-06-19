using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    public class cBytesLine : ReadOnlyCollection<byte>
    {
        public readonly bool Literal;
        public cBytesLine(bool pLiteral, IList<byte> pBytes) : base(pBytes) { Literal = pLiteral; }
        public override string ToString() => cTools.BytesToLoggableString(nameof(cBytesLine), this);
    }

    public class cBytesLines : ReadOnlyCollection<cBytesLine>
    {
        public cBytesLines(IList<cBytesLine> pBytesLines) : base(pBytesLines) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBytesLines));
            foreach (var lLine in this) lBuilder.Append(lLine);
            return lBuilder.ToString();
        }
    }
}