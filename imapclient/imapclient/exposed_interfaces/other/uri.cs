using System;
using System.Diagnostics;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a parsed URI.
    /// </summary>
    /// <remarks>
    /// See RFC 3986 and RFC 6874.
    /// </remarks>
    public class cURI
    {
        // rfc 3986, 6874

        /**<summary>The string that was parsed to initialise this instance.</summary>*/
        public readonly string OriginalString;
        private readonly cURIParts mParts;
        private readonly cURLParts mURLParts;

        /// <summary>
        /// Initialises a new instance from the specified string. Will throw if the string cannot be parsed.
        /// </summary>
        /// <param name="pURI"></param>
        public cURI(string pURI)
        {
            if (string.IsNullOrEmpty(pURI)) throw new ArgumentOutOfRangeException(nameof(pURI));
            var lCursor = new cBytesCursor(pURI);
            if (!cURIParts.Process(lCursor, out mParts, cTrace.cContext.None) || !lCursor.Position.AtEnd) throw new ArgumentOutOfRangeException(nameof(pURI));

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
            var lCursor = new cBytesCursor(pURL);
            if (!cURLParts.Process(lCursor, out var lParts, cTrace.cContext.None) || !lCursor.Position.AtEnd) return null;
            return lParts;
        }

        /**<summary>Gets the 'scheme' part of the URI. May be <see langword="null"/>.</summary>*/
        public string Scheme => mParts.Scheme;
        /**<summary>Gets the decoded 'userinfo' part of the URI. May be <see langword="null"/>.</summary>*/
        public string UserInfo => mParts.UserInfo;
        /**<summary>Gets the <see cref="cURL.UserId"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string UserId => mURLParts?.UserId;
        /**<summary>Gets the <see cref="cURL.MechanismName"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string MechanismName => mURLParts?.MechanismName;
        /**<summary>Gets the 'host' part of the URI. May be <see langword="null"/>.</summary>*/
        public string Host => mParts.Host;
        /**<summary>Gets the 'port' part of the URI. May be <see langword="null"/>.</summary>*/
        public string PortString => mParts.Port;
        /**<summary>Gets the <see cref="cURL.Port"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public int? Port => mURLParts?.Port;
        /**<summary>Gets the decoded 'path' part of the URI. May be <see langword="null"/>.</summary>*/
        public string Path => mParts.Path;
        /**<summary>Gets the <see cref="cURL.MailboxPath"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string MailboxPath => mURLParts?.MailboxPath;
        /**<summary>Gets the <see cref="cURL.UIDValidity"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public uint? UIDValidity => mURLParts?.UIDValidity;
        /**<summary>Gets the <see cref="cURL.UID"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public uint? UID => mURLParts?.UID;
        /**<summary>Gets the <see cref="cURL.Section"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string Section => mURLParts?.Section;
        /**<summary>Gets the <see cref="cURL.PartialOffset"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public uint? PartialOffset => mURLParts?.PartialOffset;
        /**<summary>Gets the <see cref="cURL.PartialLength"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public uint? PartialLength => mURLParts?.PartialLength;
        /**<summary>Gets the <see cref="cURL.Expire"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public DateTime? Expire => mURLParts?.Expire;
        /**<summary>Gets the <see cref="cURL.Application"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string Application => mURLParts?.Application;
        /**<summary>Gets the <see cref="cURL.AccessUserId"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string AccessUserId => mURLParts?.AccessUserId;
        /**<summary>Gets the <see cref="cURL.TokenMechanism"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string TokenMechanism => mURLParts?.TokenMechanism;
        /**<summary>Gets the <see cref="cURL.Token"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string Token => mURLParts?.Token;
        /**<summary>Gets the decoded 'query' part of the URI. May be <see langword="null"/>.</summary>*/
        public string Query => mParts.Query;
        /**<summary>Gets the <see cref="cURL.Search"/> if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string Search => mURLParts?.Search;
        /**<summary>Gets the decoded 'fragment' part of the URI. May be <see langword="null"/>.</summary>*/
        public string Fragment => mParts.Fragment;

        /**<summary>Indicates whether the URI is an IMAP URL that indicates that anonymous authentication must be used.</summary>*/
        public bool MustUseAnonymous => mURLParts != null && mURLParts.MustUseAnonymous;
        /**<summary>Indicates whether the URI is an IMAP home server referral URL.</summary>*/
        public bool IsHomeServerReferral => mURLParts != null && mURLParts.IsHomeServerReferral;
        /**<summary>Indicates whether the URI is an IMAP mailbox referral URL.</summary>*/
        public bool IsMailboxReferral => mURLParts != null && mURLParts.IsMailboxReferral;
        /**<summary>Indicates whether the URI is an IMAP mailbox search URL.</summary>*/
        public bool IsMailboxSearch => mURLParts != null && mURLParts.IsMailboxSearch;
        /**<summary>Indicates whether the URI is an IMAP message reference URL.</summary>*/
        public bool IsMessageReference => mURLParts != null && mURLParts.IsMessageReference;
        /**<summary>Indicates whether the URI is an IMAP URL that refers to part of a message.</summary>*/
        public bool IsPartial => mURLParts != null && mURLParts.IsPartial;
        /**<summary>Indicates whether the URI is suitable for use with RFC 4467 GENURLAUTH.</summary>*/
        public bool IsAuthorisable => mURLParts != null && mURLParts.IsAuthorisable;
        /**<summary>Indicates whether the URI is an authorized IMAP URL.</summary>*/
        public bool IsAuthorised => mURLParts != null && mURLParts.IsAuthorised;

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURI));
            lBuilder.Append(mParts);
            if (mURLParts != null) lBuilder.Append(mURLParts);
            return lBuilder.ToString();
        }

        /// <summary>
        /// Tries to parse a string as a URI.
        /// </summary>
        /// <param name="pURI"></param>
        /// <param name="rURI"></param>
        /// <returns></returns>
        public static bool TryParse(string pURI, out cURI rURI)
        {
            if (string.IsNullOrWhiteSpace(pURI)) { rURI = null; return false; }

            var lCursor = new cBytesCursor(pURI);
            if (!cURIParts.Process(lCursor, out var lParts, cTrace.cContext.None) || !lCursor.Position.AtEnd) { rURI = null; return false; };

            rURI = new cURI(pURI, lParts);
            return true;
        }

        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
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
