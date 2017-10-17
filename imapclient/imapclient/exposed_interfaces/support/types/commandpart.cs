using System;
using System.Collections.Generic;
using System.IO;

namespace work.bacome.imapclient.support
{
    public abstract class cCommandPart
    {
        public static readonly cCommandPart Space = new cMemoryCommandPart(" ");
        public static readonly cCommandPart Nil = new cMemoryCommandPart("NIL");
        public static readonly cCommandPart NilSecret = new cMemoryCommandPart("NIL", true);
        public static readonly cCommandPart LParen = new cMemoryCommandPart("(");
        public static readonly cCommandPart RParen = new cMemoryCommandPart(")");
        public static readonly cCommandPart RBracket = new cMemoryCommandPart("]");
        public static readonly cCommandPart Dot = new cMemoryCommandPart(".");

        public cCommandPart() { }
    }

    // TODO: probably remove this base class
    public abstract class cStreamCommandPartBase : cCommandPart
    {
        public cStreamCommandPartBase() { }
    }

    public class cStreamCommandPart : cStreamCommandPartBase
    {
        public readonly Stream Stream;
        public readonly int? Length;

        public cStreamCommandPart(Stream pStream)
        {
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead || !pStream.CanSeek) throw new ArgumentOutOfRangeException(nameof(pStream));
            Stream = pStream;
            Length = null;
        }

        public cStreamCommandPart(int pLength, Stream pStream)
        {
            if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
            if (pStream == null) throw new ArgumentNullException(nameof(pStream));
            if (!pStream.CanRead) throw new ArgumentOutOfRangeException(nameof(pStream));
            Stream = pStream;
            Length = pLength;
        }
    }

    public class cFileCommandPart : cStreamCommandPartBase
    {
        public readonly string Path;
        public cFileCommandPart(string pPath) { Path = pPath; }
    }

    public enum eMemoryCommandPartType
    {
        text,
        literal,
        literal8
    }

    public class cMemoryCommandPart : cCommandPart
    {
        public readonly cBytes Bytes;
        public readonly eMemoryCommandPartType Type;
        public readonly bool Secret;
        public readonly bool Encoded;

        public cMemoryCommandPart(IList<byte> pBytes, eMemoryCommandPartType pType = eMemoryCommandPartType.text, bool pSecret = false, bool pEncoded = false)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            Bytes = new cBytes(pBytes);
            Type = pType;
            Secret = pSecret;
            Encoded = pEncoded;
        }

        public cMemoryCommandPart(string pString, bool pSecret = false)
        {
            if (string.IsNullOrEmpty(pString)) throw new ArgumentOutOfRangeException(nameof(pString));

            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
            {
                if (lChar < ' ' || lChar > '~') throw new ArgumentOutOfRangeException(nameof(pString));
                lBytes.Add((byte)lChar);
            }

            Bytes = new cBytes(lBytes);
            Type = eMemoryCommandPartType.text;
            Secret = pSecret;
            Encoded = false;
        }

        public cMemoryCommandPart(uint pNumber)
        {
            var lBytes = cTools.UIntToBytesReverse(pNumber);
            lBytes.Reverse();

            Bytes = new cBytes(lBytes);
            Type = eMemoryCommandPartType.text;
            Secret = false;
            Encoded = false;
        }

        public cMemoryCommandPart(ulong pNumber)
        {
            var lBytes = cTools.ULongToBytesReverse(pNumber);
            lBytes.Reverse();

            Bytes = new cBytes(lBytes);
            Type = eMemoryCommandPartType.text;
            Secret = false;
            Encoded = false;
        }

        public cMemoryCommandPart(cSequenceSet pSequenceSet)
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
            Type = eMemoryCommandPartType.text;
            Secret = false;
            Encoded = false;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMemoryCommandPart));

            if (Secret) lBuilder.Append("secret");
            else
            {
                lBuilder.Append(Bytes);
                lBuilder.Append(Type);
                lBuilder.Append(Encoded);
            }

            return lBuilder.ToString();
        }
    }
}