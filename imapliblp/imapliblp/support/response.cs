using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapinternals;

namespace work.bacome.imapsupport
{
    /// <summary>
    /// Represents a line of a server response.
    /// </summary>
    /// <seealso cref="cResponse"/>
    public class cResponseLine : ReadOnlyCollection<byte>
    {
        /// <summary>
        /// Indicates if the bytes were sent as a literal.
        /// </summary>
        public readonly bool Literal;

        /// <summary>
        /// Initialises a new instance with the specified data.
        /// </summary>
        /// <param name="pLiteral"></param>
        /// <param name="pBytes"></param>
        public cResponseLine(bool pLiteral, IList<byte> pBytes) : base(pBytes) { Literal = pLiteral; }

        /// <inheritdoc/>
        public override string ToString() => cTools.BytesToLoggableString(nameof(cResponseLine), this, 1000);

        /// <summary>
        /// Returns a string containing at most the specified number of bytes from the server response.
        /// </summary>
        /// <param name="pMaxLength"></param>
        /// <returns></returns>
        public string ToString(int pMaxLength) => cTools.BytesToLoggableString(nameof(cResponseLine), this, pMaxLength);
    }

    /// <summary>
    /// Represents a server response.
    /// </summary>
    public class cResponse : ReadOnlyCollection<cResponseLine>
    {
        /// <summary>
        /// Initialises a new instance with the specified data.
        /// </summary>
        /// <param name="pLines"></param>
        public cResponse(IList<cResponseLine> pLines) : base(pLines) { }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cResponse));
            foreach (var lLine in this) lBuilder.Append(lLine);
            return lBuilder.ToString();
        }
    }
}