using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents the context in which response text was received.
    /// </summary>
    /// <seealso cref="cResponseTextEventArgs"/>
    public enum eResponseTextContext
    {
        /**<summary>As part of an IMAP greeting.</summary>*/
        greeting,
        /**<summary>As part of an IMAP continuation request.</summary>*/
        continuerequest,
        /**<summary>As part of an IMAP bye.</summary>*/
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
        /**<summary>As part of IMAP authentication cancellation.</summary>*/
        authenticationcancelled,
        /**<summary>As part of an IMAP protocol error command termination.</summary>*/
        protocolerror
    }
    
    /// <summary>
    /// Represents the code associated with response text.
    /// </summary>
    /// <seealso cref="cResponseText"/>
    public enum eResponseTextCode
    {
        /**<summary>There was no code.</summary>*/
        none,

        /**<summary>There was a code, but there isn't a <see cref="eResponseTextCode"/> value for it.</summary>*/
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
        unknowncte
    }

    /// <summary>
    /// Contains IMAP response text.
    /// </summary>
    /// <remarks>
    /// If <see cref="Code"/> is <see cref="eResponseTextCode.badcharset"/> <see cref="Arguments"/> may contain a list of supported character sets.
    /// If <see cref="Code"/> is <see cref="eResponseTextCode.referral"/> <see cref="Arguments"/> should contain URI(s).
    /// </remarks>
    /// <seealso cref="cResponseTextEventArgs"/>
    /// <seealso cref="cUnsuccessfulCompletionException"/>
    /// <seealso cref="cConnectByeException"/>
    /// <seealso cref="cHomeServerReferralException"/>
    /// <seealso cref="cCredentialsException"/>
    /// <seealso cref="cCommandResult"/>
    public class cResponseText
    {
        /// <summary>
        /// The response code associated with the response text in text form, may be <see langword="null"/>.
        /// </summary>
        public readonly string CodeText;

        /// <summary>
        /// The response code arguments associated with the response text in text form, may be <see langword="null"/>.
        /// </summary>
        public readonly string ArgumentsText;

        /// <summary>
        /// The response code associated with the response text in code form.
        /// </summary>
        /// <inheritdoc cref="cResponseText" select="remarks"/>
        public readonly eResponseTextCode Code;

        /// <summary>
        /// The response code arguments associated with the response text in list form, may be <see langword="null"/>.
        /// </summary>
        /// <inheritdoc cref="cResponseText" select="remarks"/>
        public readonly cStrings Arguments; // for badcharset, referrals

        /// <summary>
        /// The response text.
        /// </summary>
        public readonly string Text;

        internal cResponseText(string pText)
        {
            CodeText = null;
            ArgumentsText = null;
            Code = eResponseTextCode.none;
            Arguments = null;
            Text = pText;
        }

        internal cResponseText(IList<byte> pCodeText, eResponseTextCode pCode, string pText)
        {
            CodeText = cTools.ASCIIBytesToString(pCodeText);
            ArgumentsText = null;
            Code = pCode;
            Arguments = null;
            Text = pText;
        }

        internal cResponseText(IList<byte> pCodeText, IList<byte> pArgumentsText, eResponseTextCode pCode, cStrings pArguments, string pText)
        {
            CodeText = cTools.ASCIIBytesToString(pCodeText);
            ArgumentsText = cTools.UTF8BytesToString(pArgumentsText);
            Code = pCode;
            Arguments = pArguments;
            Text = pText;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cResponseText));
            lBuilder.Append(CodeText);
            lBuilder.Append(ArgumentsText);
            lBuilder.Append(Text);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Carries IMAP response text.
    /// </summary>
    public class cResponseTextEventArgs : EventArgs
    {
        /// <summary>
        /// The context in which the response text was received.
        /// </summary>
        public readonly eResponseTextContext Context;

        /// <summary>
        /// The response text.
        /// </summary>
        public readonly cResponseText Text;

        internal cResponseTextEventArgs(eResponseTextContext pContext, cResponseText pText)
        {
            Context = pContext;
            Text = pText;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cResponseTextEventArgs)}({Context},{Text})";
    }
}
