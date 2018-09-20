using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Thrown on an IMAP 'NO' command response.
    /// </summary>
    public class cUnsuccessfulIMAPCommandException : cMailException
    {
        /// <summary>
        /// The response text associated with the 'NO'.
        /// </summary>
        public readonly cIMAPResponseText ResponseText;

        /// <summary>
        /// Indicates that ignoring these capabilities may prevent the problem.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fIMAPCapabilities TryIgnoring;

        internal cUnsuccessfulIMAPCommandException(cIMAPResponseText pResponseText, fIMAPCapabilities pTryIgnoring)
        {
            ResponseText = pResponseText;
            TryIgnoring = pTryIgnoring;
        }

        internal cUnsuccessfulIMAPCommandException(cIMAPResponseText pResponseText, fIMAPCapabilities pTryIgnoring, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cUnsuccessfulIMAPCommandException), pResponseText);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUnsuccessfulIMAPCommandException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(TryIgnoring);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown on an IMAP 'NO' or 'BAD' command response. (Only thrown on a 'NO' when the 'NO' is an unexpected possibility.)
    /// </summary>
    public class cIMAPProtocolErrorException : cMailException
    {
        /// <summary>
        /// The command result associated with the response.
        /// </summary>
        public readonly cIMAPCommandResult CommandResult;

        /// <summary>
        /// Indicates that ignoring these capabilities may prevent the problem.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fIMAPCapabilities TryIgnoring;

        internal cIMAPProtocolErrorException(cIMAPCommandResult pCommandResult, fIMAPCapabilities pTryIgnoring)
        {
            CommandResult = pCommandResult;
            TryIgnoring = pTryIgnoring;
        }

        internal cIMAPProtocolErrorException(cIMAPCommandResult pCommandResult, fIMAPCapabilities pTryIgnoring, cTrace.cContext pContext)
        {
            CommandResult = pCommandResult;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cIMAPProtocolErrorException), pCommandResult);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIMAPProtocolErrorException));
            lBuilder.Append(CommandResult);
            lBuilder.Append(TryIgnoring);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when something happens that shouldn't (according to my reading of the RFCs).
    /// </summary>
    public class cUnexpectedIMAPServerActionException : cMailException
    {
        /// <summary>
        /// The command result associated with the unexpected action. May be <see langword="null"/>.
        /// </summary>
        public readonly cIMAPCommandResult Result;

        /// <summary>
        /// Indicates that ignoring these capabilities may prevent the problem.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fIMAPCapabilities TryIgnoring;

        internal cUnexpectedIMAPServerActionException(cIMAPCommandResult pResult, string pMessage, fIMAPCapabilities pTryIgnoring, cTrace.cContext pContext) : base(pMessage)
        {
            Result = pResult;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cUnexpectedIMAPServerActionException), pMessage);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUnexpectedIMAPServerActionException));
            lBuilder.Append(Result);
            lBuilder.Append(TryIgnoring);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when something happens that shouldn't.
    /// </summary>
    public class cUnexpectedPersistentCacheActionException : cMailException
    {
        internal cUnexpectedPersistentCacheActionException(string pClass, int pPlace = 1) : base($"{pClass}.{pPlace}") { }
        internal cUnexpectedPersistentCacheActionException(string pClass, string pMethod, int pPlace = 1) : base($"{pClass}.{pMethod}.{pPlace}") { }
        internal cUnexpectedPersistentCacheActionException(cTrace.cContext pContext, int pPlace = 1) => pContext.TraceError("{0}: {1}", nameof(cUnexpectedPersistentCacheActionException), pPlace);
    }
    /// <summary>
    /// Thrown when the section cache cannot continue.
    /// </summary>
    public class cSectionCacheException : cMailException
    {
        internal cSectionCacheException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cSectionCacheException), pMessage, pInner);
    }

    /// <summary>
    /// Thrown to indicate that <see cref="cIMAPClient.Connect"/> failure is due to the server rejecting the connection.
    /// </summary>
    public class cConnectByeException : cMailException
    {
        /// <summary>
        /// The response text associated with the 'BYE'.
        /// </summary>
        public readonly cIMAPResponseText ResponseText;

        internal cConnectByeException(cIMAPResponseText pResponseText, cTrace.cContext pContext)
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

    public class cUnexpectedPreAuthenticatedConnectionException : cMailException
    {
        internal cUnexpectedPreAuthenticatedConnectionException(cTrace.cContext pContext) => pContext.TraceError(nameof(cUnexpectedPreAuthenticatedConnectionException));
    }

    /// <summary>
    /// Thrown to indicate that a <see cref="cIMAPClient.Connect"/> failure came with a <see cref="cIMAPClient.HomeServerReferral"/>.
    /// </summary>
    public class cIMAPHomeServerReferralException : cMailException
    {
        /// <summary>
        /// The response text associated with the rejection.
        /// The home server referral will be in <see cref="cIMAPResponseText.Arguments"/>.
        /// </summary>
        public readonly cIMAPResponseText ResponseText;

        internal cIMAPHomeServerReferralException(cIMAPResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cIMAPHomeServerReferralException), pResponseText);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIMAPHomeServerReferralException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown to indicate that <see cref="cIMAPClient.Connect"/> failure is due to the server rejecting the credentials in <see cref="cIMAPClient.Authentication"/>.
    /// </summary>
    public class cIMAPCredentialsException : cMailException
    {
        /// <summary>
        /// The response text if the server explicitly rejects the credentials by using <see cref="eIMAPResponseTextCode.authenticationfailed"/>, <see cref="eIMAPResponseTextCode.authorizationfailed"/> or <see cref="eIMAPResponseTextCode.expired"/>, otherwise <see langword="null"/>.
        /// </summary>
        public readonly cIMAPResponseText ResponseText;

        internal cIMAPCredentialsException(cTrace.cContext pContext)
        {
            ResponseText = null;
            pContext.TraceError(nameof(cIMAPCredentialsException));
        }

        internal cIMAPCredentialsException(cIMAPResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cIMAPCredentialsException), pResponseText);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIMAPCredentialsException));
            if (ResponseText != null) lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown to indicate that the IMAP server unilaterally disconnected.
    /// </summary>
    public class cUnilateralByeException : cMailException
    {
        /// <summary>
        /// The response text associated with the server's 'BYE'.
        /// </summary>
        public readonly cIMAPResponseText ResponseText;

        internal cUnilateralByeException(cIMAPResponseText pResponseText, cTrace.cContext pContext)
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
    public class cPipelineConflictException : cMailException
    {
        internal cPipelineConflictException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineConflictException));
    }

    /// <summary>
    /// Thrown when the internal command pipeline has stopped processing commands.
    /// </summary>
    public class cPipelineStoppedException : cMailException
    {
        internal cPipelineStoppedException() { }
        internal cPipelineStoppedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineStoppedException));
        internal cPipelineStoppedException(Exception pInner, cTrace.cContext pContext) : base(string.Empty, pInner) => pContext.TraceError("{0}\n{1}", nameof(cPipelineStoppedException), pInner);
    }

    /// <summary>
    /// Thrown when the UIDValidity is incorrect or changed while the library was doing something that depended on it not changing.
    /// </summary>
    public class cUIDValidityException : cMailException
    {
        /// <summary>
        /// The command result associated with the exception. May be <see langword="null"/>.
        /// </summary>
        public readonly cIMAPCommandResult Result;

        internal cUIDValidityException()
        {
            Result = null;
        }

        internal cUIDValidityException(cTrace.cContext pContext)
        {
            Result = null;
            pContext.TraceError(nameof(cUIDValidityException));
        }

        internal cUIDValidityException(cIMAPCommandResult pResult, cTrace.cContext pContext)
        {
            Result = pResult;
            pContext.TraceError(nameof(cUIDValidityException));
        }
    }

    /// <summary>
    /// Thrown when the required content-transfer-decoding can't be done client-side.
    /// </summary>
    public class cContentTransferDecodingNotSupportedException : cMailException
    {
        public readonly eDecodingRequired Decoding;

        internal cContentTransferDecodingNotSupportedException(eDecodingRequired pDecoding)
        {
            Decoding = pDecoding;
        }
    }

    /// <summary>
    /// Thrown when a message's sequence number or server-side message data is required after the message has been expunged.
    /// </summary>
    public class cMessageExpungedException : cMailException
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
    /// Thrown when the requested data is not returned by the IMAP server. This is most likely because the message has been expunged.
    /// </summary>
    /// <remarks>
    /// Either the <see cref="MailboxHandle"/> and <see cref="UID"/> will be not <see langword="null"/> or the <see cref="MessageHandle"/> will be not <see langword="null"/>.
    /// </remarks>
    public class cRequestedIMAPDataNotReturnedException : cMailException
    {
        public readonly iMailboxHandle MailboxHandle;
        public readonly cUID UID;

        /// <summary>
        /// The message concerned. May be <see langword="null"/>.
        /// </summary>
        public readonly iMessageHandle MessageHandle;

        internal cRequestedIMAPDataNotReturnedException(iMailboxHandle pMailboxHandle, cUID pUID)
        {
            MailboxHandle = pMailboxHandle;
            UID = pUID;
            MessageHandle = null;
        }

        internal cRequestedIMAPDataNotReturnedException(iMessageHandle pMessageHandle)
        {
            MailboxHandle = null;
            UID = null;
            MessageHandle = pMessageHandle;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cRequestedIMAPDataNotReturnedException));
            lBuilder.Append(MessageHandle);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when a single message store operation fails.
    /// </summary>
    public class cSingleMessageStoreException : cMailException
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
    public class cMessageDataClientException : cMailException
    {
        internal cMessageDataClientException() { }
    }

    /*
    public class cAppendDataFormatException : cMailException
    {
        public readonly cAppendData AppendData;

        internal cAppendDataFormatException(cAppendData pAppendData)
        {
            AppendData = pAppendData;
        }
    } */
}