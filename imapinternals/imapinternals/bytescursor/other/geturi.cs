using System;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    public partial class cBytesCursor
    {
        public static readonly cBytes kURISlashSlash = new cBytes("//");

        public bool GetURI(out sURIParts rParts, out string rString, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(GetURI));

            var lBookmark = Position;

            if (!ZProcessURI(out rParts, lContext))
            {
                Position = lBookmark;
                rString = null;
                return false;
            }

            rString = GetFromAsString(lBookmark);
            return true;
        }

        private bool ZProcessURI(out sURIParts rParts, cTrace.cContext pParentContext)
        {
            // rfc 3986, 6874
            //  NOTE: this routine does not return the cursor to its original position if it fails

            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(ZProcessURI));

            rParts = new sURIParts();

            var lStartBookmark = Position;

            if (GetToken(cCharset.Scheme, null, null, out string lScheme) && cCharset.Alpha.Contains(lScheme[0]) && SkipByte(cASCII.COLON))
            {
                lContext.TraceVerbose("absolute: {0}", lScheme);
                rParts.Scheme = lScheme;
            }
            else
            {
                lContext.TraceVerbose("relative");
                Position = lStartBookmark;
                lScheme = null;
            }

            var lPathStartBookmark = Position;

            if (SkipBytes(kURISlashSlash))
            {
                lContext.TraceVerbose("//");

                var lUserInfoEndBookmark = Position;

                if (GetToken(cCharset.UserInfo, cASCII.PERCENT, null, out string lUserInfo) && SkipByte(cASCII.AT))
                {
                    rParts.UserInfo = lUserInfo;
                    lContext.TraceVerbose("userinfo: {0}", lUserInfo);
                    lUserInfoEndBookmark = Position;
                }
                else Position = lUserInfoEndBookmark;

                // note that the rules here for extracting the host name are substantially more liberal than the rfcs (3986, 6874)
                //
                if (SkipByte(cASCII.LBRACKET) && GetToken(cCharset.IPLiteral, cASCII.PERCENT, null, out cByteList lBytes) && SkipByte(cASCII.RBRACKET))
                {
                    rParts.Host = cTools.UTF8BytesToString(lBytes);
                    lContext.TraceVerbose("ipv6 (or greater) host: {0}", rParts.Host);
                }
                else
                {
                    Position = lUserInfoEndBookmark;

                    if (GetToken(cCharset.RegName, cASCII.PERCENT, null, out string lHost))
                    {
                        rParts.Host = lHost;
                        lContext.TraceVerbose("host: {0}", lHost);
                    }
                    else lContext.TraceVerbose("blank host");
                }

                // port
                //  (optional) - the colon is allowed to be there even if the port is not
                //   the syntax allows an unlimited number of digits
                //
                if (SkipByte(cASCII.COLON))
                {
                    if (GetToken(cCharset.Digit, null, null, out string lPort))
                    {
                        rParts.Port = lPort;
                        lContext.TraceVerbose("port: {0}", lPort);
                    }
                    else lContext.TraceVerbose("no port specified (but the ':' was there)");
                }

                if (SkipByte(cASCII.SLASH))
                {
                    rParts.SetHasPathRoot();

                    if (GetToken(cCharset.Path, cASCII.PERCENT, null, out string lPath))
                    {
                        rParts.Path = lPath;
                        lContext.TraceVerbose("path: {0}", lPath);
                    }
                }
            }
            else if (SkipByte(cASCII.SLASH))
            {
                lContext.TraceVerbose("path-absolute");

                rParts.SetHasPathRoot();

                if (GetToken(cCharset.PathSegment, cASCII.PERCENT, null, out cByteList _)) // segment-nz
                {
                    GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    rParts.Path = GetFromAsString(lPathStartBookmark);
                    lContext.TraceVerbose("path: {0}", rParts.Path);
                }
            }
            else if (lScheme == null)
            {
                if (GetToken(cCharset.PathSegmentNoColon, cASCII.PERCENT, null, out cByteList _))
                {
                    lContext.TraceVerbose("path-noscheme");
                    GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    rParts.Path = GetFromAsString(lPathStartBookmark);
                    lContext.TraceVerbose("path: {0}", rParts.Path);
                }
            }
            else
            {
                if (GetToken(cCharset.PathSegment, cASCII.PERCENT, null, out cByteList _))
                {
                    lContext.TraceVerbose("path-rootless");
                    GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    rParts.Path = GetFromAsString(lPathStartBookmark);
                    lContext.TraceVerbose("path: {0}", rParts.Path);
                }
            }

            if (SkipByte(cASCII.QUESTIONMARK))
            {
                if (GetToken(cCharset.AfterPath, cASCII.PERCENT, null, out string lQuery))
                {
                    rParts.Query = lQuery;
                    lContext.TraceVerbose("query: {0}", lQuery);
                }
            }

            if (SkipByte(cASCII.HASH))
            {
                if (GetToken(cCharset.AfterPath, cASCII.PERCENT, null, out string lFragment))
                {
                    rParts.Fragment = lFragment;
                    lContext.TraceVerbose("fragment: {0}", lFragment);
                }
            }

            if (Position == lStartBookmark)
            {
                lContext.TraceVerbose("empty");
                return false;
            }

            // done

            return true;
        }
    }
}
