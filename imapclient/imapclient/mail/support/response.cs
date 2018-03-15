using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.mailclient.support
{
    /// <summary>
    /// Represents a line of a server response.
    /// </summary>
    /// <seealso cref="cResponse"/>
    public class cResponseLine : ReadOnlyCollection<byte>
    {
        internal readonly bool Literal;
        internal cResponseLine(bool pLiteral, IList<byte> pBytes) : base(pBytes) { Literal = pLiteral; }
        /// <inheritdoc/>
        public override string ToString() => cTools.BytesToLoggableString(nameof(cResponseLine), this, 1000);
        internal string ToString(int pMaxLength) => cTools.BytesToLoggableString(nameof(cResponseLine), this, pMaxLength);
    }

    /// <summary>
    /// Represents a server response.
    /// </summary>
    /// <seealso cref="cNetworkReceiveEventArgs"/>
    public class cResponse : ReadOnlyCollection<cResponseLine>
    {
        internal cResponse(IList<cResponseLine> pLines) : base(pLines) { }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cResponse));
            foreach (var lLine in this) lBuilder.Append(lLine);
            return lBuilder.ToString();
        }
    }
}