using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The <see langword="abstract"/> base class for all of the library's custom IMAP exceptions.
    /// </summary>
    public abstract class cIMAPException : cMailException
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
        public readonly fIMAPCapabilities TryIgnoring;

        internal cUnsuccessfulCompletionException(cResponseText pResponseText, fIMAPCapabilities pTryIgnoring)
        {
            ResponseText = pResponseText;
            TryIgnoring = pTryIgnoring;
        }

        internal cUnsuccessfulCompletionException(cResponseText pResponseText, fIMAPCapabilities pTryIgnoring, cTrace.cContext pContext)
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
        public readonly fIMAPCapabilities TryIgnoring;

        internal cProtocolErrorException(cCommandResult pCommandResult, fIMAPCapabilities pTryIgnoring)
        {
            CommandResult = pCommandResult;
            TryIgnoring = pTryIgnoring;
        }

        internal cProtocolErrorException(cCommandResult pCommandResult, fIMAPCapabilities pTryIgnoring, cTrace.cContext pContext)
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
        public readonly fIMAPCapabilities TryIgnoring;

        internal cUnexpectedServerActionException(cCommandResult pResult, string pMessage, fIMAPCapabilities pTryIgnoring, cTrace.cContext pContext) : base(pMessage)
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

    public class cUnexpectedPreAuthenticatedConnectionException : cIMAPException
    {
        internal cUnexpectedPreAuthenticatedConnectionException(cTrace.cContext pContext) => pContext.TraceError(nameof(cUnexpectedPreAuthenticatedConnectionException));
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
    /// Thrown to indicate that <see cref="cIMAPClient.Connect"/> failure is due to the server rejecting the credentials in <see cref="cIMAPClient.AuthenticationParameters"/>.
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

    public class cStreamRanOutOfDataException : cIMAPException
    {
        internal cStreamRanOutOfDataException() { }
    }
}