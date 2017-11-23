using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// An immutable collection of bytes.
    /// </summary>
    /// <seealso cref="cNetworkSendEventArgs"/>
    public class cBytes : ReadOnlyCollection<byte>
    {
        internal cBytes(IList<byte> pBytes) : base(pBytes) { }

        internal cBytes(string pString) : base(ZCtor(pString)) { } // required for static constants

        private static cByteList ZCtor(string pString)
        {
            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
                if (lChar > cChar.FF) throw new ArgumentOutOfRangeException(nameof(pString));
                else lBytes.Add((byte)lChar);

            return lBytes;
        }

        /// <inheritdoc/>
        public override string ToString() => cTools.BytesToLoggableString(nameof(cBytes), this, 1000);

        internal string ToString(int pMaxLength) => cTools.BytesToLoggableString(nameof(cBytes), this, pMaxLength);
    }
}