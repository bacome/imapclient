using System;
using System.Collections.Generic;
using System.IO;

namespace work.bacome.imapclient.support
{
    internal abstract class cCommandPart
    {
        public static readonly cCommandPart Space = new cTextCommandPart(" ");
        public static readonly cCommandPart Nil = new cTextCommandPart("NIL");
        public static readonly cCommandPart NilSecret = new cTextCommandPart("NIL", true);
        public static readonly cCommandPart LParen = new cTextCommandPart("(");
        public static readonly cCommandPart RParen = new cTextCommandPart(")");
        public static readonly cCommandPart RBracket = new cTextCommandPart("]");
        public static readonly cCommandPart Dot = new cTextCommandPart(".");
        public static readonly cCommandPart Inbox = new cTextCommandPart(cMailboxName.InboxBytes);

        public readonly bool Secret;
        public readonly bool Encoded;

        public cCommandPart(bool pSecret, bool pEncoded)
        {
            Secret = pSecret;
            Encoded = pEncoded;
        }
    }

    internal abstract class cLiteralCommandPartBase : cCommandPart
    {
        public readonly bool Binary;

        public cLiteralCommandPartBase(bool pBinary, bool pSecret, bool pEncoded) : base(pSecret, pEncoded)
        {
            Binary = pBinary;
        }

        public abstract int Length { get; }
    }

    internal class cStreamCommandPart : cLiteralCommandPartBase
    {
        public readonly Stream Stream;
        private readonly int mLength;
        public readonly cBatchSizerConfiguration ReadConfiguration;

        public cStreamCommandPart(Stream pStream, int pLength, bool pBinary, cBatchSizerConfiguration pReadConfiguration) : base(pBinary, false, false)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (pReadConfiguration == null) throw new ArgumentNullException(nameof(pReadConfiguration));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            Stream = pStream;
            mLength = pLength;
            ReadConfiguration = pReadConfiguration;
        }

        public override int Length => mLength;

        public override string ToString()
        {
            if (Secret) return $"{nameof(cStreamCommandPart)}({ReadConfiguration})";
            else return $"{nameof(cStreamCommandPart)}({Length},{Binary},{Encoded},{ReadConfiguration})";
        }
    }

    internal class cLiteralCommandPart : cLiteralCommandPartBase
    {
        public readonly cBytes Bytes;

        public cLiteralCommandPart(IList<byte> pBytes, bool pBinary = false, bool pSecret = false, bool pEncoded = false) : base(pBinary, pSecret, pEncoded)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            Bytes = new cBytes(pBytes);
        }

        public override int Length => Bytes.Count;

        public override string ToString()
        {
            if (Secret) return $"{nameof(cLiteralCommandPart)}()";
            else return $"{nameof(cLiteralCommandPart)}({Bytes},{Binary},{Encoded})";
        }
    }

    internal class cTextCommandPart : cCommandPart
    {
        public readonly cBytes Bytes;

        public cTextCommandPart(IList<byte> pBytes, bool pSecret = false, bool pEncoded = false) : base(pSecret, pEncoded)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            Bytes = new cBytes(pBytes);
        }

        public cTextCommandPart(string pString, bool pSecret = false) : base(pSecret, false)
        {
            if (string.IsNullOrEmpty(pString)) throw new ArgumentOutOfRangeException(nameof(pString));

            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
            {
                if (lChar < ' ' || lChar > '~') throw new ArgumentOutOfRangeException(nameof(pString));
                lBytes.Add((byte)lChar);
            }

            Bytes = new cBytes(lBytes);
        }

        public cTextCommandPart(uint pNumber) : base(false, false)
        {
            var lBytes = cTools.UIntToBytesReverse(pNumber);
            lBytes.Reverse();
            Bytes = new cBytes(lBytes);
        }

        public cTextCommandPart(ulong pNumber) : base(false, false)
        {
            var lBytes = cTools.ULongToBytesReverse(pNumber);
            lBytes.Reverse();
            Bytes = new cBytes(lBytes);
        }

        public cTextCommandPart(cSequenceSet pSequenceSet) : base(false, false)
        {
            cByteList lBytes = new cByteList();
            cByteList lTemp = new cByteList();

            bool lFirst = true;

            foreach (var lItem in pSequenceSet)
            {
                if (lFirst) lFirst = false;
                else lBytes.Add(cASCII.COMMA);

                if (lItem == cSequenceSetItem.Asterisk)
                {
                    lBytes.Add(cASCII.ASTERISK);
                    continue;
                }

                if (lItem is cSequenceSetNumber lNumber)
                {
                    lTemp = cTools.UIntToBytesReverse(lNumber.Number);
                    lTemp.Reverse();
                    lBytes.AddRange(lTemp);
                    continue;
                }

                if (!(lItem is cSequenceSetRange lRange)) throw new ArgumentException("invalid form 1", nameof(pSequenceSet));

                if (lRange.From == cSequenceSetItem.Asterisk)
                {
                    lBytes.Add(cASCII.ASTERISK);
                    continue;
                }

                if (!(lRange.From is cSequenceSetNumber lFrom)) throw new ArgumentException("invalid form 2", nameof(pSequenceSet));

                lTemp = cTools.UIntToBytesReverse(lFrom.Number);
                lTemp.Reverse();
                lBytes.AddRange(lTemp);

                lBytes.Add(cASCII.COLON);

                if (lRange.To == cSequenceSetItem.Asterisk)
                {
                    lBytes.Add(cASCII.ASTERISK);
                    continue;
                }

                if (!(lRange.To is cSequenceSetNumber lTo)) throw new ArgumentException("invalid form 3", nameof(pSequenceSet));

                lTemp = cTools.UIntToBytesReverse(lTo.Number);
                lTemp.Reverse();
                lBytes.AddRange(lTemp);
            }

            Bytes = new cBytes(lBytes);
        }

        public override string ToString()
        {
            if (Secret) return $"{nameof(cTextCommandPart)}()";
            else return $"{nameof(cTextCommandPart)}({Bytes},{Encoded})";
        }
    }
}
