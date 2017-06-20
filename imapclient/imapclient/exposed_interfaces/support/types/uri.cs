using System;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient.support
{
    public class cURI
    {
        // rfc 3986, 6874

        public readonly string OriginalString;
        private readonly cURIParts mParts;
        private readonly cURLParts mURLParts;

        public cURI(string pURI)
        {
            if (string.IsNullOrEmpty(pURI)) throw new ArgumentOutOfRangeException(nameof(pURI));
            if (!cBytesCursor.TryConstruct(pURI, out var lCursor)) throw new ArgumentOutOfRangeException(nameof(pURI));
            if (!cURIParts.Process(lCursor, out mParts, cTrace.cContext.Null) || !lCursor.Position.AtEnd) throw new ArgumentOutOfRangeException(nameof(pURI));

            OriginalString = pURI;
            mURLParts = ZGetURLParts(pURI);
        }

        private cURI(string pURI, cURIParts pParts)
        {
            OriginalString = pURI;
            mParts = pParts;
            mURLParts = ZGetURLParts(pURI);
        }

        private cURLParts ZGetURLParts(string pURL)
        {
            if (!cBytesCursor.TryConstruct(pURL, out var lCursor)) return null;
            if (!cURLParts.Process(lCursor, out var lParts, cTrace.cContext.Null) || !lCursor.Position.AtEnd) return null;
            return lParts;
        }

        public string Scheme => mParts.Scheme;
        public string UserInfo => mParts.UserInfo;
        public string UserId => mURLParts?.UserId;
        public string MechanismName => mURLParts?.MechanismName;
        public string Host => mParts.Host;
        public string PortString => mParts.Port;
        public int? Port => mURLParts?.Port;
        public string Path => mParts.Path;
        public string MailboxName => mURLParts?.MailboxName;
        public uint? UIDValidity => mURLParts?.UIDValidity;
        public uint? UID => mURLParts?.UID;
        public string Section => mURLParts?.Section;
        public uint? PartialOffset => mURLParts?.PartialOffset;
        public uint? PartialLength => mURLParts?.PartialLength;
        public DateTime? Expire => mURLParts?.Expire;
        public string Application => mURLParts?.Application;
        public string AccessUserId => mURLParts?.AccessUserId;
        public string TokenMechanism => mURLParts?.TokenMechanism;
        public string Token => mURLParts?.Token;
        public string Query => mParts.Query;
        public string Search => mURLParts?.Search;
        public string Fragment => mParts.Fragment;

        public bool MustUseAnonymous => mURLParts != null && mURLParts.MustUseAnonymous;
        public bool IsHomeServerReferral => mURLParts != null && mURLParts.IsHomeServerReferral;
        public bool IsMailboxReferral => mURLParts != null && mURLParts.IsMailboxReferral;
        public bool IsMailboxSearch => mURLParts != null && mURLParts.IsMailboxSearch;
        public bool IsMessageReference => mURLParts != null && mURLParts.IsMessageReference;
        public bool IsPartial => mURLParts != null && mURLParts.IsPartial;
        public bool IsAuthorisable => mURLParts != null && mURLParts.IsAuthorisable;
        public bool IsAuthorised => mURLParts != null && mURLParts.IsAuthorised;

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURI));
            lBuilder.Append(mParts);
            if (mURLParts != null) lBuilder.Append(mURLParts);
            return lBuilder.ToString();
        }

        public static bool TryParse(string pURI, out cURI rURI)
        {
            if (string.IsNullOrWhiteSpace(pURI)) { rURI = null; return false; }

            if (!cBytesCursor.TryConstruct(pURI, out var lCursor)) { rURI = null; return false; }
            if (!cURIParts.Process(lCursor, out var lParts, cTrace.cContext.Null) || !lCursor.Position.AtEnd) { rURI = null; return false; };

            rURI = new cURI(pURI, lParts);
            return true;
        }

        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cURI), nameof(_Tests));

            cURI lURI;


            // 1

            if (!TryParse("imap://fred.com", out lURI)) throw new cTestsException("should have succeeded 1", lContext);

            if (!lURI.MustUseAnonymous || lURI.UserId != null || lURI.MechanismName != null || lURI.Host != "fred.com" || lURI.Port != 143)
                throw new cTestsException("unexpected properties in test 1");

            // 2

            if (!TryParse("IMAP://user;AUTH=*@SERVER2/", out lURI)) throw new cTestsException("should have succeeded 2", lContext);

            if (lURI.MustUseAnonymous || lURI.UserId != "user" || lURI.MechanismName != null || lURI.Host != "SERVER2" || lURI.Port != 143)
                throw new cTestsException("unexpected properties in test 2");

            // 3

            if (!TryParse("IMAP://MIKE@SERVER2/", out lURI)) throw new cTestsException("should have succeeded 3", lContext);

            if (lURI.MustUseAnonymous || lURI.UserId != "MIKE" || lURI.MechanismName != null || lURI.Host != "SERVER2" || lURI.Port != 143)
                throw new cTestsException("unexpected properties in test 3");

            // 4

            if (!TryParse("IMAP://user;AUTH=GSSAPI@SERVER2/", out lURI)) throw new cTestsException("should have succeeded 4", lContext);

            if (lURI.MustUseAnonymous || lURI.UserId != "user" || !lURI.MechanismName.Equals("gssapi", StringComparison.OrdinalIgnoreCase) || lURI.Host != "SERVER2" || lURI.Port != 143)
                throw new cTestsException("unexpected properties in test 4");

            // 5

            if (!TryParse("HTTP://user;AUTH=GSSAPI@SERVER2/", out lURI)) throw new cTestsException("should have succeeded 5", lContext);
            if (lURI.mURLParts != null) throw new cTestsException("should have failed 5", lContext);
            if (lURI.Scheme != "HTTP" || lURI.UserInfo != "user;AUTH=GSSAPI" || lURI.Host != "SERVER2") throw new cTestsException("unexpected properties in test 5");

            // 6

            if (!TryParse("IMAP://user@[th.is:is::a:funny:one]:993", out lURI)) throw new cTestsException("should have succeeded 6", lContext);

            if (lURI.MustUseAnonymous || lURI.UserId != "user" || lURI.MechanismName != null || lURI.Host != "th.is:is::a:funny:one" || lURI.Port != 993)
                throw new cTestsException("unexpected properties in test 6");

            // 7

            if (!TryParse("IMAP://user@[th.is:is::a:funny:one]:", out lURI)) throw new cTestsException("should have succeeded 7", lContext);

            if (lURI.MustUseAnonymous || lURI.UserId != "user" || lURI.MechanismName != null || lURI.Host != "th.is:is::a:funny:one" || lURI.Port != 143)
                throw new cTestsException("unexpected properties in test 7");


            // 11

            if (!TryParse("IMAP:///", out lURI)) throw new cTestsException("should have succeeded 11", lContext);
            if (lURI.mURLParts != null) throw new cTestsException("should have failed 11", lContext);


            // 15

            if (!TryParse("http://www.ics.uci.edu/pub/ietf/uri/#Related", out lURI)) throw new cTestsException("URI.15");
            if (lURI.mURLParts != null || lURI.IsMailboxReferral) throw new cTestsException("URI.15.1");
            if (lURI.Scheme != "http" || lURI.Host != "www.ics.uci.edu" || lURI.Path != "pub/ietf/uri/" || lURI.Fragment != "Related") throw new cTestsException("URI.15.2");

            // 16

            if (!TryParse("http://www.ics.uci.edu/pub/ietf/uri/historical.html#WARNING", out lURI)) throw new cTestsException("URI.16");
            if (lURI.mURLParts != null || lURI.IsMailboxReferral) throw new cTestsException("URI.16.1");
            if (lURI.Scheme != "http" || lURI.Host != "www.ics.uci.edu" || lURI.Path != "pub/ietf/uri/historical.html" || lURI.Fragment != "WARNING") throw new cTestsException("URI.16.2");

            // 17

            if (!TryParse("IMAP://;AUTH=*@SERVER2/", out lURI)) throw new cTestsException("should have succeeded 17", lContext);

            if (lURI.MustUseAnonymous || lURI.UserId != null || lURI.MechanismName != null || lURI.Host != "SERVER2" || lURI.Port != 143)
                throw new cTestsException("unexpected properties in test 17");


            // relative URIs
            //  TODO

            // edge cases
            //  TODO
        }
    }
}
