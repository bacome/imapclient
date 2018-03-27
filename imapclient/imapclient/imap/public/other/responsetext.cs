using System;
using System.Collections.Generic;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents the context in which response text was received.
    /// </summary>
    public enum eIMAPResponseTextContext
    {
        /**<summary>As part of an IMAP '* OK' greeting.</summary>*/
        greetingok,
        /**<summary>As part of an IMAP '* PREAUTH' greeting.</summary>*/
        greetingpreauth,
        /**<summary>As part of an IMAP '* BYE' greeting.</summary>*/
        greetingbye,
        /**<summary>As part of an IMAP continuation request.</summary>*/
        continuerequest,
        /**<summary>As part of an IMAP '* BYE'.</summary>*/
        bye,
        /**<summary>As part of an IMAP '* OK'.</summary>*/
        information,
        /**<summary>As part of an IMAP '* NO'.</summary>*/
        warning,
        /**<summary>As part of an IMAP '* BAD'.</summary>*/
        error,
        /**<summary>As part of an IMAP command success.</summary>*/
        success,
        /**<summary>As part of an IMAP command failure.</summary>*/
        failure,
        /**<summary>As part of an IMAP authentication cancellation.</summary>*/
        authenticationcancelled,
        /**<summary>As part of an IMAP protocol error command termination.</summary>*/
        protocolerror
    }
    
    /// <summary>
    /// Represents the code associated with response text.
    /// </summary>
    public enum eIMAPResponseTextCode
    {
        /**<summary>There was no code.</summary>*/
        none,

        /**<summary>There was a code, but there isn't a <see cref="eIMAPResponseTextCode"/> value for it.</summary>*/
        other,

        // rfc 3501

        /**<summary>RFC 3501 ALERT.</summary>*/
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
        unknowncte,

        /**<summary>RFC 4469 BADURL.</summary>*/
        badurl,
        /**<summary>RFC 4469 TOOBIG: the resulting message is too big.</summary>*/
        toobig
    }

    /// <summary>
    /// Contains IMAP response text.
    /// </summary>
    /// <remarks>
    /// If <see cref="Code"/> is <see cref="eIMAPResponseTextCode.badcharset"/> then <see cref="Arguments"/> may contain a list of supported character sets.
    /// If <see cref="Code"/> is <see cref="eIMAPResponseTextCode.referral"/> then <see cref="Arguments"/> should contain URI(s).
    /// </remarks>
    public class cIMAPResponseText
    {
        /// <summary>
        /// The response code associated with the response text as a string, may be <see langword="null"/>.
        /// </summary>
        public readonly string CodeText;

        /// <summary>
        /// The response code arguments associated with the response text as a string, may be <see langword="null"/>.
        /// </summary>
        public readonly string ArgumentsText;

        /// <summary>
        /// The response code associated with the response text as a code.
        /// </summary>
        /// <inheritdoc cref="cIMAPResponseText" select="remarks"/>
        public readonly eIMAPResponseTextCode Code;

        /// <summary>
        /// The response code arguments associated with the response text in list form, may be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="cIMAPResponseText" select="remarks"/>
        public readonly cStrings Arguments; // for badcharset, referrals

        /// <summary>
        /// The response text.
        /// </summary>
        public readonly string Text;
    
        internal readonly bool CodeIsAlwaysAnError;

        internal cIMAPResponseText(string pText)
        {
            CodeText = null;
            ArgumentsText = null;
            Code = eIMAPResponseTextCode.none;
            Arguments = null;
            Text = pText;
            CodeIsAlwaysAnError = false;
        }

        internal cIMAPResponseText(IList<byte> pCodeText, eIMAPResponseTextCode pCode, bool pCodeIsAlwaysAnError, string pText)
        {
            CodeText = cTools.ASCIIBytesToString(pCodeText);
            ArgumentsText = null;
            Code = pCode;
            Arguments = null;
            Text = pText;
            CodeIsAlwaysAnError = pCodeIsAlwaysAnError;
        }

        internal cIMAPResponseText(IList<byte> pCodeText, IList<byte> pArgumentsText, eIMAPResponseTextCode pCode, bool pCodeIsAlwaysAnError, cStrings pArguments, string pText)
        {
            CodeText = cTools.ASCIIBytesToString(pCodeText);
            ArgumentsText = cTools.UTF8BytesToString(pArgumentsText);
            Code = pCode;
            Arguments = pArguments;
            Text = pText;
            CodeIsAlwaysAnError = pCodeIsAlwaysAnError;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIMAPResponseText));
            lBuilder.Append(CodeText);
            lBuilder.Append(ArgumentsText);
            lBuilder.Append(Text);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Carries IMAP response text.
    /// </summary>
    public class cIMAPResponseTextEventArgs : EventArgs
    {
        /// <summary>
        /// The context in which the response text was received.
        /// </summary>
        public readonly eIMAPResponseTextContext Context;

        /// <summary>
        /// The response text.
        /// </summary>
        public readonly cIMAPResponseText Text;

        internal cIMAPResponseTextEventArgs(eIMAPResponseTextContext pContext, cIMAPResponseText pText)
        {
            Context = pContext;
            Text = pText;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cIMAPResponseTextEventArgs)}({Context},{Text})";
    }
}
