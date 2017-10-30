using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public enum eGreetingType { ok, preauth, bye }

            public struct sGreeting
            {
                public readonly eGreetingType Type;
                public readonly cResponseText ResponseText;
                public readonly cStrings Capabilities;
                public readonly cStrings AuthenticationMechanisms;

                public sGreeting(eGreetingType pType, cResponseText pResponseText, cStrings pCapabilities, cStrings pAuthenticationMechanisms)
                {
                    Type = pType;
                    ResponseText = pResponseText;
                    Capabilities = pCapabilities;
                    AuthenticationMechanisms = pAuthenticationMechanisms;
                }
            }
        }
    }
}