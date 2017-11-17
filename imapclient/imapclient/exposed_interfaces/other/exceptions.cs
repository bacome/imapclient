using System;
using work.bacome.trace;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The abstract base class for all of the library's custom exceptions.
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
        /// Indicates that ignoring these capabilities may have prevented the problem.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fCapabilities TryIgnoring;

        internal cUnsuccessfulCompletionException(cResponseText pResponseText, fCapabilities pTryIgnoring, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cUnsuccessfulCompletionException), pResponseText);
        }

        /**<summary>Returns a string that represents the exception.</summary>*/
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
    /// Thrown on a 'NO' or 'BAD' command response. (Thrown on a 'NO' only when the 'NO' is an unexpected possibility.)
    /// </summary>
    public class cProtocolErrorException : cIMAPException
    {
        /// <summary>
        /// The command result associated with the response.
        /// </summary>
        public readonly cCommandResult CommandResult;

        /// <summary>
        /// Indicates that ignoring these capabilities may have prevented the exception.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fCapabilities TryIgnoring;

        internal cProtocolErrorException(cCommandResult pCommandResult, fCapabilities pTryIgnoring, cTrace.cContext pContext)
        {
            CommandResult = pCommandResult;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cProtocolErrorException), pCommandResult);
        }

        /**<summary>Returns a string that represents the exception.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cProtocolErrorException));
            lBuilder.Append(CommandResult);
            lBuilder.Append(TryIgnoring);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when something happens that shouldn't (according to my reading of the RFCs).
    /// </summary>
    public class cUnexpectedServerActionException : cIMAPException
    {
        /// <summary>
        /// Indicates that ignoring these capabilities may have prevented the exception.
        /// </summary>
        /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
        public readonly fCapabilities TryIgnoring;

        internal cUnexpectedServerActionException(fCapabilities pTryIgnoring, string pMessage, cTrace.cContext pContext) : base(pMessage)
        {
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cUnexpectedServerActionException), pMessage);
        }

        /**<summary>Returns a string that represents the exception.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUnexpectedServerActionException));
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
    /// Thrown when the server said bye at connect.
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

        /**<summary>Returns a string that represents the exception.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cConnectByeException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when the server rejects connection but suggests that we try a different server.
    /// </summary>
    /// <seealso cref="cIMAPClient.Connect"/>
    public class cHomeServerReferralException : cIMAPException
    {
        /// <summary>
        /// The response text associated with the rejection.
        /// </summary>
        public readonly cResponseText ResponseText;

        internal cHomeServerReferralException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cHomeServerReferralException), pResponseText);
        }

        /**<summary>Returns a string that represents the exception.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHomeServerReferralException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when the server didn't accept the credentials provided.
    /// </summary>
    /// <seealso cref="cIMAPClient.Connect"/>
    public class cCredentialsException : cIMAPException
    {
        /// <summary>
        /// Has a value if there was an explicit rejection of the credetials by the server.
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

        /**<summary>Returns a string that represents the exception.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCredentialsException));
            if (ResponseText != null) lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown to indicate that the inability to connect is related to the lack of usable authentication mechanisms offered by the server.
    /// </summary>
    /// <seealso cref="cIMAPClient.Connect"/>
    public class cAuthenticationMechanismsException : cIMAPException
    {
        /// <summary>
        /// Indicates if the problem might be fixed by using TLS.
        /// </summary>
        public readonly bool TLSIssue;

        internal cAuthenticationMechanismsException(bool pTLSIssue, cTrace.cContext pContext)
        {
            TLSIssue = pTLSIssue;
            pContext.TraceError("{0}: {1}", nameof(cAuthenticationMechanismsException), pTLSIssue);
        }

        /**<summary>Returns a string that represents the exception.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cAuthenticationMechanismsException));
            lBuilder.Append(TLSIssue);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown to indicate that a server initiated unilateral 'BYE' occurred.
    /// </summary>
    public class cUnilateralByeException : cIMAPException
    {
        /// <summary>
        /// The response text associated with the 'BYE'.
        /// </summary>
        public readonly cResponseText ResponseText;

        internal cUnilateralByeException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cUnilateralByeException), pResponseText);
        }

        /**<summary>Returns a string that represents the exception.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUnilateralByeException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when the installed SASL security layer encoding or decoding fails.
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
    /// Thrown when the UIDValidity changed while doing something that depended on it not changing.
    /// </summary>
    public class cUIDValidityChangedException : cIMAPException
    {
        internal cUIDValidityChangedException() { }
        internal cUIDValidityChangedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cUIDValidityChangedException));
    }

    /// <summary>
    /// Thrown when the content-transfer-encoding can't be handled.
    /// </summary>
    public class cContentTransferDecodingException : cIMAPException
    {
        internal cContentTransferDecodingException(cTrace.cContext pContext) => pContext.TraceError(nameof(cContentTransferDecodingException));
        internal cContentTransferDecodingException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cContentTransferDecodingException), pMessage);
    }

    /// <summary>
    /// Thrown when a message sequence number couldn't be determined when building a message filter. Probably due to the message being expunged.
    /// </summary>
    /// <seealso cref="cFilterMSN"/>
    public class cFilterMSNException : cIMAPException
    {
        /// <summary>
        /// The message that couldn't be resolved to a message sequence number.
        /// </summary>
        public readonly iMessageHandle Handle;

        internal cFilterMSNException(iMessageHandle pHandle) { Handle = pHandle; }

        /**<summary>Returns a string that represents the exception.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cFilterMSNException));
            lBuilder.Append(Handle);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Thrown when an internal test fails.
    /// </summary>
    public class cTestsException : Exception
    {
        internal cTestsException() { }
        internal cTestsException(string pMessage) : base(pMessage) { }
        internal cTestsException(string pMessage, Exception pInner) : base(pMessage, pInner) { }
        internal cTestsException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError(pMessage);
        internal cTestsException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}\n{1}", pMessage, pInner);
    }
}