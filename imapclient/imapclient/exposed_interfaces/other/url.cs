using System;
using work.bacome.imapclient.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a parsed IMAP URL.
    /// </summary>
    /// <remarks>
    /// See RFC 5092 and RFC 5593.
    /// </remarks>
    /// <seealso cref="cIMAPClient.HomeServerReferral"/>
    /// <seealso cref="cIMAPClient.MailboxReferrals"/>
    public class cURL : IEquatable<cURL>
    {
        // IMAP URL (rfc 5092, 5593)

        /**<summary>The string that was parsed to initialise this instance.</summary>*/
        public readonly string OriginalString;
        private readonly cURLParts mParts;

        /// <summary>
        /// Initialises a new instance from the specified string. Will throw if the string cannot be parsed.
        /// </summary>
        /// <param name="pURL"></param>
        public cURL(string pURL)
        {
            if (string.IsNullOrEmpty(pURL)) throw new ArgumentOutOfRangeException(nameof(pURL));
            var lCursor = new cBytesCursor(pURL);
            if (!cURLParts.Process(lCursor, out mParts, cTrace.cContext.None) || !lCursor.Position.AtEnd) throw new ArgumentOutOfRangeException(nameof(pURL));

            OriginalString = pURL;
        }

        private cURL(string pURL, cURLParts pParts)
        {
            OriginalString = pURL;
            mParts = pParts;
        }

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
        /**<summary>Gets the parsed 'datetime' part of the 'expire' part of the URL. May be <see langword="null"/>.</summary>*/
        public DateTimeOffset? ExpireDateTimeOffset => mParts.ExpireDateTimeOffset;
        /**<summary>Gets the parsed 'datetime' part of the 'expire' part of the URL (in local time if there is useable time zone information). May be <see langword="null"/>.</summary>*/
        public DateTime? ExpireDateTime => mParts.ExpireDateTime;
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
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
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
            if (!cURLParts.Process(lCursor, out var lParts, cTrace.cContext.None) || !lCursor.Position.AtEnd) { rURL = null; return false; };

            rURL = new cURL(pURL, lParts);
            return true;
        }
    }
}