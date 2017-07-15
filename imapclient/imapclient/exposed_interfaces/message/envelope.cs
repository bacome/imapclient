using System;

namespace work.bacome.imapclient
{
    public class cEnvelope
    {
        // all may be null
        public readonly DateTime? Sent;
        public readonly cCulturedString Subject;
        public readonly string BaseSubject; // as defined by rfc5256
        public readonly cAddresses From; 
        public readonly cAddresses Sender; 
        public readonly cAddresses ReplyTo;
        public readonly cAddresses To;
        public readonly cAddresses CC;
        public readonly cAddresses BCC; 
        public readonly string InReplyTo; // the first (if any) message id in the in-reply-to
        public readonly string MessageId;

        public cEnvelope(DateTime? pSent, cCulturedString pSubject, string pBaseSubject, cAddresses pFrom, cAddresses pSender, cAddresses pReplyTo, cAddresses pTo, cAddresses pCC, cAddresses pBCC, string pInReplyTo, string pMessageId)
        {
            Sent = pSent;
            Subject = pSubject;
            BaseSubject = pBaseSubject;
            From = pFrom;
            Sender = pSender;
            ReplyTo = pReplyTo;
            To = pTo;
            CC = pCC;
            BCC = pBCC;
            InReplyTo = pInReplyTo;
            MessageId = pMessageId;
        }

        public override string ToString() => $"{nameof(cEnvelope)}({Sent},{Subject},{BaseSubject},{From},{Sender},{ReplyTo},{To},{CC},{BCC},{InReplyTo},{MessageId})";
    }
}