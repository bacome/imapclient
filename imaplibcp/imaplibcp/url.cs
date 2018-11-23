using System;
using System.Collections.Generic;
using work.bacome.imapinternals;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Defines properties for getting IMAP URL details.
    /// </summary>
    public interface iURL
    {
        /** <inheritdoc cref="cURL.URL"/> **/
        string URL { get; }
        /** <inheritdoc cref="cURL.UserId"/> **/
        string UserId { get; }
        /** <inheritdoc cref="cURL.MechanismName"/> **/
        string MechanismName { get; }
        /** <inheritdoc cref="cURL.Port"/> **/
        int Port { get; }
        /** <inheritdoc cref="cURL.MailboxPath"/> **/
        string MailboxPath { get; }
        /** <inheritdoc cref="cURL.UIDValidity"/> **/
        uint? UIDValidity { get; }
        /** <inheritdoc cref="cURL.UID"/> **/
        uint? UID { get; }
        /** <inheritdoc cref="cURL.Section"/> **/
        string Section { get; }
        /** <inheritdoc cref="cURL.PartialOffset"/> **/
        uint? PartialOffset { get; }
        /** <inheritdoc cref="cURL.PartialLength"/> **/
        uint? PartialLength { get; }
        /** <inheritdoc cref="cURL.Expire"/> **/
        cTimestamp Expire { get; }
        /** <inheritdoc cref="cURL.Application"/> **/
        string Application { get; }
        /** <inheritdoc cref="cURL.AccessUserId"/> **/
        string AccessUserId { get; }
        /** <inheritdoc cref="cURL.TokenMechanism"/> **/
        string TokenMechanism { get; }
        /** <inheritdoc cref="cURL.Token"/> **/
        string Token { get; }
        /** <inheritdoc cref="cURL.Search"/> **/
        string Search { get; }
        /** <inheritdoc cref="cURL.MustUseAnonymous"/> **/
        bool MustUseAnonymous { get; }
        /** <inheritdoc cref="cURL.IsHomeServerReferral"/> **/
        bool IsHomeServerReferral { get; }
        /** <inheritdoc cref="cURL.IsMailboxReferral"/> **/
        bool IsMailboxReferral { get; }
        /** <inheritdoc cref="cURL.IsMailboxSearch"/> **/
        bool IsMailboxSearch { get; }
        /** <inheritdoc cref="cURL.IsMessageReference"/> **/
        bool IsMessageReference { get; }
        /** <inheritdoc cref="cURL.IsPartial"/> **/
        bool IsPartial { get; }
        /** <inheritdoc cref="cURL.IsAuthorisable"/> **/
        bool IsAuthorisable { get; }
        /** <inheritdoc cref="cURL.IsAuthorised"/> **/
        bool IsAuthorised { get; }
    }

    /// <summary>
    /// Represents a parsed IMAP URL.
    /// </summary>
    /// <remarks>
    /// See RFC 5092 and RFC 5593.
    /// </remarks>
    public class cURL : iURL, IEquatable<cURL>
    {
        // IMAP URL (rfc 5092, 5593)

        private readonly string mURL;
        private readonly cURLParts mParts;

        /// <summary>
        /// Initialises a new instance from the specified string. Will throw if the string cannot be parsed.
        /// </summary>
        /// <param name="pURL"></param>
        public cURL(string pURL)
        {
            if (string.IsNullOrWhiteSpace(pURL)) throw new ArgumentOutOfRangeException(nameof(pURL));
            var lCursor = new cBytesCursor(pURL);
            if (!lCursor.ProcessURLParts(out mParts) || !lCursor.Position.AtEnd) throw new ArgumentOutOfRangeException(nameof(pURL));
            mURL = pURL;
        }

        /// <summary>
        /// Initialises a new instance from the specified ASCII bytes. Will throw if the bytes cannot be parsed.
        /// </summary>
        /// <param name="pURL"></param>
        public cURL(IList<byte> pURL)
        {
            if (pURL == null) throw new ArgumentNullException(nameof(pURL));
            if (pURL.Count == 0) throw new ArgumentOutOfRangeException(nameof(pURL));
            var lCursor = new cBytesCursor(pURL);
            if (!lCursor.ProcessURLParts(out mParts) || !lCursor.Position.AtEnd) throw new ArgumentOutOfRangeException(nameof(pURL));
            mURL = cTools.UTF8BytesToString(pURL);
        }

        private cURL(string pURL, cURLParts pParts)
        {
            mURL = pURL ?? throw new ArgumentNullException(nameof(pURL));
            mParts = pParts ?? throw new ArgumentNullException(nameof(pParts));
        }

        /**<summary>Gets the URL as a string.</summary>*/
        public string URL => mURL;
        /**<summary>Gets the decoded 'enc-user' part of the 'iuserinfo' part of the URL. May be <see langword="null"/>.</summary>*/
        public string UserId => mParts.UserId;
        /**<summary>Gets the decoded 'enc-auth-type' from the 'iauth' part of the URL (if iauth is ';AUTH=*' this returns <see langword="null"/>). May be <see langword="null"/>.</summary>*/
        public string MechanismName => mParts.MechanismName;
        /**<summary>Gets the 'host' part of the URL. May be <see langword="null"/>.</summary>*/
        public string Host => mParts.Host;
        /**<summary>Gets the punycode decoded 'host' part of the URL. May be <see langword="null"/>.</summary>*/
        public string DisplayHost => mParts.DisplayHost;
        /**<summary>Gets the 'port' part of the URL. Will be 143 if the port isn't specified in the URL.</summary>*/
        public int Port => mParts.Port;
        /**<summary>Gets the decoded 'enc-mailbox' part of the URL. May be <see langword="null"/>.</summary>*/
        public string MailboxPath => mParts.MailboxPath;
        /**<summary>Gets the 'uidvalidity' part of the URL. May be <see langword="null"/>.</summary>*/
        public uint? UIDValidity => mParts.UIDValidity;
        /**<summary>Gets the decoded 'enc-search' part of the URL. May be <see langword="null"/>.</summary>*/
        public string Search => mParts.Search;
        /**<summary>Gets the 'iuid' part of the URL. May be <see langword="null"/>.</summary>*/
        public uint? UID => mParts.UID;
        /**<summary>Gets the decoded 'isection' part of the URL. May be <see langword="null"/>.</summary>*/
        public string Section => mParts.Section;
        /**<summary>Gets the 'offset' part of the 'partial-range' part of the URL. May be <see langword="null"/>.</summary>*/
        public uint? PartialOffset => mParts.PartialOffset;
        /**<summary>Gets the 'length' part of the 'partial-range' part of the URL. May be <see langword="null"/>.</summary>*/
        public uint? PartialLength => mParts.PartialLength;
        /**<summary>Gets the 'expire' part of the URL. May be <see langword="null"/>.</summary>*/
        public cTimestamp Expire => mParts.Expire;
        /**<summary>Gets the 'application' part of the 'access identifier' part of the URL. May be <see langword="null"/>.</summary>*/
        public string Application => mParts.Application;
        /**<summary>Gets the decoded 'enc-user' part of the 'access identifier' part of the URL. May be <see langword="null"/>.</summary>*/
        public string AccessUserId => mParts.AccessUserId;
        /**<summary>Gets the 'uauth-mechanism' part of the URL. May be <see langword="null"/>.</summary>*/
        public string TokenMechanism => mParts.TokenMechanism;
        /**<summary>Gets the 'enc-urlauth' part of the URL. May be <see langword="null"/>.</summary>*/
        public string Token => mParts.Token;

        /**<summary>Indicates whether the URL indicates that anonymous authentication must be used.</summary>*/
        public bool MustUseAnonymous => mParts.MustUseAnonymous;
        /**<summary>Indicates whether the URL is a home server referral URL.</summary>*/
        public bool IsHomeServerReferral => mParts.IsHomeServerReferral;
        /**<summary>Indicates whether the URL is a mailbox referral URL.</summary>*/
        public bool IsMailboxReferral => mParts.IsMailboxReferral;
        /**<summary>Indicates whether the URL is a mailbox search URL.</summary>*/
        public bool IsMailboxSearch => mParts.IsMailboxSearch;
        /**<summary>Indicates whether the URL is a message reference URL.</summary>*/
        public bool IsMessageReference => mParts.IsMessageReference;
        /**<summary>Indicates whether the URL refers to part of a message.</summary>*/
        public bool IsPartial => mParts.IsPartial;
        /**<summary>Indicates whether the URL is suitable for use with RFC 4467 GENURLAUTH.</summary>*/
        public bool IsAuthorisable => mParts.IsAuthorisable;
        /**<summary>Indicates whether the URL is an authorized URL.</summary>*/
        public bool IsAuthorised => mParts.IsAuthorised;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cURL pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cURL;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode() => mParts.GetHashCode();

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURL));
            lBuilder.Append(mParts);
            return lBuilder.ToString();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cURL pA, cURL pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.mParts.Equals(pB.mParts);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cURL pA, cURL pB) => !(pA == pB);

        /// <summary>
        /// Tries to parse a string as an IMAP URL.
        /// </summary>
        /// <param name="pURL"></param>
        /// <param name="rURL"></param>
        /// <returns></returns>
        public static bool TryParse(string pURL, out cURL rURL)
        {
            if (string.IsNullOrWhiteSpace(pURL)) { rURL = null; return false; }
            var lCursor = new cBytesCursor(pURL);
            if (!lCursor.ProcessURLParts(out var lParts) || !lCursor.Position.AtEnd) { rURL = null; return false; }
            rURL = new cURL(pURL, lParts);
            return true;
        }
    }
}