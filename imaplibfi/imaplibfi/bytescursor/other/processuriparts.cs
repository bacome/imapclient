using System;
using work.bacome.imapsupport;

namespace work.bacome.imapinternals
{
    public partial class cBytesCursor
    {
        public static readonly cBytes kSlashSlash = new cBytes("//");

        public bool ProcessURIParts(out cURIParts rParts)
        {
            // rfc 3986, 6874
            //  NOTE: this routine does not return the cursor to its original position if it fails

            rParts = new cURIParts();

            var lStartBookmark = Position;

            if (GetToken(cCharset.Scheme, null, null, out string lScheme) && cCharset.Alpha.Contains(lScheme[0]) && SkipByte(cASCII.COLON)) rParts.Scheme = lScheme;
            else
            {
                Position = lStartBookmark;
                lScheme = null;
            }

            var lPathStartBookmark = Position;

            if (SkipBytes(kSlashSlash))
            {
                var lUserInfoEndBookmark = Position;

                if (GetToken(cCharset.UserInfo, cASCII.PERCENT, null, out string lUserInfo) && SkipByte(cASCII.AT))
                {
                    rParts.UserInfo = lUserInfo;
                    lUserInfoEndBookmark = Position;
                }
                else Position = lUserInfoEndBookmark;

                // note that the rules here for extracting the host name are substantially more liberal than the rfcs (3986, 6874)
                //
                if (SkipByte(cASCII.LBRACKET) && GetToken(cCharset.IPLiteral, cASCII.PERCENT, null, out cByteList lBytes) && SkipByte(cASCII.RBRACKET)) rParts.Host = "[" + cTools.UTF8BytesToString(lBytes) + "]";
                else
                {
                    Position = lUserInfoEndBookmark;
                    if (GetToken(cCharset.RegName, cASCII.PERCENT, null, out string lHost)) rParts.Host = lHost;
                }

                // port
                //  (optional) - the colon is allowed to be there even if the port is not
                //   the syntax allows an unlimited number of digits
                //
                if (SkipByte(cASCII.COLON))
                {
                    if (GetToken(cCharset.Digit, null, null, out string lPort)) rParts.Port = lPort;
                }

                if (SkipByte(cASCII.SLASH))
                {
                    rParts.SetHasPathRoot();
                    if (GetToken(cCharset.Path, cASCII.PERCENT, null, out string lPath)) rParts.Path = lPath;
                }
            }
            else if (SkipByte(cASCII.SLASH))
            {
                rParts.SetHasPathRoot();

                if (GetToken(cCharset.PathSegment, cASCII.PERCENT, null, out cByteList _)) // segment-nz
                {
                    GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    rParts.Path = GetFromAsString(lPathStartBookmark);
                }
            }
            else if (lScheme == null)
            {
                if (GetToken(cCharset.PathSegmentNoColon, cASCII.PERCENT, null, out cByteList _))
                {
                    GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    rParts.Path = GetFromAsString(lPathStartBookmark);
                }
            }
            else
            {
                if (GetToken(cCharset.PathSegment, cASCII.PERCENT, null, out cByteList _))
                {
                    GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    rParts.Path = GetFromAsString(lPathStartBookmark);
                }
            }

            if (SkipByte(cASCII.QUESTIONMARK))
            {
                if (GetToken(cCharset.AfterPath, cASCII.PERCENT, null, out string lQuery)) rParts.Query = lQuery;
            }

            if (SkipByte(cASCII.HASH))
            {
                if (GetToken(cCharset.AfterPath, cASCII.PERCENT, null, out string lFragment)) rParts.Fragment = lFragment;
            }

            if (Position == lStartBookmark) return false;

            // done

            return true;
        }
    }
}
