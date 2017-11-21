using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents a line of a server response.
    /// </summary>
    /// <seealso cref="cBytesLines"/>
    public class cBytesLine : ReadOnlyCollection<byte>
    {
        internal readonly bool Literal;
        internal cBytesLine(bool pLiteral, IList<byte> pBytes) : base(pBytes) { Literal = pLiteral; }
        /// <inheritdoc/>
        public override string ToString() => cTools.BytesToLoggableString(nameof(cBytesLine), this, 1000);
        internal string ToString(int pMaxLength) => cTools.BytesToLoggableString(nameof(cBytesLine), this, pMaxLength);
    }

    /// <summary>
    /// Represents a server response.
    /// </summary>
    /// <seealso cref="cNetworkReceiveEventArgs"/>
    public class cBytesLines : ReadOnlyCollection<cBytesLine>
    {
        internal cBytesLines(IList<cBytesLine> pBytesLines) : base(pBytesLines) { }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBytesLines));
            foreach (var lLine in this) lBuilder.Append(lLine);
            return lBuilder.ToString();
        }
    }
}