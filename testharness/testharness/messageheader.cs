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

        public bool Deleted => ((Message.Flags ?? 0) & fMessageFlags.deleted) != 0;
        public bool Seen => ((Message.Flags ?? 0) & fMessageFlags.seen) != 0;

        public DateTime? Received => Message.Received;

        public string From
        {
            get
            {
                var lFrom = Message.From;
                if (lFrom == null) return null;
                if (lFrom[0].DisplayName != null) return lFrom[0].DisplayName;
                if (lFrom[0] is cEmailAddress lEmailAddress) return lEmailAddress.DisplayAddress;
                return null;
            }
        }

        public string Subject => Message.Subject;
    }
}
