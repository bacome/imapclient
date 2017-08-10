using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public enum eCommandPartType
    {
        text,
        literal,
        literal8
    }

    public class cCommandPart
    {
        public static readonly cCommandPart Space = new cCommandPart(" ");
        public static readonly cCommandPart Nil = new cCommandPart("NIL");
        public static readonly cCommandPart NilSecret = new cCommandPart("NIL", true);
        public static readonly cCommandPart LParen = new cCommandPart("(");
        public static readonly cCommandPart RParen = new cCommandPart(")");
        public static readonly cCommandPart RBracket = new cCommandPart("]");
        public static readonly cCommandPart Dot = new cCommandPart(".");

        public readonly cBytes Bytes;
        public readonly eCommandPartType Type;
        public readonly bool Secret;
        public readonly bool Encoded;
        public readonly cBytes LiteralLengthBytes;

        public cCommandPart(IList<byte> pBytes, eCommandPartType pType = eCommandPartType.text, bool pSecret = false, bool pEncoded = false)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
            Bytes = new cBytes(pBytes);
            Type = pType;
            Secret = pSecret;
            Encoded = pEncoded;

            if (pType == eCommandPartType.text) LiteralLengthBytes = null;
            else
            {
                cByteList lBytes = cTools.IntToBytesReverse(pBytes.Count);
                lBytes.Reverse();
                LiteralLengthBytes = new cBytes(lBytes);
            }
        }

        public cCommandPart(string pString, bool pSecret = false)
        {
            if (string.IsNullOrEmpty(pString)) throw new ArgumentOutOfRangeException(nameof(pString));

            var lBytes = new cByteList(pString.Length);

            foreach (char lChar in pString)
            {
                if (lChar < ' ' || lChar > '~') throw new ArgumentOutOfRangeException(nameof(pString));
                lBytes.Add((byte)lChar);
            }

            Bytes = new cBytes(lBytes);
            Type = eCommandPartType.text;
            Secret = pSecret;
            Encoded = false;
            LiteralLengthBytes = null;
        }

        public cCommandPart(uint pNumber)
        {
            var lBytes = cTools.UIntToBytesReverse(pNumber);
            lBytes.Reverse();

            Bytes = new cBytes(lBytes);
            Type = eCommandPartType.text;
            Secret = false;
            Encoded = false;
            LiteralLengthBytes = null;
        }

        public cCommandPart(cSequenceSet pSequenceSet)
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
            Type = eCommandPartType.text;
            Secret = false;
            Encoded = false;
            LiteralLengthBytes = null;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCommandPart));

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