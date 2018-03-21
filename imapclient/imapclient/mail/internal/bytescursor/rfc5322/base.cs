using System;
using System.Collections.Generic;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        public bool GetRFC5322MsgId(out string rMessageId)
        {
            ;?;

            var lBookmark = Position;

            SkipRFC822CFWS();

            if (!SkipByte(cASCII.LESSTHAN) ||
                !GetRFC5322DotAtomText(out string lIdLeft) ||
                !SkipByte(cASCII.AT) ||
                (!GetRFC5322DotAtomText(out string lIdRight) && !GetRFC5322NoFoldLiteral(out lIdRight)) ||
                !SkipByte(cASCII.GREATERTHAN)
               )
            {
                Position = lBookmark;
                rMessageId = null;
                return false;
            }

            SkipRFC822CFWS();

            rMessageId = lIdLeft + "@" + lIdRight;
            return true;
        }

        public bool GetRFC5322DotAtomText(out string rDotAtomText)
        {
            ;?; // could get the qs form as long as it could be converted

            List<string> lParts = new List<string>();
            string lPart;

            if (!GetRFC822Atom(out lPart)) { rDotAtomText = null; return false; }
            lParts.Add(lPart);

            while (true)
            {
                var lBookmark = Position;
                if (!SkipByte(cASCII.DOT)) break;
                if (!GetRFC822Atom(out lPart)) { Position = lBookmark; break; }
                lParts.Add(lPart);
            }

            rDotAtomText = string.Join(".", lParts);
            return true;
        }

        public bool GetRFC5322QuotedString(out string rString)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            // open quote
            if (!SkipByte(cASCII.DQUOTE)) { rString = null; return false; }

            cByteList lResult = new cByteList();

            while (true)
            {
                ZGetRFC822FWS(lResult);

                if (Position.AtEnd || Position.BytesLine.Literal) { Position = lBookmark; rString = null; return false; }

                byte lByte = Position.BytesLine[Position.Byte];

                if (lByte == cASCII.BACKSL)
                {
                    ZAdvance(ref Position);
                    if (Position.AtEnd || Position.BytesLine.Literal) { Position = lBookmark; rString = null; return false; }
                    lByte = Position.BytesLine[Position.Byte];
                    if (!cCharset.WSPVChar.Contains(lByte)) { Position = lBookmark; rString = null; return false; }
                }
                else if (!cCharset.QText.Contains(lByte)) break;

                lResult.Add(lByte);

                ZAdvance(ref Position);
            }

            // close quote
            if (!SkipByte(cASCII.DQUOTE)) { Position = lBookmark; rString = null; return false; }

            // optional trailing spaces
            SkipRFC822CFWS();

            rString = cTools.UTF8BytesToString(lResult);
            return true;
        }

        public bool GetRFC5322DomainLiteral(out string rDomainLiteral)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            // open bracket
            if (!SkipByte(cASCII.LBRACKET)) { Position = lBookmark; rDomainLiteral = null; return false; }

            // the brackets are part of the result
            cByteList lResult = new cByteList();
            lResult.Add(cASCII.LBRACKET);

            while (true)
            {
                ZGetRFC822FWS(lResult);

                if (Position.AtEnd || Position.BytesLine.Literal) { Position = lBookmark; rDomainLiteral = null; return false; }

                byte lByte = Position.BytesLine[Position.Byte];

                if (lByte == cASCII.BACKSL)
                {
                    ZAdvance(ref Position);
                    if (Position.AtEnd || Position.BytesLine.Literal) { Position = lBookmark; rDomainLiteral = null; return false; }
                    lByte = Position.BytesLine[Position.Byte];
                }

                if (!cCharset.WSPDText.Contains(lByte)) break;

                lResult.Add(lByte);

                ZAdvance(ref Position);
            }

            // close bracket
            if (!SkipByte(cASCII.RBRACKET)) { Position = lBookmark; rDomainLiteral = null; return false; }
            lResult.Add(cASCII.RBRACKET);

            // optional trailing spaces
            SkipRFC822CFWS();

            // done
            rDomainLiteral = cTools.UTF8BytesToString(lResult);
            return true;
        }

        public bool GetRFC5322NoFoldLiteral(out string rNoFoldLiteral)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            if (SkipByte(cASCII.LBRACKET) && GetToken(cCharset.DText, null, null, out string lDText) && SkipByte(cASCII.RBRACKET))
            {
                // optional trailing spaces
                SkipRFC822CFWS();

                rNoFoldLiteral = "[" + lDText + "]";
                return true;
            }

            Position = lBookmark;
            rNoFoldLiteral = null;
            return false;
        }
    }
}