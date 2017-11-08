using System;

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
        /// <summary>
        /// The IMAP response text code associated with the response text
        /// </summary>
        /// <remarks>
        /// If there was no code 'none' is used. 
        /// If there was a code but it wasn't recognised then 'unknown' is used here and the text of the code is stored in UnknownCodeAtom.
        /// </remarks>
        public readonly eResponseTextCode Code;

        /// <summary>
        /// Data associated with the response text code
        /// </summary>
        /// <remarks>
        /// For badcharset it may contain the list of valid charsets.
        /// For referral it should contain the URL(s).
        /// </remarks>
        public readonly cStrings Strings; // for badcharset, referrals

        /// <summary>
        /// If the response text code was unrecognised the text of the code is made available here
        /// </summary>
        public readonly string UnknownCodeAtom;

        /// <summary>
        /// If the unrecognised response text code had text following it the text is made available here
        /// </summary>
        public readonly string UnknownCodeText;

        /// <summary>
        /// The response text
        /// </summary>
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
        /// <summary>
        /// The response text type
        /// </summary>
        /// <remarks>
        /// Indicates the situation in which the response text was received
        /// </remarks>
        public readonly eResponseTextType TextType;

        /// <summary>
        /// The response text
        /// </summary>
        public readonly cResponseText Text;

        public cResponseTextEventArgs(eResponseTextType pTextType, cResponseText pText)
        {
            TextType = pTextType;
            Text = pText;
        }

        public override string ToString()
        {
            return $"{nameof(cResponseTextEventArgs)}({TextType},{Text})";
        }
    }
}
