using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        private static readonly cBytes kCRLF = new cBytes("\r\n");

        public int SkipRFC822WSP()
        {
            int lResult = 0;

            while (true)
            {
                if (Position.AtEnd || Position.BytesLine.Literal) return lResult;
                byte lByte = Position.BytesLine[Position.Byte];
                if (lByte != cASCII.SPACE && lByte != cASCII.TAB) return lResult;
                lResult++;
                ZAdvance(ref Position);
            }
        }

        public int SkipRFC822FWS()
        {
            int lResult = SkipRFC822WSP();

            while (true)
            {
                var lBookmark = Position;

                if (!SkipBytes(kCRLF)) return lResult;

                int lSpaces = SkipRFC822WSP();

                if (lSpaces == 0)
                {
                    Position = lBookmark;
                    return lResult;
                }

                lResult += lSpaces;
            }
        }

        public bool SkipRFC822CFWS()
        {
            bool lResult = false;

            while (true)
            {
                if (SkipRFC822FWS() != 0) lResult = true;
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
                else if (!cCharset.CText.Contains(lByte)) return lResult;

                lResult = true;
                ZAdvance(ref Position);
            }
        }

        public bool GetRFC822HeaderName(out string rName)
        {

        }

        public bool GetRFC822HeaderValue(out IList<byte> rValue)
        {

        }

        public bool GetRFC822Atom(out string rString)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            if (!GetToken(cCharset.AText, null, null, out rString)) { Position = lBookmark; return false; }

            // optional trailing spaces
            SkipRFC822CFWS();

            return true;
        }

        public bool GetRFC822DotAtom(out string rString)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            cByteList lResult = new cByteList();
            cByteList lSegment;

            if (!GetToken(cCharset.AText, null, null, out lSegment)) { Position = lBookmark; rString = null; return false; }

            lResult.AddRange(lSegment);

            while (true)
            {
                lBookmark = Position;
                if (!SkipByte(cASCII.DOT)) break;
                if (!GetToken(cCharset.AText, null, null, out lSegment)) { Position = lBookmark; break; }
                lResult.AddRange(lSegment);
            }

            // optional trailing spaces
            SkipRFC822CFWS();

            // done
            rString = cTools.UTF8BytesToString(lResult);
            return true;
        }

        public bool GetRFC822QuotedString(out string rString)
        {
            var lBookmark = Position;

            // optional leading spaces
            SkipRFC822CFWS();

            // open quote
            if (!SkipByte(cASCII.DQUOTE)) { rString = null; return false; }

            ;?;


            // tfws






            // optional trailing spaces
            SkipRFC822CFWS();


            ;?;
        }




        /*
        private bool ZGet822Atom(out IList<byte> rBytes)
        {
            // [CFWS] 1*atext [CFWS]
        }


        private bool ZGet822QuotedString(out IList<byte> rBytes)
        {
            // [CFWS] DQUOTE *([FWS] qcontent) [FWS] DQUOTE [CFWS]

            //  note that the FWS has to be extracted sans the CRLF

            // contains qtext (1-8,11,12,14-31,33,35-91,93-126,127) or quoted-pair
            //  = not null, not tab, not lf, not cr, not space, not ", not \

            // quoted-pair = \ anything
            //  note comments in rfc822 about quoted crlf tho - 
        }


        private bool ZGet822Word(out IList<byte> rBytes)
        {
            // atom / quoted-string

        }

        private bool ZGet822Phrase(out IList<byte> rBytes)
        {
            // word *(word / "." / CFWS)
            List<byte> lResult = new List<byte>();

            // note that wsp between words is considered to be ONE wsp
        }

        private bool ZGet822MsgId(out IList<byte> rBytes)
        {
            // [CFWS] "<" id-left "@" id-right ">" [CFWS]
        } */
    }
}