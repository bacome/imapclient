using System;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness
{
    public class cMessageHeader
    {
        private cMessage mMessage;

        public cMessageHeader(cMessage pMessage)
        {
            mMessage = pMessage;
        }

        public DateTime? Received => mMessage.Received;

        public string From
        {
            get
            {
                var lFrom = mMessage.Envelope?.From;
                if (lFrom == null) return null;
                return lFrom[0].DisplayName;
            }
        }

        public string Subject => mMessage.Envelope?.Subject;
    }
}
