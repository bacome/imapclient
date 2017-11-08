using System;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public class cURL
    {
        // IMAP URL (rfc 5092, 5593)

        public readonly string OriginalString;
        private readonly cURLParts mParts;

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

        public string UserId => mParts.UserId;
        public string MechanismName => mParts.MechanismName;
        public string Host => mParts.Host;
        public int Port => mParts.Port;
        public string MailboxPath => mParts.MailboxPath;
        public uint? UIDValidity => mParts.UIDValidity;
        public string Search => mParts.Search;
        public uint? UID => mParts.UID;
        public string Section => mParts.Section;
        public uint? PartialOffset => mParts.PartialOffset;
        public uint? PartialLength => mParts.PartialLength;
        public DateTime? Expire => mParts.Expire;
        public string Application => mParts.Application;
        public string AccessUserId => mParts.AccessUserId;
        public string TokenMechanism => mParts.TokenMechanism;
        public string Token => mParts.Token;

        public bool MustUseAnonymous => mParts.MustUseAnonymous;
        public bool IsHomeServerReferral => mParts.IsHomeServerReferral;
        public bool IsMailboxReferral => mParts.IsMailboxReferral;
        public bool IsMailboxSearch => mParts.IsMailboxSearch;
        public bool IsMessageReference => mParts.IsMessageReference;
        public bool IsPartial => mParts.IsPartial;
        public bool IsAuthorisable => mParts.IsAuthorisable;
        public bool IsAuthorised => mParts.IsAuthorised;

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURL));
            lBuilder.Append(mParts);
            return lBuilder.ToString();
        }

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