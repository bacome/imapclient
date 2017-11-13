using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// A read-only collection of bytes.
    /// </summary>
    /// <seealso cref="cNetworkSendEventArgs.Buffers"/>
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

        /**<summary>Returns a string that represents the collection.</summary>*/
        public override string ToString() => cTools.BytesToLoggableString(nameof(cBytes), this, 1000);

        internal string ToString(int pMaxLength) => cTools.BytesToLoggableString(nameof(cBytes), this, pMaxLength);
    }
}