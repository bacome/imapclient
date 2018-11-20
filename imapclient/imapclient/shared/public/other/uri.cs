using System;
using System.Collections.Generic;
using work.bacome.imapinternals;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a parsed URI.
    /// </summary>
    /// <remarks>
    /// See RFC 3986 and RFC 6874.
    /// </remarks>
    public class cURI : iURL, IEquatable<cURI>
    {
        // rfc 3986, 6874

        /**<summary>The string that was parsed to initialise this instance.</summary>*/
        private readonly string mURI;
        private readonly cURIParts mParts;
        private readonly cURLParts mURLParts;

        /// <summary>
        /// Initialises a new instance from the specified string. Will throw if the string cannot be parsed.
        /// </summary>
        /// <param name="pURI"></param>
        public cURI(string pURI)
        {
            if (string.IsNullOrWhiteSpace(pURI)) throw new ArgumentOutOfRangeException(nameof(pURI));
            var lCursor = new cBytesCursor(pURI);
            var lBookmark = lCursor.Position;
            if (!lCursor.ProcessURIParts(out mParts) || !lCursor.Position.AtEnd) throw new ArgumentOutOfRangeException(nameof(pURI));
            lCursor.Position = lBookmark;
            if (!lCursor.ProcessURLParts(out mURLParts) || !lCursor.Position.AtEnd) mURLParts = null;
            mURI = pURI;
        }

        /// <summary>
        /// Initialises a new instance from the specified ASCII bytes. Will throw if the bytes cannot be parsed.
        /// </summary>
        /// <param name="pURI"></param>
        public cURI(IList<byte> pURI)
        {
            if (pURI == null) throw new ArgumentNullException(nameof(pURI));
            if (pURI.Count == 0) throw new ArgumentOutOfRangeException(nameof(pURI));
            var lCursor = new cBytesCursor(pURI);
            var lBookmark = lCursor.Position;
            if (!lCursor.ProcessURIParts(out mParts) || !lCursor.Position.AtEnd) throw new ArgumentOutOfRangeException(nameof(pURI));
            lCursor.Position = lBookmark;
            if (!lCursor.ProcessURLParts(out mURLParts) || !lCursor.Position.AtEnd) mURLParts = null;
            mURI = cTools.UTF8BytesToString(pURI);
        }

        private cURI(string pURI, cURIParts pParts, cURLParts pURLParts)
        {
            mURI = pURI ?? throw new ArgumentNullException(nameof(pURI));
            mParts = pParts ?? throw new ArgumentNullException(nameof(pParts));
            mURLParts = pURLParts;
        }

        /**<summary>Gets the URI as a string.</summary>*/
        public string URI => mURI;

        /**<summary>Gets the URL as a string if the URI is a valid IMAP URL. May be <see langword="null"/>.</summary>*/
        public string URL
        {
            get
            {
                if (mURLParts == null) return null;
                return mURI;
            }
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
        /**<summary>Gets the punycode decoded 'host' part of the URI. May be <see langword="null"/>.</summary>*/
        public string DisplayHost => mParts.DisplayHost;
        /**<summary>Gets the 'port' part of the URI. May be <see langword="null"/>.</summary>*/
        public string PortString => mParts.Port;

        /**<summary>Gets the <see cref="cURL.Port"/> if the URI is a valid IMAP URL, otherwise -1.</summary>*/
        public int Port
        {
            get
            {
                if (mURLParts == null) return -1;
                return mURLParts.Port;
            }
        }

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
        public cTimestamp Expire => mURLParts?.Expire;
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cURI pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cURI;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            if (mURLParts == null) return mParts.GetHashCode();
            return mURLParts.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURI));
            lBuilder.Append(mParts);
            if (mURLParts != null) lBuilder.Append(mURLParts);
            return lBuilder.ToString();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cURI pA, cURI pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            if (pA.mURLParts != null) return pA.mURLParts.Equals(pB.mURLParts);
            if (pB.mURLParts != null) return false;

            return pA.mParts.Equals(pB.mParts);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cURI pA, cURI pB) => !(pA == pB);

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
            var lBookmark = lCursor.Position;
            if (!lCursor.ProcessURIParts(out var lParts) || !lCursor.Position.AtEnd) { rURI = null; return false; }
            lCursor.Position = lBookmark;
            if (!lCursor.ProcessURLParts(out var lURLParts) || !lCursor.Position.AtEnd) lURLParts = null;
            rURI = new cURI(pURI, lParts, lURLParts);
            return true;
        }
    }
}
