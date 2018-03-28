using System;
using System.Net.Mail;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// The <see langword="abstract"/> base class for all of the library's custom exceptions.
    /// </summary>
    public abstract class cMailException : Exception
    {
        internal cMailException() { }
        internal cMailException(string pMessage) : base(pMessage) { }
        internal cMailException(string pMessage, Exception pInnerException) : base(pMessage, pInnerException) { }
    }

    /// <summary>
    /// Thrown when something happens that shouldn't.
    /// </summary>
    public class cInternalErrorException : cMailException
    {
        internal cInternalErrorException(string pClass, int pPlace = 1) : base($"{pClass}.{pPlace}") { }
        internal cInternalErrorException(string pClass, string pMethod, int pPlace = 1) : base($"{pClass}.{pMethod}.{pPlace}") { }
        internal cInternalErrorException(cTrace.cContext pContext, int pPlace = 1) => pContext.TraceError($"{nameof(cInternalErrorException)}.{pPlace}");
    }

    /// <summary>
    /// Thrown when the installed SASL security layer fails to encode or decode.
    /// </summary>
    public class cSASLSecurityException : cMailException
    {
        internal cSASLSecurityException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cSASLSecurityException), pMessage);
        internal cSASLSecurityException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cSASLSecurityException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown when the internal network stream has been closed.
    /// </summary>
    public class cStreamClosedException : cMailException
    {
        internal cStreamClosedException() { }
        internal cStreamClosedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cStreamClosedException));
        internal cStreamClosedException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cStreamClosedException), pMessage);
        internal cStreamClosedException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cStreamClosedException), pMessage, pInner);
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

        internal cAuthenticationMechanismsException(bool pTLSIssue, cTrace.cContext pContext)
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

    public class cAddressFormException : cMailException
    {
        /// <summary>
        /// The mail address concerned. May be <see langword="null"/>.
        /// </summary>
        public readonly MailAddress MailAddress;

        /// <summary>
        /// The address concerned. May be <see langword="null"/>.
        /// </summary>
        public readonly cAddress Address;

        internal cAddressFormException(MailAddress pMailAddress)
        {
            MailAddress = pMailAddress;
        }

        internal cAddressFormException(cEmailAddress pEmailAddress)
        {
            Address = pEmailAddress;
        }

        internal cAddressFormException(cGroupAddress pGroupAddress)
        {
            Address = pGroupAddress;
        }
    }



    /*
    ;?; // list

    /// <summary>
    /// Thrown when a <see cref="MailMessage"/> instance has a form that is not supported by the library.
    /// </summary>
    /// <remarks>
    /// The library only supports messages with the header and subject encoding the same.  ()Note that address encodings are ignored (the subject encoding is used). one <see cref="Encoding"/> per mail message.
    /// The library supports seekable streams.
    /// The library supports <see cref="cMessageDataStream"/> instances where;
    /// <see cref="cMessageDataStream.HasKnownLength"/> is <see langword="true"/>.
    /// <see cref="cMessageDataStream"/> must not reference the <see cref="cIMAPClient"/> instance that the message is being appended through
    /// </remarks>
    /// */

    public class cMailMessageFormException : cMailException
    {
        public readonly MailMessage MailMessage;
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

        internal cMailMessageFormException(MailMessage pMailMessage, AttachmentBase pAttachment, string pMessage, Exception pInnerException) : base(pMessage, pInnerException)
        {
            MailMessage = pMailMessage;
            Attachment = pAttachment;
        }
    }

    public class cCommandResultUnknownException : cMailException
    {
        internal cCommandResultUnknownException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cCommandResultUnknownException), pMessage, pInner);
    }

    public class cStreamRanOutOfDataException : cMailException
    {
        internal cStreamRanOutOfDataException() { }
    }

    internal class cTestsException : Exception
    {
        internal cTestsException() { }
        internal cTestsException(string pMessage) : base(pMessage) { }
        internal cTestsException(string pMessage, Exception pInner) : base(pMessage, pInner) { }
        internal cTestsException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError(pMessage);
        internal cTestsException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}\n{1}", pMessage, pInner);
    }
}