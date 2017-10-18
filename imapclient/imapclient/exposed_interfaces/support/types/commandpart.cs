using System;
using System.Collections.Generic;
using System.IO;

namespace work.bacome.imapclient.support
{
    public abstract class cCommandPart
    {
        public static readonly cCommandPart Space = new cTextCommandPart(" ");
        public static readonly cCommandPart Nil = new cTextCommandPart("NIL");
        public static readonly cCommandPart NilSecret = new cTextCommandPart("NIL", true);
        public static readonly cCommandPart LParen = new cTextCommandPart("(");
        public static readonly cCommandPart RParen = new cTextCommandPart(")");
        public static readonly cCommandPart RBracket = new cTextCommandPart("]");
        public static readonly cCommandPart Dot = new cTextCommandPart(".");

        public readonly bool Secret;
        public readonly bool Encoded;

        public cCommandPart(bool pSecret, bool pEncoded)
        {
            Secret = pSecret;
            Encoded = pEncoded;
        }
    }

    public abstract class cLiteralCommandPartBase : cCommandPart
    {
        public readonly bool Binary;

        public cLiteralCommandPartBase(bool pSecret, bool pEncoded, bool pBinary) : base(pSecret, pEncoded)
        {
            Binary = pBinary;
        }

        public abstract int Length { get; }
    }

    public class cStreamCommandPart : cLiteralCommandPartBase
    {
        private readonly int mLength;
        public readonly Stream Stream;

        public cStreamCommandPart(bool pBinary, int pLength, Stream pStream) : base(false, false, pBinary)
        {
            if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            mLength = pLength;
            Stream = pStream;
        }

        public override int Length => mLength;

        public override string ToString()
        {
            if (Secret) return $"{nameof(cStreamCommandPart)}()";
            else return $"{nameof(cStreamCommandPart)}({Encoded},{Binary},{Length})";
        }
    }

    public class cLiteralCommandPart : cLiteralCommandPartBase
    {
        public readonly cBytes Bytes;

        public cLiteralCommandPart(bool pSecret, bool pEncoded, bool pBinary, IList<byte> pBytes) : base(pSecret, pEncoded, pBinary)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            Bytes = new cBytes(pBytes);
        }

        public override int Length => Bytes.Count;

        public override string ToString()
        {
            if (Secret) return $"{nameof(cLiteralCommandPart)}()";
            else return $"{nameof(cLiteralCommandPart)}({Encoded},{Binary},{Bytes})";
        }
    }

    public class cTextCommandPart : cCommandPart
    {
        public readonly cBytes Bytes;

        public cTextCommandPart(bool pSecret, bool pEncoded, IList<byte> pBytes) : base(pSecret, pEncoded)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            Bytes = new cBytes(pBytes);
        }

        public cTextCommandPart(string pString) : base(false, false)
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

        public cTextCommandPart(string pString) : base(false, false)
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
            else return $"{nameof(cTextCommandPart)}({Encoded},{Bytes})";
        }
    }
}
