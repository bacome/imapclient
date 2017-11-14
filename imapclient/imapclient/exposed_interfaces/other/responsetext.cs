using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The type of IMAP response text.
    /// </summary>
    /// <seealso cref="cResponseTextEventArgs.TextType"/>
    public enum eResponseTextType
    {
        /**<summary>Response text associated with an IMAP greeting.</summary>*/
        greeting,
        /**<summary>Response text associated with an IMAP command continuation request.</summary>*/
        continuerequest,
        /**<summary>Response text associated with an IMAP BYE.</summary>*/
        bye,
        /**<summary>IMAP information text.</summary>*/
        information,
        /**<summary>IMAP warning text.</summary>*/
        warning,
        /**<summary>IMAP error text.</summary>*/
        error,
        /**<summary>Response text associated with an IMAP command success notification.</summary>*/
        success,
        /**<summary>Response text associated with an IMAP command failure notification.</summary>*/
        failure,
        /**<summary>Response text associated with an IMAP authentication cancellation notification.</summary>*/
        authenticationcancelled,
        /**<summary>Response text associated with an IMAP command protocol error notification.</summary>*/
        protocolerror
    }

    /// <summary>
    /// The text code associated with IMAP response text. See <see cref="cResponseText.Code"/>.
    /// </summary>
    public enum eResponseTextCode
    {
        /**<summary>There was no code.</summary>*/
        none,
        /**<summary>There was a code, but it wasn't recognised.</summary>*/
        unknown,

        // rfc 3501

        /**<summary>RFC 3501 ALERT: the text is an alert.</summary>*/
        alert,
        /**<summary>RFC 3501 BADCHARSET.</summary>*/
        badcharset,
        /**<summary>RFC 3501 PARSE: there was an error parsing a message.</summary>*/
        parse,
        /**<summary>RFC 3501 TRYCREATE: try creating the mailbox.</summary>*/
        trycreate,

        // rfc 5530

        /**<summary>RFC 5530 UNAVAILABLE.</summary>*/
        unavailable,
        /**<summary>RFC 5530 AUTHENTICATIONFAILED.</summary>*/
        authenticationfailed,
        /**<summary>RFC 5530 AUTHORIZATIONFAILED.</summary>*/
        authorizationfailed,
        /**<summary>RFC 5530 EXPIRED.</summary>*/
        expired,
        /**<summary>RFC 5530 PRIVACYREQUIRED.</summary>*/
        privacyrequired,
        /**<summary>RFC 5530 CONTACTADMIN.</summary>*/
        contactadmin,
        /**<summary>RFC 5530 NOPERM.</summary>*/
        noperm,
        /**<summary>RFC 5530 INUSE.</summary>*/
        inuse,
        /**<summary>RFC 5530 EXPUNGEISSUED.</summary>*/
        expungeissued,
        /**<summary>RFC 5530 CORRUPTION.</summary>*/
        corruption,
        /**<summary>RFC 5530 SERVERBUG.</summary>*/
        serverbug,
        /**<summary>RFC 5530 CLIENTBUG.</summary>*/
        clientbug,
        /**<summary>RFC 5530 CANNOT.</summary>*/
        cannot,
        /**<summary>RFC 5530 LIMIT.</summary>*/
        limit,
        /**<summary>RFC 5530 OVERQUOTA.</summary>*/
        overquota,
        /**<summary>RFC 5530 ALREADYEXISTS.</summary>*/
        alreadyexists,
        /**<summary>RFC 5530 NONEXISTENT.</summary>*/
        nonexistent,

        /**<summary>RFC 2193 REFERRAL.</summary>*/
        referral, 

        /**<summary>RFC 6154 USEATTR.</summary>*/
        useattr, 

        /**<summary>RFC 3516 UNKNOWNCTE: the server can't decode the content.</summary>*/
        unknowncte
    }

    /// <summary>
    /// IMAP response text.
    /// </summary>
    /// <seealso cref="cResponseTextEventArgs.Text"/>
    /// <seealso cref="cUnsuccessfulCompletionException.ResponseText"/>
    /// <seealso cref="cConnectByeException.ResponseText"/>
    /// <seealso cref="cHomeServerReferralException.ResponseText"/>
    /// <seealso cref="cCredentialsException.ResponseText"/>
    /// <seealso cref="cCommandResult.ResponseText"/>
    public class cResponseText
    {
        /// <summary>
        /// The code associated with the response text. If this is <see cref="eResponseTextCode.unknown"/> then the text of the code is in <see cref="UnknownCodeAtom"/>.
        /// </summary>
        public readonly eResponseTextCode Code;

        /// <summary>
        /// The data associated with the <see cref="Code"/>. 
        /// If the code is <see cref="eResponseTextCode.badcharset"/> this may contain a list of valid charsets.
        /// If the code is <see cref="eResponseTextCode.referral"/> this should contain the URI(s).
        /// </summary>
        public readonly cStrings Strings; // for badcharset, referrals

        /// <summary>
        /// If the <see cref="Code"/> is <see cref="eResponseTextCode.unknown"/> this is the text of the code, otherwise null.
        /// </summary>
        public readonly string UnknownCodeAtom;

        /// <summary>
        /// If the <see cref="Code"/> is <see cref="eResponseTextCode.unknown"/> this is the text following the code, otherwise null. (May also be null if there was no text.)
        /// </summary>
        public readonly string UnknownCodeText;

        /// <summary>
        /// The response text.
        /// </summary>
        public readonly string Text;

        internal cResponseText(string pText)
        {
            Code = eResponseTextCode.none;
            Strings = null;
            UnknownCodeAtom = null;
            UnknownCodeText = null;
            Text = pText;
        }

        internal cResponseText(eResponseTextCode pCode, string pText)
        {
            Code = pCode;
            Strings = null;
            UnknownCodeAtom = null;
            UnknownCodeText = null;
            Text = pText;
        }

        internal cResponseText(eResponseTextCode pCode, cStrings pStrings, string pText)
        {
            Code = pCode;
            Strings = pStrings;
            UnknownCodeAtom = null;
            UnknownCodeText = null;
            Text = pText;
        }

        internal cResponseText(string pUnknownCodeAtom, string pUnknownCodeText, string pText)
        {
            Code = eResponseTextCode.unknown;
            Strings = null;
            UnknownCodeAtom = pUnknownCodeAtom;
            UnknownCodeText = pUnknownCodeText;
            Text = pText;
        }

        /**<summary>Returns a string that represents the instance.</summary>*/
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

    /// <summary>
    /// See <see cref="cIMAPClient.ResponseText"/>.
    /// </summary>
    public class cResponseTextEventArgs : EventArgs
    {
        /// <summary>
        /// The response text type. This indicates the situation in which the response text was received
        /// </summary>
        public readonly eResponseTextType TextType;

        /// <summary>
        /// The response text.
        /// </summary>
        public readonly cResponseText Text;

        internal cResponseTextEventArgs(eResponseTextType pTextType, cResponseText pText)
        {
            TextType = pTextType;
            Text = pText;
        }

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString()
        {
            return $"{nameof(cResponseTextEventArgs)}({TextType},{Text})";
        }
    }
}
