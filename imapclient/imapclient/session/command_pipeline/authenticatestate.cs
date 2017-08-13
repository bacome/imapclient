using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            { 
                private class cAuthenticateState
                {
                    public bool CancelSent = false;
                    public cAuthenticateState() { }
                }
            }
        }
    }
}