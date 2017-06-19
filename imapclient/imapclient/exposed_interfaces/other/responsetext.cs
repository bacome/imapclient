using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public enum eResponseTextType
    {
        greeting, continuerequest, bye,
        information, warning, error,
        success, failure, authenticationcancelled, protocolerror
    }

    public enum eResponseTextCode
    {
        none, unknown,
        alert, badcharset, parse, trycreate, // rfc 3501
        unavailable, authenticationfailed, authorizationfailed, expired, privacyrequired, contactadmin, noperm, inuse, expungeissued, corruption, serverbug, clientbug, cannot, limit, overquota, alreadyexists, nonexistent, // rfc 5530
        referral, // rfc 2193
        useattr, // rfc 6154
        unknowncte // rfc 3516
    }

    public class cResponseText
    {
        public readonly eResponseTextCode Code;
        public readonly cStrings Strings; // for badcharset, referrals
        public readonly string UnknownCodeAtom;
        public readonly string UnknownCodeText;
        public readonly string Text;

        public cResponseText(string pText)
        {
            Code = eResponseTextCode.none;
            Strings = null;
            UnknownCodeAtom = null;
            UnknownCodeText = null;
            Text = pText;
        }

        public cResponseText(eResponseTextCode pCode, string pText)
        {
            Code = pCode;
            Strings = null;
            UnknownCodeAtom = null;
            UnknownCodeText = null;
            Text = pText;
        }

        public cResponseText(eResponseTextCode pCode, cStrings pStrings, string pText)
        {
            Code = pCode;
            Strings = pStrings;
            UnknownCodeAtom = null;
            UnknownCodeText = null;
            Text = pText;
        }

        public cResponseText(string pUnknownCodeAtom, string pUnknownCodeText, string pText)
        {
            Code = eResponseTextCode.unknown;
            Strings = null;
            UnknownCodeAtom = pUnknownCodeAtom;
            UnknownCodeText = pUnknownCodeText;
            Text = pText;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cResponseText));
            lBuilder.Append(Code);
            if (Strings != null) lBuilder.Append(Strings);
            if (UnknownCodeAtom != null) lBuilder.Append(UnknownCodeAtom);
            if (UnknownCodeText != null) lBuilder.Append(UnknownCodeText);
            lBuilder.Append(Text);
            return lBuilder.ToString();
        }
    }

    public class cResponseTextEventArgs : EventArgs
    {
        public readonly eResponseTextType ResponseTextType;
        public readonly cResponseText ResponseText;

        public cResponseTextEventArgs(eResponseTextType pResponseTextType, cResponseText pResponseText)
        {
            ResponseTextType = pResponseTextType;
            ResponseText = pResponseText;
        }

        public override string ToString()
        {
            return $"{nameof(cResponseTextEventArgs)}({ResponseTextType},{ResponseText})";
        }
    }
}
