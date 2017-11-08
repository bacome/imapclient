using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// IMAP message envelope data.
    /// </summary>
    public class cEnvelope
    {
        /** <summary>The message sent date. May be null.</summary> */
        public readonly DateTime? Sent;

        /** <summary>The message subject. May be null.</summary> */
        public readonly cCulturedString Subject;

        /// <summary>
        /// <para>The base subject as defined in RFC 5256.</para>
        /// <para>(i.e. with the RE: FWD: etc stripped off)</para>
        /// <para>May be null.</para>
        /// </summary>
        public readonly string BaseSubject;

        /** <summary>The message 'from' address(s). May be null.</summary> */
        public readonly cAddresses From;

        /** <summary>The message sender address(s). May be null.</summary> */
        public readonly cAddresses Sender;

        /** <summary>The message repy-to address(s). May be null.</summary> */
        public readonly cAddresses ReplyTo;

        /** <summary>The message 'to' address(s). May be null.</summary> */
        public readonly cAddresses To;

        /** <summary>The message CC address(s). May be null.</summary> */
        public readonly cAddresses CC;

        /** <summary>The message BCC address(s). May be null.</summary> */
        public readonly cAddresses BCC;

        /** <summary>The normalised (delimiters, quoting, comments and white space removed) in-reply-to message-ids. May be null.</summary> */
        public readonly cHeaderFieldMsgIds InReplyTo;

        /** <summary>The normalised (delimiters, quoting, comments and white space removed) message-id of the message. May be null.</summary> */
        public readonly cHeaderFieldMsgId MessageId;

        public cEnvelope(DateTime? pSent, cCulturedString pSubject, string pBaseSubject, cAddresses pFrom, cAddresses pSender, cAddresses pReplyTo, cAddresses pTo, cAddresses pCC, cAddresses pBCC, cHeaderFieldMsgIds pInReplyTo, cHeaderFieldMsgId pMessageId)
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