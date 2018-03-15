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

                public sGreeting(eGreetingType pType, cResponseText pResponseText)
                {
                    Type = pType;
                    ResponseText = pResponseText;
                }
            }
        }
    }
}