using System;
using work.bacome.imapclient;

namespace testharness
{
    public class cMessageHeader
    {
        public readonly cMessage Message;

        public cMessageHeader(cMessage pMessage)
        {
            Message = pMessage;
        }

        public bool Expunged => Message.Handle.Expunged;
        public bool? Deleted => Message.Flags?.Deleted;
        public bool? Seen => Message.Flags?.Seen;

        public DateTime? Received => Message.Received;

        public string From
        {
            get
            {
                var lFrom = Message.Envelope?.From;
                if (lFrom == null) return null;
                if (lFrom[0].DisplayName != null) return lFrom[0].DisplayName;
                if (lFrom[0] is cEmailAddress lEmailAddress) return lEmailAddress.DisplayAddress;
                return null;
            }
        }

        public string Subject => Message.Envelope?.Subject;
    }
}
