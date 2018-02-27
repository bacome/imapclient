using System;
using System.Net.Mail;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal static class kInvalidOperationExceptionMessage
    {
        public const string AlreadyConnected = "already connected";
        public const string AlreadyEnabled = "already enabled";
        public const string AlreadyChallenged = "already challenged";
        public const string AlreadyEmitted = "already emitted";
        public const string AlreadySet = "already set";

        public const string NotUnconnected = "not unconnected";
        public const string NotConnecting = "not connecting";
        public const string NotConnected = "not connected";
        public const string NotUnauthenticated = "not unauthenticated";
        public const string NotAuthenticated = "not authenticated";
        public const string NotEnabled = "not enabled";
        public const string NotSelected = "not selected";
        public const string NotSelectedForUpdate = "not selected for update";
        public const string MailboxNotSelected = "mailbox not selected";

        public const string NotPopulatedWithData = "not populated with data";

        public const string NoMailboxHierarchy = "no mailbox hierarchy";
        public const string CondStoreNotInUse = "condstore not in use";
        public const string BodyStructureHasNotBeenFetched = "bodystructure has not been fetched";
    }

    internal static class kArgumentOutOfRangeExceptionMessage
    {
        public const string IsInvalid = "is invalid";
        public const string MailboxMustBeSelected = "mailbox must be selected";

        public const string ContainsNulls = "contains nulls";
        public const string ContainsMixedMessageCaches = "contains mixed message caches";
        public const string ContainsMixedUIDValidities = "contains mixed uidvalidities";

        //public const string CantConvert = "can't convert: ";
    }

    internal static class kMailMessageFormExceptionMessage
    {
        public const string MixedEncodings = "mixed encodings";
        public const string StreamNotSeekable = "stream not seekable";
        public const string MessageDataStreamUnknownLength = "message data stream unknown length";
        public const string ReplyToNotSupported = "reply-to not supported";
    }

    /// <summary>
    /// The <see langword="abstract"/> base class for all of the library's custom exceptions.
    /// </summary>
    public abstract class cIMAPException : Exception
    {
        internal cIMAPException() { }
        internal cIMAPException(string pMessage) : base(pMessage) { }
        internal cIMAPException(string pMessage, Exception pInnerException) : base(pMessage, pInnerException) { }
    }

    /// <summary>
    /// Thrown on a 'NO' command response.
    /// </summary>
    public class cUnsuccessfulCompletionException : cIMAPException
    {
        /// <summary>
        /// The response text associated with the 'NO'.
        /// </summary>
        public readonly cResponseText ResponseText;

        /// <summary>
        /// Indicates that ignoring these capabilities may prevent the problem.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fCapabilities TryIgnoring;

        internal cUnsuccessfulCompletionException(cResponseText pResponseText, fCapabilities pTryIgnoring)
        {
            ResponseText = pResponseText;
            TryIgnoring = pTryIgnoring;
        }

        internal cUnsuccessfulCompletionException(cResponseText pResponseText, fCapabilities pTryIgnoring, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cUnsuccessfulCompletionException), pResponseText);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUnsuccessfulCompletionException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(TryIgnoring);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown on a 'NO' or 'BAD' command response. (Only thrown on a 'NO' when the 'NO' is an unexpected possibility.)
    /// </summary>
    public class cProtocolErrorException : cIMAPException
    {
        /// <summary>
        /// The command result associated with the response.
        /// </summary>
        public readonly cCommandResult CommandResult;

        /// <summary>
        /// Indicates that ignoring these capabilities may prevent the problem.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fCapabilities TryIgnoring;

        internal cProtocolErrorException(cCommandResult pCommandResult, fCapabilities pTryIgnoring)
        {
            CommandResult = pCommandResult;
            TryIgnoring = pTryIgnoring;
        }

        internal cProtocolErrorException(cCommandResult pCommandResult, fCapabilities pTryIgnoring, cTrace.cContext pContext)
        {
            CommandResult = pCommandResult;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cProtocolErrorException), pCommandResult);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cProtocolErrorException));
            lBuilder.Append(CommandResult);
            lBuilder.Append(TryIgnoring);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    public class cCommandResultUnknownException : cIMAPException
    {
        internal cCommandResultUnknownException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cCommandResultUnknownException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown when something happens that shouldn't (according to my reading of the RFCs).
    /// </summary>
    public class cUnexpectedServerActionException : cIMAPException
    {
        /// <summary>
        /// The command result associated with the unexpected action. May be <see langword="null"/>.
        /// </summary>
        public readonly cCommandResult Result;

        /// <summary>
        /// Indicates that ignoring these capabilities may prevent the problem.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fCapabilities TryIgnoring;

        internal cUnexpectedServerActionException(cCommandResult pResult, string pMessage, fCapabilities pTryIgnoring, cTrace.cContext pContext) : base(pMessage)
        {
            Result = pResult;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cUnexpectedServerActionException), pMessage);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUnexpectedServerActionException));
            lBuilder.Append(Result);
            lBuilder.Append(TryIgnoring);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when something happens that shouldn't.
    /// </summary>
    public class cInternalErrorException : cIMAPException
    {
        internal cInternalErrorException() { }
        internal cInternalErrorException(cTrace.cContext pContext) => pContext.TraceError(nameof(cInternalErrorException));
        internal cInternalErrorException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cInternalErrorException), pMessage);
        internal cInternalErrorException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cInternalErrorException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown to indicate that <see cref="cIMAPClient.Connect"/> failure is due to the server rejecting the connection.
    /// </summary>
    /// <seealso cref="cIMAPClient.Connect"/>
    public class cConnectByeException : cIMAPException
    {
        /// <summary>
        /// The response text associated with the 'BYE'.
        /// </summary>
        public readonly cResponseText ResponseText;

        internal cConnectByeException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cConnectByeException), pResponseText);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cConnectByeException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown to indicate that a <see cref="cIMAPClient.Connect"/> failure came with a <see cref="cIMAPClient.HomeServerReferral"/>.
    /// </summary>
    /// <seealso cref="cIMAPClient.Connect"/>
    public class cHomeServerReferralException : cIMAPException
    {
        /// <summary>
        /// The response text associated with the rejection.
        /// The home server referral will be in <see cref="cResponseText.Arguments"/>.
        /// </summary>
        public readonly cResponseText ResponseText;

        internal cHomeServerReferralException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cHomeServerReferralException), pResponseText);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHomeServerReferralException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown to indicate that <see cref="cIMAPClient.Connect"/> failure is due to the server rejecting the <see cref="cIMAPClient.Credentials"/>.
    /// </summary>
    /// <seealso cref="cIMAPClient.Connect"/>
    public class cCredentialsException : cIMAPException
    {
        /// <summary>
        /// The response text if the server explicitly rejects the credentials by using <see cref="eResponseTextCode.authenticationfailed"/>, <see cref="eResponseTextCode.authorizationfailed"/> or <see cref="eResponseTextCode.expired"/>, otherwise <see langword="null"/>.
        /// </summary>
        public readonly cResponseText ResponseText;

        internal cCredentialsException(cTrace.cContext pContext)
        {
            ResponseText = null;
            pContext.TraceError(nameof(cCredentialsException));
        }

        internal cCredentialsException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cCredentialsException), pResponseText);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCredentialsException));
            if (ResponseText != null) lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown to indicate that <see cref="cIMAPClient.Connect"/> failure is due to a lack of usable authentication mechanisms.
    /// </summary>
    /// <seealso cref="cIMAPClient.Connect"/>
    public class cAuthenticationMechanismsException : cIMAPException
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

    /// <summary>
    /// Thrown to indicate that the server unilaterally disconnected.
    /// </summary>
    public class cUnilateralByeException : cIMAPException
    {
        /// <summary>
        /// The response text associated with the server's 'BYE'.
        /// </summary>
        public readonly cResponseText ResponseText;

        internal cUnilateralByeException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cUnilateralByeException), pResponseText);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUnilateralByeException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when the installed SASL security layer fails to encode or decode.
    /// </summary>
    public class cSASLSecurityException : cIMAPException
    {
        internal cSASLSecurityException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cSASLSecurityException), pMessage);
        internal cSASLSecurityException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cSASLSecurityException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown when there are two pipelined commands that conflict in some way. Indicates a bug in the library.
    /// </summary>
    public class cPipelineConflictException : cIMAPException
    {
        internal cPipelineConflictException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineConflictException));
    }

    /// <summary>
    /// Thrown when the internal command pipeline has stopped processing commands.
    /// </summary>
    public class cPipelineStoppedException : cIMAPException
    {
        internal cPipelineStoppedException() { }
        internal cPipelineStoppedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineStoppedException));
        internal cPipelineStoppedException(Exception pInner, cTrace.cContext pContext) : base(string.Empty, pInner) => pContext.TraceError("{0}\n{1}", nameof(cPipelineStoppedException), pInner);
    }

    /// <summary>
    /// Thrown when the internal network stream has been closed.
    /// </summary>
    public class cStreamClosedException : cIMAPException
    {
        internal cStreamClosedException() { }
        internal cStreamClosedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cStreamClosedException));
        internal cStreamClosedException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cStreamClosedException), pMessage);
        internal cStreamClosedException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cStreamClosedException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown when the UIDValidity is incorrect or changed while the library was doing something that depended on it not changing.
    /// </summary>
    public class cUIDValidityException : cIMAPException
    {
        /// <summary>
        /// The command result associated with the exception. May be <see langword="null"/>.
        /// </summary>
        public readonly cCommandResult Result;

        internal cUIDValidityException()
        {
            Result = null;
        }

        internal cUIDValidityException(cTrace.cContext pContext)
        {
            Result = null;
            pContext.TraceError(nameof(cUIDValidityException));
        }

        internal cUIDValidityException(cCommandResult pResult, cTrace.cContext pContext)
        {
            Result = pResult;
            pContext.TraceError(nameof(cUIDValidityException));
        }
    }

    /// <summary>
    /// Thrown when the required content-transfer-decoding can't be done client-side.
    /// </summary>
    /// <remarks>
    /// Will be thrown either due to an error in the decoder or due to the library not having a suitable decoder to use.
    /// </remarks>
    public class cContentTransferDecodingException : cIMAPException
    {
        internal cContentTransferDecodingException(cTrace.cContext pContext) => pContext.TraceError(nameof(cContentTransferDecodingException));
        internal cContentTransferDecodingException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cContentTransferDecodingException), pMessage);
    }

    /// <summary>
    /// Thrown when a message's sequence number or server-side message data is required after the message has been expunged.
    /// </summary>
    public class cMessageExpungedException : cIMAPException
    {
        /// <summary>
        /// The message concerned.
        /// </summary>
        public readonly iMessageHandle MessageHandle;

        internal cMessageExpungedException(iMessageHandle pMessageHandle) { MessageHandle = pMessageHandle; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cMessageExpungedException));
            lBuilder.Append(MessageHandle);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when the requested data is not returned by the server. This is most likely because the message has been expunged.
    /// </summary>
    /// <remarks>
    /// Either the <see cref="MailboxHandle"/> and <see cref="UID"/> will be not <see langword="null"/> or the <see cref="MessageHandle"/> will be not <see langword="null"/>.
    /// </remarks>
    public class cRequestedDataNotReturnedException : cIMAPException
    {
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;

        /// <summary>
        /// The message concerned. May be <see langword="null"/>.
        /// </summary>
        public readonly iMessageHandle MessageHandle;

        internal cRequestedDataNotReturnedException(iMailboxHandle pMailboxHandle, cUID pUID)
        {
            MailboxHandle = pMailboxHandle;
            UID = pUID;
            MessageHandle = null;
        }

        internal cRequestedDataNotReturnedException(iMessageHandle pMessageHandle)
        {
            MailboxHandle = null;
            UID = null;
            MessageHandle = pMessageHandle;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cRequestedDataNotReturnedException));
            lBuilder.Append(MessageHandle);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when a single message store operation fails.
    /// </summary>
    public class cSingleMessageStoreException : cIMAPException
    {
        /// <summary>
        /// The message concerned.
        /// </summary>
        public readonly iMessageHandle MessageHandle;

        internal cSingleMessageStoreException(iMessageHandle pMessageHandle) { MessageHandle = pMessageHandle; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cSingleMessageStoreException));
            lBuilder.Append(MessageHandle);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when supplied message data references the <see cref="cIMAPClient"/> instance that the data is being processed by.
    /// </summary>
    /// <remarks>
    /// This situation can lead to an internal library deadlock if the data is being retrieved from the server at the same time as it is being sent to the server.
    /// Either use another <see cref="cIMAPClient"/> instance with the same <see cref="cIMAPClient.ConnectedAccountId"/> as the source of the data
    /// or read and cache the data locally (e.g. in a file) before using it.
    /// </remarks>
    public class cMessageDataClientException : cIMAPException
    {
        internal cMessageDataClientException() { }
    }

    public class cMailAddressFormException : cIMAPException
    {
        /// <summary>
        /// The mail address concerned.
        /// </summary>
        public readonly MailAddress MailAddress;

        internal cMailAddressFormException(MailAddress pMailAddress)
        {
            MailAddress = pMailAddress;
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

    public class cMailMessageFormException : cIMAPException
    {
        internal cMailMessageFormException(string pMessage) : base(pMessage) { }
    }

    public class cStreamRanOutOfDataException : cIMAPException
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