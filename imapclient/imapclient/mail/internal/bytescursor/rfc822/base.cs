using System;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        public static readonly cBytes CRLF = new cBytes("\r\n");
        private static readonly cBytes kCRLFTAB = new cBytes("\r\n\t");
        private static readonly cBytes kCRLFSPACE = new cBytes("\r\n ");

        public bool SkipRFC822WSP()
        {
            bool lResult = false;

            while (true)
            {
                if (Position.AtEnd || Position.BytesLine.Literal) return lResult;
                byte lByte = Position.BytesLine[Position.Byte];
                if (lByte != cASCII.SPACE && lByte != cASCII.TAB) return lResult;
                lResult = true;
                ZAdvance(ref Position);
            }
        }

        public bool SkipRFC822FWS()
        {
            bool lResult = SkipRFC822WSP();

            while (true)
            {
                var lBookmark = Position;

                if (!SkipBytes(CRLF)) return lResult;

                if (!SkipRFC822WSP())
                {
                    Position = lBookmark;
                    return lResult;
                }

                lResult = true;
            }
        }

        public bool SkipRFC822CFWS()
        {
            bool lResult = false;

            while (true)
            {
                if (SkipRFC822FWS()) lResult = true;
                else if (ZSkipRFC822Comment()) lResult = true;
                else return lResult;
            }
        }

        private bool ZSkipRFC822Comment()
        {
            var lBookmark = Position;

            if (!SkipByte(cASCII.LPAREN)) return false;

            while (true)
            {
                SkipRFC822FWS();
                if (ZSkipRFC822CommentContent()) continue;
                if (ZSkipRFC822Comment()) continue;
                break;
            }

            if (SkipByte(cASCII.RPAREN)) return true;

            Position = lBookmark;
            return false;
        }

        private bool ZSkipRFC822CommentContent()
        {
            bool lResult = false;

            while (true)
            {
                if (Position.AtEnd || Position.BytesLine.Literal) return lResult;

                byte lByte = Position.BytesLine[Position.Byte];

                if (lByte == cASCII.BACKSL)
                {
                    var lBookmark = Position;

                    ZAdvance(ref Position);

                    if (Position.AtEnd || Position.BytesLine.Literal)
                    {
                        Position = lBookmark;
                        return lResult;
                    }

                    lByte = Position.BytesLine[Position.Byte];
                }
                else if (!cCharset.ObsCText.Contains(lByte)) return lResult;

                lResult = true;
                ZAdvance(ref Position);
            }
        }

        public bool GetRFC822FieldName(out string rFieldName) => GetToken(cCharset.FText, null, null, out rFieldName);

        public bool GetRFC822FieldValue(out cByteList rFieldValue)
        {
            var lBookmark = Position;

            var lByteList = new cByteList();

            while (true)
            {
                if (Position.AtEnd || Position.BytesLine.Literal)
                {
                    Position = lBookmark;
                    rFieldValue = null;
                    return false;
                }

                if (SkipBytes(kCRLFTAB)) lByteList.Add(cASCII.TAB);
                else if (SkipBytes(kCRLFSPACE)) lByteList.Add(cASCII.SPACE);
                else if (SkipBytes(CRLF))
                {
                    rFieldValue = lByteList;
                    return true;
                }
                else
                {
                    lByteList.Add(Position.BytesLine[Position.Byte]);
                    ZAdvance(ref Position);
                }
            }
        }

        public bool GetRFC822Atom(out string rAtom)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            if (!GetToken(cCharset.AText, null, null, out rAtom)) { Position = lBookmark; return false; }

            // optional trailing spaces
            SkipRFC822CFWS();

            return true;
        }

        public bool GetRFC822DAtom(out string rAtom)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            if (!GetToken(cCharset.DotAText, null, null, out rAtom)) { Position = lBookmark; return false; }

            // optional trailing spaces
            SkipRFC822CFWS();

            return true;
        }

        public bool GetRFC822QuotedString(out string rString)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            // open quote
            if (!SkipByte(cASCII.DQUOTE)) { Position = lBookmark; rString = null; return false; }

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
                }
                else if (!cCharset.ObsQText.Contains(lByte)) break;

                lResult.Add(lByte);

                ZAdvance(ref Position);
            }

            // close quote
            if (!SkipByte(cASCII.DQUOTE)) { Position = lBookmark; rString = null; return false; }

            // optional trailing spaces
            SkipRFC822CFWS();

            // done
            rString = cTools.UTF8BytesToString(lResult);
            return true;
        }

        private bool ZGetRFC822WSP(cByteList pBytes)
        {
            bool lResult = false;

            while (true)
            {
                if (Position.AtEnd || Position.BytesLine.Literal) return lResult;
                byte lByte = Position.BytesLine[Position.Byte];
                if (lByte != cASCII.SPACE && lByte != cASCII.TAB) return lResult;
                lResult = true;
                pBytes.Add(lByte);
                ZAdvance(ref Position);
            }
        }

        public bool ZGetRFC822FWS(cByteList pBytes)
        {
            bool lResult = ZGetRFC822WSP(pBytes);

            while (true)
            {
                var lBookmark = Position;

                if (!SkipBytes(CRLF)) return lResult;

                if (!ZGetRFC822WSP(pBytes))
                {
                    Position = lBookmark;
                    return lResult;
                }

                lResult = true;
            }
        }

        public bool GetRFC822DomainLiteral(out string rDomainLiteral)
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
                else if (!cCharset.ObsDText.Contains(lByte)) break;

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
    }
}