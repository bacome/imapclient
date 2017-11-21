﻿using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a parsed IMAP URL. See RFC 5092 and RFC 5593 for terminology.
    /// </summary>
    /// <seealso cref="cIMAPClient.HomeServerReferral"/>
    /// <seealso cref="cIMAPClient.MailboxReferrals"/>
    public class cURL
    {
        // IMAP URL (rfc 5092, 5593)

        /**<summary>The URL in string form.</summary>*/
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

        /**<summary>Gets the decoded enc-user part of the iuserinfo part of the URL. May be <see langword="null"/>.</summary>*/
        public string UserId => mParts.UserId;
        /**<summary>Gets the decoded enc-auth-type from the iauth part of the URL (if iauth is ';AUTH=*' this returns <see langword="null"/>). May be <see langword="null"/>.</summary>*/
        public string MechanismName => mParts.MechanismName;
        /**<summary>Gets the host part of the URL. May be <see langword="null"/>.</summary>*/
        public string Host => mParts.Host;
        /**<summary>Gets the port part of the URL. Will be 143 if it isn't specified in the URL.</summary>*/
        public int Port => mParts.Port;
        /**<summary>Gets the decoded enc-mailbox part of the URL. May be <see langword="null"/>.</summary>*/
        public string MailboxPath => mParts.MailboxPath;
        /**<summary>Gets the uidvalidity part of the URL. May be <see langword="null"/>.</summary>*/
        public uint? UIDValidity => mParts.UIDValidity;
        /**<summary>Gets the decoded enc-search part of the URL. May be <see langword="null"/>.</summary>*/
        public string Search => mParts.Search;
        /**<summary>Gets the iuid part of the URL. May be <see langword="null"/>.</summary>*/
        public uint? UID => mParts.UID;
        /**<summary>Gets the decoded isection part of the URL. May be <see langword="null"/>.</summary>*/
        public string Section => mParts.Section;
        /**<summary>Gets the from part of the partial-range of the URL. May be <see langword="null"/>.</summary>*/
        public uint? PartialOffset => mParts.PartialOffset;
        /**<summary>Gets the length part of the partial-range of the URL. May be <see langword="null"/>.</summary>*/
        public uint? PartialLength => mParts.PartialLength;
        /**<summary>Gets the datetime of the expire part of the URL. May be <see langword="null"/>.</summary>*/
        public DateTime? Expire => mParts.Expire;
        /**<summary>Gets the application part of the access identifier part of the URL. May be <see langword="null"/>.</summary>*/
        public string Application => mParts.Application;
        /**<summary>Gets the decoded enc-user part of the access identifier part of the URL. May be <see langword="null"/>.</summary>*/
        public string AccessUserId => mParts.AccessUserId;
        /**<summary>Gets the uauth-mechanism part of the URL. May be <see langword="null"/>.</summary>*/
        public string TokenMechanism => mParts.TokenMechanism;
        /**<summary>Gets the enc-urlauth part of the URL. May be <see langword="null"/>.</summary>*/
        public string Token => mParts.Token;

        /**<summary>Indicates whether the URL requires that anonymous authentication be used.</summary>*/
        public bool MustUseAnonymous => mParts.MustUseAnonymous;
        /**<summary>Indicates whether the URL is a valid home server referral.</summary>*/
        public bool IsHomeServerReferral => mParts.IsHomeServerReferral;
        /**<summary>Indicates whether the URL is a valid mailbox referral.</summary>*/
        public bool IsMailboxReferral => mParts.IsMailboxReferral;
        /**<summary>Indicates whether the URL is a valid mailbox search URL.</summary>*/
        public bool IsMailboxSearch => mParts.IsMailboxSearch;
        /**<summary>Indicates whether the URL is a valid message reference URL.</summary>*/
        public bool IsMessageReference => mParts.IsMessageReference;
        /**<summary>Indicates whether the URL refers to part of a message.</summary>*/
        public bool IsPartial => mParts.IsPartial;
        /**<summary>Indicates whether the URL is suitable for use with RFC 4467 GENURLAUTH.</summary>*/
        public bool IsAuthorisable => mParts.IsAuthorisable;
        /**<summary>Indicates whether the URL is authorized.</summary>*/
        public bool IsAuthorised => mParts.IsAuthorised;

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURL));
            lBuilder.Append(mParts);
            return lBuilder.ToString();
        }

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