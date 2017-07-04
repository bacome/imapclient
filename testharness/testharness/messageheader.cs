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

        public bool Expunged => Message.IsExpunged;
        public bool Deleted => Message.IsDeleted;
        public bool Seen => Message.IsSeen;
        public DateTime Received => Message.Received;
        public string From => Message.From.DisplaySortString;
        public string Subject => Message.Subject;
    }
}
