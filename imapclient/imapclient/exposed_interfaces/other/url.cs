using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a parsed IMAP URL.
    /// </summary>
    /// <seealso cref="cIMAPClient.HomeServerReferral"/>
    /// <seealso cref="cIMAPClient.MailboxReferrals"/>
    public class cURL
    {
        // IMAP URL (rfc 5092, 5593)

        /**<summary>The string that the instance was constructed from.</summary>*/
        public readonly string OriginalString;
        private readonly cURLParts mParts;

        /// <summary>
        /// Initialises a new instance. Will throw if the string is not a valid IMAP URL.
        /// </summary>
        /// <param name="pURL"></param>
        public cURL(string pURL)
        {
            if (string.IsNullOrEmpty(pURL)) throw new ArgumentOutOfRangeException(nameof(pURL));
            var lCursor = new cBytesCursor(pURL);
            if (!cURLParts.Process(lCursor, out mParts, cTrace.cContext.Null) || !lCursor.Position.AtEnd) throw new ArgumentOutOfRangeException(nameof(pURL));

            OriginalString = pURL;
        }

        private cURL(string pURL, cURLParts pParts)
        {
            OriginalString = pURL;
            mParts = pParts;
        }

        /**<summary>Gets the userid.</summary>*/
        public string UserId => mParts.UserId;
        /**<summary>Gets the SASL mechanism name.</summary>*/
        public string MechanismName => mParts.MechanismName;
        /**<summary>Gets the host.</summary>*/
        public string Host => mParts.Host;
        /**<summary>Gets the port.</summary>*/
        public int Port => mParts.Port;
        /**<summary>Gets the mailbox path.</summary>*/
        public string MailboxPath => mParts.MailboxPath;
        /**<summary>Gets the UIDValidity.</summary>*/
        public uint? UIDValidity => mParts.UIDValidity;
        public string Search => mParts.Search;
        /**<summary>Gets the UID.</summary>*/
        public uint? UID => mParts.UID;
        public string Section => mParts.Section;
        public uint? PartialOffset => mParts.PartialOffset;
        public uint? PartialLength => mParts.PartialLength;
        public DateTime? Expire => mParts.Expire;
        public string Application => mParts.Application;
        public string AccessUserId => mParts.AccessUserId;
        public string TokenMechanism => mParts.TokenMechanism;
        public string Token => mParts.Token;

        /**<summary>Determines if the URL requires that anonymous authentication be used.</summary>*/
        public bool MustUseAnonymous => mParts.MustUseAnonymous;
        /**<summary>Determines if the URL is a valid home server referral.</summary>*/
        public bool IsHomeServerReferral => mParts.IsHomeServerReferral;
        /**<summary>Determines if the URL is a valid mailbox referral.</summary>*/
        public bool IsMailboxReferral => mParts.IsMailboxReferral;
        /**<summary>Determines if the URL is a valid mailbox search URL.</summary>*/
        public bool IsMailboxSearch => mParts.IsMailboxSearch;
        public bool IsMessageReference => mParts.IsMessageReference;
        public bool IsPartial => mParts.IsPartial;
        public bool IsAuthorisable => mParts.IsAuthorisable;
        public bool IsAuthorised => mParts.IsAuthorised;

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURL));
            lBuilder.Append(mParts);
            return lBuilder.ToString();
        }

        /// <summary>
        /// Tries to parse a string into an IMAP URL.
        /// </summary>
        /// <param name="pURL"></param>
        /// <param name="rURL"></param>
        /// <returns></returns>
        public static bool TryParse(string pURL, out cURL rURL)
        {
            if (string.IsNullOrWhiteSpace(pURL)) { rURL = null; return false; }

            var lCursor = new cBytesCursor(pURL);
            if (!cURLParts.Process(lCursor, out var lParts, cTrace.cContext.Null) || !lCursor.Position.AtEnd) { rURL = null; return false; };

            rURL = new cURL(pURL, lParts);
            return true;
        }
    }
}