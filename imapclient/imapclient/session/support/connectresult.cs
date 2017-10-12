using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public enum eConnectResultCode { ok, preauth, bye }

            public struct sConnectResult
            {
                public readonly eConnectResultCode Code;
                public readonly cResponseText ResponseText;
                public readonly cStrings Capabilities;
                public readonly cStrings AuthenticationMechanisms;

                public sConnectResult(eConnectResultCode pCode, cResponseText pResponseText, cStrings pCapabilities, cStrings pAuthenticationMechanisms)
                {
                    Code = pCode;
                    ResponseText = pResponseText;
                    Capabilities = pCapabilities;
                    AuthenticationMechanisms = pAuthenticationMechanisms;
                }
            }
        }
    }
}