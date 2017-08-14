using System;
using work.bacome.trace;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cIMAPException : Exception
    {
        public cIMAPException() { }
        public cIMAPException(string pMessage) : base(pMessage) { }
        public cIMAPException(string pMessage, Exception pInnerException) : base(pMessage, pInnerException) { }
    }

    // thrown on a 'NO'
    public class cUnsuccessfulCompletionException : cIMAPException
    {
        public readonly cResponseText ResponseText;
        public readonly fKnownCapabilities TryIgnoring;

        public cUnsuccessfulCompletionException(cResponseText pResponseText, fKnownCapabilities pTryIgnoring, cTrace.cContext pContext)
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

    // thrown on a 'BAD' 
    public class cProtocolErrorException : cIMAPException
    {
        public readonly cCommandResult CommandResult;
        public readonly fKnownCapabilities TryIgnoring;

        public cProtocolErrorException(cCommandResult pCommandResult, fKnownCapabilities pTryIgnoring, cTrace.cContext pContext)
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

    // thrown when something happens that shouldn't (according to my reading of the rfcs)
    public class cUnexpectedServerActionException : cIMAPException
    {
        public readonly fKnownCapabilities TryIgnoring;

        public cUnexpectedServerActionException(fKnownCapabilities pTryIgnoring, string pMessage, cTrace.cContext pContext) : base(pMessage)
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

    // thrown on detected internal errors
    public class cInternalErrorException : cIMAPException
    {
        public cInternalErrorException() { }
        public cInternalErrorException(cTrace.cContext pContext) => pContext.TraceError(nameof(cInternalErrorException));
        public cInternalErrorException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cInternalErrorException), pMessage);
        public cInternalErrorException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cInternalErrorException), pMessage, pInner);
    }

    // server said bye at connect
    public class cConnectByeException : cIMAPException
    {
        public readonly cResponseText ResponseText;

        public cConnectByeException(cResponseText pResponseText, cTrace.cContext pContext)
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

    // server declined connection suggesting that we try a different server
    public class cHomeServerReferralException : cIMAPException
    {
        public readonly cResponseText ResponseText;

        public cHomeServerReferralException(cResponseText pResponseText, cTrace.cContext pContext)
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

    // server didn't accept the credentials we provided: if there was an explicit rejection then the response text will be filled in
    public class cCredentialsException : cIMAPException
    {
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

    // thrown to indicate that the inability to connect is related to the lack of usable authentication mechanisms offered by the server
    public class cAuthenticationMechanismsException : cIMAPException
    {
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

    // thrown to indicate that a server initiated 'BYE' occurred
    public class cUnilateralByeException : cIMAPException
    {
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

    // thrown when SASL encoding or decoding fails
    public class cSASLSecurityException : cIMAPException
    {
        public cSASLSecurityException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cSASLSecurityException), pMessage);
        public cSASLSecurityException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cSASLSecurityException), pMessage, pInner);
    }

    // thrown when there are two pipelined commands that conflict in some way
    public class cPipelineConflictException : cIMAPException
    {
        public cPipelineConflictException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineConflictException));
    }

    // thrown when the command pipeline is stopped
    public class cPipelineStoppedException : cIMAPException
    {
        public cPipelineStoppedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineStoppedException));
        public cPipelineStoppedException(Exception pInner, cTrace.cContext pContext) : base(string.Empty, pInner) => pContext.TraceError("{0}\n{1}", nameof(cPipelineStoppedException), pInner);
    }

    // thrown when the stream is closed
    public class cStreamClosedException : cIMAPException
    {
        public cStreamClosedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cStreamClosedException));
        public cStreamClosedException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cStreamClosedException), pMessage);
        public cStreamClosedException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cStreamClosedException), pMessage, pInner);
    }

    // thrown when the UIDValidity changed while doing something that depended on it not changing
    public class cUIDValidityChangedException : cIMAPException
    {
        public cUIDValidityChangedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cUIDValidityChangedException));
    }

    // thrown when the CTE can't be handled
    public class cContentTransferDecodingException : cIMAPException
    {
        public cContentTransferDecodingException(cTrace.cContext pContext) => pContext.TraceError(nameof(cContentTransferDecodingException));
        public cContentTransferDecodingException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cContentTransferDecodingException), pMessage);
    }

    // thrown when a fetch of an attribute didn't return it
    public class cFetchFailedException : cIMAPException
    {
        public cFetchFailedException() { }
    }

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