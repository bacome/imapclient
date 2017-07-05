﻿using System;
using work.bacome.trace;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    // thrown on a 'NO'
    public class cUnsuccessfulCompletionException : Exception
    {
        public readonly cResponseText ResponseText;
        public readonly fCapabilities TryIgnoring;

        public cUnsuccessfulCompletionException(cResponseText pResponseText, fCapabilities pTryIgnoring, cTrace.cContext pContext)
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
    public class cProtocolErrorException : Exception
    {
        public readonly cCommandResult CommandResult;
        public readonly fCapabilities TryIgnoring;

        public cProtocolErrorException(cCommandResult pCommandResult, fCapabilities pTryIgnoring, cTrace.cContext pContext)
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
    public class cUnexpectedServerActionException : Exception
    {
        public readonly fCapabilities TryIgnoring;

        public cUnexpectedServerActionException(fCapabilities pTryIgnoring, string pMessage, cTrace.cContext pContext) : base(pMessage)
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
    public class cInternalErrorException : Exception
    {
        public cInternalErrorException() { }
        public cInternalErrorException(cTrace.cContext pContext) => pContext.TraceError(nameof(cInternalErrorException));
        public cInternalErrorException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cInternalErrorException), pMessage);
        public cInternalErrorException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cInternalErrorException), pMessage, pInner);
    }

    // thrown to indicate that the inablility to connect came with a referral
    public class cHomeServerReferralException : Exception
    {
        public readonly cURL URL;
        public readonly cResponseText ResponseText;

        public cHomeServerReferralException(cURL pURL, cResponseText pResponseText, cTrace.cContext pContext)
        {
            URL = pURL;
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}: {2}", nameof(cHomeServerReferralException), pURL, pResponseText);
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cHomeServerReferralException));
            lBuilder.Append(URL);
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    // thrown to indicate that a server initiated 'BYE' occurred
    public class cByeException : Exception
    {
        public readonly cResponseText ResponseText;

        public cByeException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cByeException), pResponseText);
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cByeException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    // thrown to indicate that the inability to connect is related to the credentials
    public class cCredentialsException : Exception
    {
        public readonly cResponseText ResponseText; // filled in only if we have confirmation from the server that there is something wrong with the credentials
        // just because it is null doesn't mean that they are valid though

        public cCredentialsException(cResponseText pResponseText, cTrace.cContext pContext)
        {
            ResponseText = pResponseText;
            pContext.TraceError("{0}: {1}", nameof(cCredentialsException), pResponseText);
        }

        public cCredentialsException(cTrace.cContext pContext)
        {
            ResponseText = null;
            pContext.TraceError(nameof(cCredentialsException));
        }

        public override string ToString()
        {
            if (ResponseText == null) return base.ToString();

            var lBuilder = new cListBuilder(nameof(cCredentialsException));
            lBuilder.Append(ResponseText);
            lBuilder.Append(base.ToString());
            return lBuilder.ToString();
        }
    }

    // thrown to indicate that the inability to connect is related to the lack of authentication mechanisms offered by the server
    public class cAuthenticationException : Exception
    {
        public cAuthenticationException(cTrace.cContext pContext) => pContext.TraceError(nameof(cAuthenticationException));
    }

    // thrown when SASL encoding or decoding fails
    public class cSASLSecurityException : Exception
    {
        public cSASLSecurityException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cSASLSecurityException), pMessage);
        public cSASLSecurityException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cSASLSecurityException), pMessage, pInner);
    }

    // thrown when there are two pipelined commands that conflict in some way
    public class cPipelineConflictException : Exception
    {
        public cPipelineConflictException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineConflictException));
    }

    // thrown when the command pipeline is stopped
    public class cPipelineStoppedException : Exception
    {
        public cPipelineStoppedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cPipelineStoppedException));
    }

    // thrown when the stream reader is stopped
    public class cStreamReaderStoppedException : Exception
    {
        public cStreamReaderStoppedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cStreamReaderStoppedException));
        public cStreamReaderStoppedException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cStreamReaderStoppedException), pMessage);
        public cStreamReaderStoppedException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}: {1}\n{2}", nameof(cStreamReaderStoppedException), pMessage, pInner);
    }

    // thrown when the UIDValidity changes
    public class cUIDValidityChangedException : Exception
    {
        public cUIDValidityChangedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cUIDValidityChangedException));
    }

    // thrown when the required account is not connected
    public class cAccountNotConnectedException : Exception
    {
        public cAccountNotConnectedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cAccountNotConnectedException));
    }

    // thrown when the required mailbox is not selected
    public class cMailboxNotSelectedException : Exception
    {
        public cMailboxNotSelectedException(cTrace.cContext pContext) => pContext.TraceError(nameof(cMailboxNotSelectedException));
    }

    // thrown when an invalid handle is passed
    public class cInvalidMessageHandleException : Exception
    {
        public cInvalidMessageHandleException(cTrace.cContext pContext) => pContext.TraceError(nameof(cInvalidMessageHandleException));
    }

    // thrown when the CTE can't be handled
    public class cContentTransferDecodingException : Exception
    {
        public cContentTransferDecodingException(cTrace.cContext pContext) => pContext.TraceError(nameof(cContentTransferDecodingException));
        public cContentTransferDecodingException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError("{0}: {1}", nameof(cContentTransferDecodingException), pMessage);
    }

    // thrown when a fetch of an attribute didn't return it
    public class cFetchAttributeException : Exception
    {
        public cFetchAttributeException() { }
    }

    // thrown when a required capability for the call isn't available on the server
    public class cUnsupportedByServerException : Exception
    {
        public readonly fCapabilities Required;

        public cUnsupportedByServerException(fCapabilities pRequired)
        {
            Required = pRequired;
        }

        public cUnsupportedByServerException(fCapabilities pRequired, cTrace.cContext pContext)
        {
            Required = pRequired;
            pContext.TraceError("{0}: {1}", nameof(cUnsupportedByServerException), pRequired);
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