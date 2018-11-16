using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient;

namespace work.bacome.imapsupport
{
    /// <summary>
    /// An immutable collection of bytes.
    /// </summary>
    [Serializable]
    public class cBytes : ReadOnlyCollection<byte>
    {
        /// <summary>
        /// An empty collection of bytes.
        /// </summary>
        public static readonly cBytes Empty = new cBytes(new byte[] { });

        /// <summary>
        /// Initialises a new instance with the specified bytes.
        /// </summary>
        /// <param name="pBytes"></param>
        public cBytes(IEnumerable<byte> pBytes) : base(new List<byte>(pBytes)) { }

        /// <summary>
        /// Initialises a new instance with the specified string.
        /// </summary>
        /// <remarks>
        /// The characters in the string must all be less than '\u0100'.
        /// </remarks>
        /// <param name="pString"></param>
        public cBytes(string pString) : base(ZCtor(pString)) { } // required for static constants

        private static List<byte> ZCtor(string pString)
        {
            var lBytes = new List<byte>(pString.Length);

            foreach (char lChar in pString)
                if (lChar > kChar.FF) throw new ArgumentOutOfRangeException(nameof(pString));
                else lBytes.Add((byte)lChar);

            return lBytes;
        }

        /// <inheritdoc/>
        public override string ToString() => cTools.BytesToLoggableString(nameof(cBytes), this, 1000);

        internal string ToString(int pMaxLength) => cTools.BytesToLoggableString(nameof(cBytes), this, pMaxLength);
    }
}