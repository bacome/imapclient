using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient.support
{
    public class cBytes : ReadOnlyCollection<byte>
    {
        public cBytes(IList<byte> pBytes) : base(pBytes) { }

        public cBytes(string pString) : base(ZCtor(pString)) { } // required for static constants

        private static cByteList ZCtor(string pString)
        {
            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
                if (lChar > cChar.FF) throw new ArgumentOutOfRangeException(nameof(pString));
                else lBytes.Add((byte)lChar);

            return lBytes;
        }

        public override string ToString() => cTools.BytesToLoggableString(nameof(cBytes), this);
    }
}