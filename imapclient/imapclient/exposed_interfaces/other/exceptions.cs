using System;
using work.bacome.trace;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Base class for all of the library's custom exceptions.
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
        /// If set this is an indication that ignoring these capabilities (see <see cref="cIMAPClient.IgnoreCapabilities"/>) may have prevented the exception.
        /// </summary>
        public readonly fCapabilities TryIgnoring;

        internal cUnsuccessfulCompletionException(cResponseText pResponseText, fCapabilities pTryIgnoring, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cUnsuccessfulCompletionException), pResponseText);
        }

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
        /// If set this is an indication that ignoring these capabilities (see <see cref="cIMAPClient.IgnoreCapabilities"/>) may have prevented the exception.
        /// </summary>
        public readonly fCapabilities TryIgnoring;

        internal cProtocolErrorException(cCommandResult pCommandResult, fCapabilities pTryIgnoring, cTrace.cContext pContext)
        {
            CommandResult = pCommandResult;
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cProtocolErrorException), pCommandResult);
        }

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
        /// If set this is an indication that ignoring these capabilities (see <see cref="cIMAPClient.IgnoreCapabilities"/>) may have prevented the exception.
        /// </summary>
        public readonly fCapabilities TryIgnoring;

        internal cUnexpectedServerActionException(fCapabilities pTryIgnoring, string pMessage, cTrace.cContext pContext) : base(pMessage)
        {
            TryIgnoring = pTryIgnoring;
            pContext.TraceError("{0}: {1}", nameof(cUnexpectedServerActionException), pMessage);
        }

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
    public class cCredentialsException : cIMAPException
    {
        /// <summary>
        /// Has a value if there was an explicit rejection of the credetials by the server.
        /// </summary>
        public readonly cResponseText ResponseText;

        public cCredentialsException(cTrace.cContext pContext)
        {
            ResponseText = null;
            pContext.TraceError(nameof(cCredentialsException));
        }

        public cCredentialsException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cCredentialsException), pResponseText);
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCredentialsException));
            if (ResponseText != null) lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// thrown to indicate that the inability to connect is related to the lack of usable authentication mechanisms offered by the server
    /// </summary>
    public class cAuthenticationMechanismsException : cIMAPException
    {
        /// <summary>
        /// This is set to true if the problem might be fixed by using TLS
        /// </summary>
        public readonly bool TLSIssue;

        public cAuthenticationMechanismsException(bool pTLSIssue, cTrace.cContext pContext)
        {
            TLSIssue = pTLSIssue;
            pContext.TraceError("{0}: {1}", nameof(cAuthenticationMechanismsException), pTLSIssue);
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cAuthenticationMechanismsException));
            lBuilder.Append(TLSIssue);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// thrown to indicate that a server initiated 'BYE' occurred
    /// </summary>
    public class cUnilateralByeException : cIMAPException
    {
        /// <summary>
        /// The response text associated with the 'BYE'
        /// </summary>
        public readonly cResponseText ResponseText;

        public cUnilateralByeException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cUnilateralByeException), pResponseText);
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUnilateralByeException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// thrown when SASL security layer encoding or decoding fails
    /// </summary>
    public class cSASLSecurityException : cIMAPException
    {
        public cSASLSecurityException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cSASLSecurityException), pMessage);
        public cSASLSecurityException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cSASLSecurityException), pMessage, pInner);
    }

    /// <summary>
    /// thrown when there are two pipelined commands that conflict in some way
    /// </summary>
    public class cPipelineConflictException : cIMAPException
    {
        public cPipelineConflictException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineConflictException));
    }

    /// <summary>
    /// thrown when the internal command pipeline has stopped processing commands
    /// </summary>
    public class cPipelineStoppedException : cIMAPException
    {
        public cPipelineStoppedException() { }
        public cPipelineStoppedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineStoppedException));
        public cPipelineStoppedException(Exception pInner, cTrace.cContext pContext) : base(string.Empty, pInner) => pContext.TraceError("{0}\n{1}", nameof(cPipelineStoppedException), pInner);
    }

    /// <summary>
    /// thrown when the internal network stream has been closed
    /// </summary>
    public class cStreamClosedException : cIMAPException
    {
        public cStreamClosedException() { }
        public cStreamClosedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cStreamClosedException));
        public cStreamClosedException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cStreamClosedException), pMessage);
        public cStreamClosedException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cStreamClosedException), pMessage, pInner);
    }

    /// <summary>
    /// thrown when the UIDValidity changed while doing something that depended on it not changing
    /// </summary>
    public class cUIDValidityChangedException : cIMAPException
    {
        public cUIDValidityChangedException() { }
        public cUIDValidityChangedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cUIDValidityChangedException));
    }

    /// <summary>
    /// thrown when the CTE can't be handled
    /// </summary>
    public class cContentTransferDecodingException : cIMAPException
    {
        public cContentTransferDecodingException(cTrace.cContext pContext) => pContext.TraceError(nameof(cContentTransferDecodingException));
        public cContentTransferDecodingException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cContentTransferDecodingException), pMessage);
    }

    /*
    // thrown when a required capability for the call isn't available on the server
    public class cUnsupportedByServerException : cIMAPException
    {
        public readonly fKnownCapabilities Required;

        public cUnsupportedByServerException(fKnownCapabilities pRequired, cTrace.cContext pContext)
        {
            Required = pRequired;
            pContext.TraceError("{0}: {1}", nameof(cUnsupportedByServerException), pRequired);
        }
    }

    // thrown when a required capability for the call isn't available for the mailbox
    public class cUnsupportedByMailboxException : cIMAPException
    {
        public readonly fKnownCapabilities Required;

        public cUnsupportedByMailboxException(fKnownCapabilities pRequired, cTrace.cContext pContext)
        {
            Required = pRequired;
            pContext.TraceError("{0}: {1}", nameof(cUnsupportedByMailboxException), pRequired);
        }
    } */

    /// <summary>
    /// thrown when a handle can't resolved when building the filter
    /// </summary>
    public class cFilterMSNException : cIMAPException
    {
        /// <summary>
        /// The handle that couldn't be resolved to an MSN
        /// </summary>
        public readonly iMessageHandle Handle;

        public cFilterMSNException(iMessageHandle pHandle) { Handle = pHandle; }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cFilterMSNException));
            lBuilder.Append(Handle);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    public class cTestsException : Exception
    {
        public cTestsException() { }
        public cTestsException(string pMessage) : base(pMessage) { }
        public cTestsException(string pMessage, Exception pInner) : base(pMessage, pInner) { }
        public cTestsException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError(pMessage);
        public cTestsException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}\n{1}", pMessage, pInner);
    }
}