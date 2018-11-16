using System;
using System.Net.Mail;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The <see langword="abstract"/> base class for all of the library's custom exceptions.
    /// </summary>
    public abstract class cMailException : Exception
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public cMailException() { }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMessage"></param>
        public cMailException(string pMessage) : base(pMessage) { }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pInner"></param>
        public cMailException(string pMessage, Exception pInner) : base(pMessage, pInner) { }
    }

    /// <summary>
    /// Thrown when something happens that shouldn't.
    /// </summary>
    public class cInternalErrorException : cMailException
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pClass"></param>
        /// <param name="pPlace"></param>
        public cInternalErrorException(string pClass, int pPlace = 1) : base($"{pClass}.{pPlace}") { }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pClass"></param>
        /// <param name="pMethod"></param>
        /// <param name="pPlace"></param>
        public cInternalErrorException(string pClass, string pMethod, int pPlace = 1) : base($"{pClass}.{pMethod}.{pPlace}") { }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pContext"></param>
        /// <param name="pPlace"></param>
        public cInternalErrorException(cTrace.cContext pContext, int pPlace = 1) => pContext.TraceError("{0}: {1}", nameof(cInternalErrorException), pPlace);
    }

    /// <summary>
    /// Thrown when the installed SASL security layer fails to encode or decode.
    /// </summary>
    public class cSASLSecurityException : cMailException
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pContext"></param>
        public cSASLSecurityException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cSASLSecurityException), pMessage);

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pInner"></param>
        /// <param name="pContext"></param>
        public cSASLSecurityException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cSASLSecurityException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown when the internal network stream has been closed.
    /// </summary>
    public class cStreamClosedException : cMailException
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public cStreamClosedException() { }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pContext"></param>
        public cStreamClosedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cStreamClosedException));

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pContext"></param>
        public cStreamClosedException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cStreamClosedException), pMessage);

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pInner"></param>
        /// <param name="pContext"></param>
        public cStreamClosedException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cStreamClosedException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown to indicate that connect failure is due to a lack of usable authentication mechanisms.
    /// </summary>
    public class cAuthenticationMechanismsException : cMailException
    {
        /// <summary>
        /// Indicates whether the problem might be fixed by using TLS.
        /// </summary>
        public readonly bool TLSIssue;

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pTLSIssue"></param>
        /// <param name="pContext"></param>
        public cAuthenticationMechanismsException(bool pTLSIssue, cTrace.cContext pContext)
        {
            TLSIssue = pTLSIssue;
            pContext.TraceError("{0}: {1}", nameof(cAuthenticationMechanismsException), pTLSIssue);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cAuthenticationMechanismsException));
            lBuilder.Append(TLSIssue);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown to indicate that the result of a command sent to the server was not received.
    /// </summary>
    public class cCommandResultUnknownException : cMailException
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="pInner"></param>
        /// <param name="pContext"></param>
        public cCommandResultUnknownException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cCommandResultUnknownException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown to indicate that a stream of data was shorter than specified.
    /// </summary>
    public class cStreamRanOutOfDataException : cMailException
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        internal cStreamRanOutOfDataException() { }
    }

    /// <summary>
    /// Thrown to indicate that a deserialised object was discovered to be in an inconsistent state.
    /// </summary>
    public class cDeserialiseException : cMailException
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pClass"></param>
        /// <param name="pProperty"></param>
        /// <param name="pMessage"></param>
        /// <param name="pPlace"></param>
        public cDeserialiseException(string pClass, string pProperty, string pMessage, int pPlace = 1) : base($"{pClass}.{pProperty}: {pMessage} ({pPlace})") { }
    }

    /// <summary>
    /// Thrown to indicate that decoding of data failed.
    /// </summary>
    public class cDecodingException : cMailException
    {
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pMessage"></param>
        public cDecodingException(string pMessage) : base(pMessage) { }
    }

    /// <summary>
    /// Thrown to indicate that this type of mail message is not supported by the library.
    /// </summary>
    public class cMailMessageFormException : cMailException
    {
        /// <summary>
        /// The mail message concerned.
        /// </summary>
        public readonly MailMessage MailMessage;

        /// <summary>
        /// The attachment concerned. May be <see langword="null"/>.
        /// </summary>
        public readonly AttachmentBase Attachment;

        internal cMailMessageFormException(MailMessage pMailMessage, string pMessage) : base(pMessage)
        {
            MailMessage = pMailMessage;
            Attachment = null;
        }

        internal cMailMessageFormException(MailMessage pMailMessage, AttachmentBase pAttachment, string pMessage) : base(pMessage)
        {
            MailMessage = pMailMessage;
            Attachment = pAttachment;
        }

        internal cMailMessageFormException(MailMessage pMailMessage, AttachmentBase pAttachment, string pMessage, Exception pInner) : base(pMessage, pInner)
        {
            MailMessage = pMailMessage;
            Attachment = pAttachment;
        }
    }
}