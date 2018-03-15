using System;
using System.Diagnostics;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cURIParts : IEquatable<cURIParts>
    {
        // rfc 3986, 6874
            
        private static readonly cBytes kSlashSlash = new cBytes("//");

        [Flags]
        private enum fParts
        {
            scheme = 1 << 0,
            userinfo = 1 << 1,
            host = 1 << 2,
            port = 1 << 3,
            pathroot = 1 << 4,
            path = 1 << 5,
            query = 1 << 6,
            fragment = 1 << 7
        }

        private fParts mParts = 0;

        private string _Scheme = null;
        private string _UserInfo = null;
        private string _Host = null;
        private string _DisplayHost = null;
        private string _Port = null;
        private string _Path = null;
        private string _Query = null;
        private string _Fragment = null;

        private cURIParts() { }

        public string Scheme
        {
            get => _Scheme;

            private set
            {
                _Scheme = value;
                mParts |= fParts.scheme;
            }
        }

        public string UserInfo
        {
            get => _UserInfo;

            private set
            {
                _UserInfo = value;
                mParts |= fParts.userinfo;
            }
        }

        public string Host
        {
            get => _Host;

            private set
            {
                _Host = value;
                _DisplayHost = cTools.GetDisplayHost(value);
                mParts |= fParts.host;
            }
        }

        public string DisplayHost => _DisplayHost;

        public string Port
        {
            get => _Port;

            private set
            {
                _Port = value;
                mParts |= fParts.port;
            }
        }

        public string Path
        {
            get => _Path;

            private set
            {
                _Path = value;
                mParts |= fParts.path;
            }
        }

        public string Query
        {
            get => _Query;

            private set
            {
                _Query = value;
                mParts |= fParts.query;
            }
        }

        public string Fragment
        {
            get => _Fragment;

            private set
            {
                _Fragment = value;
                mParts |= fParts.fragment;
            }
        }

        public bool Equals(cURIParts pObject) => this == pObject;

        public override bool Equals(object pObject) => this == pObject as cURIParts;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + mParts.GetHashCode();
                if (_Scheme != null) lHash = lHash * 23 + _Scheme.GetHashCode();
                if (_UserInfo != null) lHash = lHash * 23 + _UserInfo.GetHashCode();
                if (_Host != null) lHash = lHash * 23 + _Host.GetHashCode();
                if (_Port != null) lHash = lHash * 23 + _Port.GetHashCode();
                if (_Path != null) lHash = lHash * 23 + _Path.GetHashCode();
                if (_Query != null) lHash = lHash * 23 + _Query.GetHashCode();
                if (_Fragment != null) lHash = lHash * 23 + _Fragment.GetHashCode();
                return lHash;
            }
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURIParts));

            lBuilder.Append(mParts);
            if (_Scheme != null) lBuilder.Append(nameof(Scheme), _Scheme);
            if (_UserInfo != null) lBuilder.Append(nameof(UserInfo), _UserInfo);
            if (_Host != null) lBuilder.Append(nameof(Host), _Host);
            if (_Port != null) lBuilder.Append(nameof(Port), _Port);
            if (_Path != null) lBuilder.Append(nameof(Path), _Path);
            if (_Query != null) lBuilder.Append(nameof(Query), _Query);
            if (_Fragment != null) lBuilder.Append(nameof(Fragment), _Fragment);

            return lBuilder.ToString();
        }


        public static bool operator ==(cURIParts pA, cURIParts pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            return
                pA.mParts == pB.mParts &&
                pA._Scheme == pB._Scheme &&
                pA._UserInfo == pB._UserInfo &&
                pA._Host == pB._Host &&
                pA._Port == pB._Port &&
                pA._Path == pB._Path &&
                pA._Query == pB._Query &&
                pA._Fragment == pB._Fragment;
        }

        public static bool operator !=(cURIParts pA, cURIParts pB) => !(pA == pB);

        public static bool Process(cBytesCursor pCursor, out cURIParts rParts, cTrace.cContext pParentContext)
        {
            //  NOTE: this routine does not return the cursor to its original position if it fails

            var lContext = pParentContext.NewMethod(nameof(cURIParts), nameof(Process));

            cURIParts lParts = new cURIParts();

            var lStartBookmark = pCursor.Position;

            if (pCursor.GetToken(cCharset.Scheme, null, null, out string lScheme) && cCharset.Alpha.Contains(lScheme[0]) && pCursor.SkipByte(cASCII.COLON))
            {
                lContext.TraceVerbose("absolute: {0}", lScheme);
                lParts.Scheme = lScheme;
            }
            else
            {
                lContext.TraceVerbose("relative");
                pCursor.Position = lStartBookmark;
                lScheme = null;
            }

            var lPathStartBookmark = pCursor.Position;

            if (pCursor.SkipBytes(kSlashSlash))
            {
                lContext.TraceVerbose("//");

                var lUserInfoEndBookmark = pCursor.Position;

                if (pCursor.GetToken(cCharset.UserInfo, cASCII.PERCENT, null, out string lUserInfo) && pCursor.SkipByte(cASCII.AT))
                {
                    lParts.UserInfo = lUserInfo;
                    lContext.TraceVerbose("userinfo: {0}", lUserInfo);
                    lUserInfoEndBookmark = pCursor.Position;
                }
                else pCursor.Position = lUserInfoEndBookmark;

                // note that the rules here for extracting the host name are substantially more liberal than the rfcs (3986, 6874)
                //
                if (pCursor.SkipByte(cASCII.LBRACKET) && pCursor.GetToken(cCharset.IPLiteral, cASCII.PERCENT, null, out cByteList lBytes) && pCursor.SkipByte(cASCII.RBRACKET))
                {
                    lParts.Host = cTools.UTF8BytesToString(lBytes);
                    lContext.TraceVerbose("ipv6 (or greater) host: {0}", lParts.Host);
                }
                else
                {
                    pCursor.Position = lUserInfoEndBookmark;

                    if (pCursor.GetToken(cCharset.RegName, cASCII.PERCENT, null, out string lHost))
                    {
                        lParts.Host = lHost;
                        lContext.TraceVerbose("host: {0}", lHost);
                    }
                    else lContext.TraceVerbose("blank host");
                }

                // port
                //  (optional) - the colon is allowed to be there even if the port is not
                //   the syntax allows an unlimited number of digits
                //
                if (pCursor.SkipByte(cASCII.COLON))
                {
                    if (pCursor.GetToken(cCharset.Digit, null, null, out string lPort))
                    {
                        lParts.Port = lPort;
                        lContext.TraceVerbose("port: {0}", lPort);
                    }
                    else lContext.TraceVerbose("no port specified (but the ':' was there)");
                }

                if (pCursor.SkipByte(cASCII.SLASH))
                {
                    lParts.mParts |= fParts.pathroot;

                    if (pCursor.GetToken(cCharset.Path, cASCII.PERCENT, null, out string lPath))
                    {
                        lParts.Path = lPath;
                        lContext.TraceVerbose("path: {0}", lPath);
                    }
                }
            }
            else if (pCursor.SkipByte(cASCII.SLASH))
            {
                lContext.TraceVerbose("path-absolute");

                lParts.mParts |= fParts.pathroot;

                if (pCursor.GetToken(cCharset.PathSegment, cASCII.PERCENT, null, out cByteList _)) // segment-nz
                {
                    pCursor.GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    lParts.Path = pCursor.GetFromAsString(lPathStartBookmark);
                    lContext.TraceVerbose("path: {0}", lParts.Path);
                }
            }
            else if (lScheme == null)
            {
                if (pCursor.GetToken(cCharset.PathSegmentNoColon, cASCII.PERCENT, null, out cByteList _))
                {
                    lContext.TraceVerbose("path-noscheme");
                    pCursor.GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    lParts.Path = pCursor.GetFromAsString(lPathStartBookmark);
                    lContext.TraceVerbose("path: {0}", lParts.Path);
                }
            }
            else
            {
                if (pCursor.GetToken(cCharset.PathSegment, cASCII.PERCENT, null, out cByteList _))
                {
                    lContext.TraceVerbose("path-rootless");
                    pCursor.GetToken(cCharset.Path, cASCII.PERCENT, null, out cByteList _); // *( "/" segment)
                    lParts.Path = pCursor.GetFromAsString(lPathStartBookmark);
                    lContext.TraceVerbose("path: {0}", lParts.Path);
                }
            }

            if (pCursor.SkipByte(cASCII.QUESTIONMARK))
            {
                if (pCursor.GetToken(cCharset.AfterPath, cASCII.PERCENT, null, out string lQuery))
                {
                    lParts.Query = lQuery;
                    lContext.TraceVerbose("query: {0}", lQuery);
                }
            }

            if (pCursor.SkipByte(cASCII.HASH))
            {
                if (pCursor.GetToken(cCharset.AfterPath, cASCII.PERCENT, null, out string lFragment))
                {
                    lParts.Fragment = lFragment;
                    lContext.TraceVerbose("fragment: {0}", lFragment);
                }
            }

            if (pCursor.Position == lStartBookmark)
            {
                lContext.TraceVerbose("empty");
                rParts = null;
                return false;
            }

            // done

            rParts = lParts;
            return true;
        }

        private bool ZHasParts(fParts pMustHave, fParts pMayHave = 0)
        {
            if ((mParts & pMustHave) != pMustHave) return false;
            if ((mParts & ~(pMustHave | pMayHave)) != 0) return false;
            return true;
        }

        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cURIParts), nameof(_Tests));

            cBytesCursor lCursor;
            cURIParts lParts;
            string lString;
            cURL lURL;
            cURI lURI;



            // 5

            if (!LTryParse("HTTP://user;AUTH=GSSAPI@SERVER2/", out lParts)) throw new cTestsException("should have succeeded 5", lContext);
            if (lParts.Scheme != "HTTP" || lParts.UserInfo != "user;AUTH=GSSAPI" || lParts.Host != "SERVER2" || !lParts.ZHasParts(fParts.scheme | fParts.userinfo | fParts.host | fParts.pathroot))
                throw new cTestsException("unexpected properties in test 5");

            // 8
            //  € to type it hold alt and type 0128
            // 

            if (!LTryParse("IMAP://fr%E2%82%aCd@fred.com:123456789123456", out lParts)) throw new cTestsException("should have succeeded 8", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.userinfo | fParts.host | fParts.port) || lParts.UserInfo != "fr€d" || lParts.Port != "123456789123456") throw new cTestsException("unexpected state 8", lContext);





            // 9

            lCursor = new cBytesCursor("IMAP://user@[]:111");

            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("should have succeeded 9", lContext);
            if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response 9", lContext);
            if (cURL.TryParse(lString, out lURL)) throw new cTestsException("should have failed 9", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.userinfo) || lParts.Scheme != "IMAP" || lParts.UserInfo != "user" || lCursor.GetRestAsString() != "[]:111") throw new cTestsException("unexpected state 9", lContext);

            // 10

            lCursor = new cBytesCursor("IMAP://user@[1.2.3.4");

            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("should have succeeded 10", lContext);
            if (lCursor.Position.AtEnd) throw new cTestsException("should not have read entire response 10", lContext);
            if (cURL.TryParse(lString, out lURL)) throw new cTestsException("should have failed 10", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.userinfo) || lParts.Scheme != "IMAP" || lParts.UserInfo != "user" || lCursor.GetRestAsString() != "[1.2.3.4") throw new cTestsException("unexpected state 9", lContext);

            // 12

            lCursor = new cBytesCursor("IMAP:///still here");

            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("should have succeeded 12", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.pathroot | fParts.path) || lParts.Scheme != "IMAP" || lParts.Path != "still" || lCursor.GetRestAsString() != " here") throw new cTestsException("unexpected properties 12");
            if (cURL.TryParse(lString, out lURL)) throw new cTestsException("should have failed 12", lContext);

            // 13

            lCursor = new cBytesCursor("IMAP://:7still here");
            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("should have succeeded 13", lContext);
            if (lCursor.GetRestAsString() != "still here") throw new cTestsException("should be some left 13", lContext);

            if (lParts.Port != "7") throw new cTestsException("unexpected properties in test 13");





            // 14
            lCursor = new cBytesCursor("IMAP://user;AUTH=*@SERVER2/REMOTE IMAP://user;AUTH=*@SERVER3/REMOTE]");
            if (!lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("2193.3.1");
            lURL = new cURL(lString);
            if (!lURL.IsMailboxReferral) throw new cTestsException("2193.3.2");
            if (!lCursor.SkipByte(cASCII.SPACE) || !lCursor.GetURI(out lParts, out lString, lContext)) throw new cTestsException("2193.3.3");
            lURI = new cURI(lString);
            if (!lURI.IsMailboxReferral) throw new cTestsException("2193.3.4");
            if (lCursor.Position.AtEnd) throw new cTestsException("2193.3.5");
            if (lCursor.GetRestAsString() != "]") throw new cTestsException("2193.3.6");
            if (lURI.MustUseAnonymous || lURI.UserId != "user" || lURI.MechanismName != null || lURI.Host != "SERVER3" || lURI.MailboxPath != "REMOTE") throw new cTestsException("2193.3.7");


            // 15

            if (!LTryParse("http://www.ics.uci.edu/pub/ietf/uri/#Related", out lParts)) throw new cTestsException("URI.15");

            if (!lParts.ZHasParts(fParts.scheme | fParts.host | fParts.pathroot | fParts.path | fParts.fragment)) throw new cTestsException("URI.15.1");
            if (lParts.Scheme != "http" || lParts.Host != "www.ics.uci.edu" || lParts.Path != "pub/ietf/uri/" || lParts.Fragment != "Related") throw new cTestsException("URI.15.2");

            // 16

            if (!LTryParse("http://www.ics.uci.edu/pub/ietf/uri/historical.html#WARNING", out lParts)) throw new cTestsException("URI.16");
            if (!lParts.ZHasParts(fParts.scheme | fParts.host | fParts.pathroot | fParts.path | fParts.fragment)) throw new cTestsException("URI.16.1");
            if (lParts.Scheme != "http" || lParts.Host != "www.ics.uci.edu" || lParts.Path != "pub/ietf/uri/historical.html" || lParts.Fragment != "WARNING") throw new cTestsException("URI.16.2");


            // 17 - IDN
            if (!LTryParse("IMAP://fr%E2%82%aCd@xn--frd-l50a.com:123456789123456", out lParts)) throw new cTestsException("should have succeeded 17", lContext);
            if (!lParts.ZHasParts(fParts.scheme | fParts.userinfo | fParts.host | fParts.port) || lParts.UserInfo != "fr€d" || lParts.Host != "xn--frd-l50a.com" || lParts.DisplayHost != "fr€d.com" || lParts.Port != "123456789123456") throw new cTestsException("unexpected state 8", lContext);


            // relative URIs
            //  TODO

            // edge cases
            //  TODO



            bool LTryParse(string pURL, out cURIParts rParts)
            {
                var lxCursor = new cBytesCursor(pURL);
                if (!Process(lxCursor, out rParts, pParentContext)) return false;
                if (!lxCursor.Position.AtEnd) return false;
                return true;
            }
        }
    }
}