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
                public readonly cIMAPResponseText ResponseText;

                public sGreeting(eGreetingType pType, cIMAPResponseText pResponseText)
                {
                    Type = pType;
                    ResponseText = pResponseText;
                }
            }
        }
    }
}