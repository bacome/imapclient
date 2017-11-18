using System;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains IMAP message envelope data.
    /// </summary>
    /// <seealso cref="cMessage.Envelope"/>
    /// <seealso cref="cMessageBodyPart.Envelope"/>
    /// <seealso cref="iMessageHandle.Envelope"/>
    public class cEnvelope
    {
        /** <summary>The message sent date. May be <see langword="null"/>.</summary> */
        public readonly DateTime? Sent;

        /** <summary>The message subject. May be <see langword="null"/>.</summary> */
        public readonly cCulturedString Subject;

        /// <summary>
        /// The message base subject. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// The base subject is defined RFC 5256 and is the subject with the RE: FW: etc artifacts removed.
        /// </remarks>
        public readonly string BaseSubject;

        /** <summary>The message 'from' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses From;

        /** <summary>The message 'sender' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses Sender;

        /** <summary>The message 'reply-to' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses ReplyTo;

        /** <summary>The message 'to' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses To;

        /** <summary>The message CC address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses CC;

        /** <summary>The message BCC address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses BCC;

        /** <summary>The normalised (delimiters, quoting, comments and white space removed) 'in-reply-to' message-ids. May be <see langword="null"/>.</summary> */
        public readonly cHeaderFieldMsgIds InReplyTo;

        /** <summary>The normalised (delimiters, quoting, comments and white space removed) 'message-id' of the message. May be <see langword="null"/>.</summary> */
        public readonly cHeaderFieldMsgId MessageId;

        internal cEnvelope(DateTime? pSent, cCulturedString pSubject, string pBaseSubject, cAddresses pFrom, cAddresses pSender, cAddresses pReplyTo, cAddresses pTo, cAddresses pCC, cAddresses pBCC, cHeaderFieldMsgIds pInReplyTo, cHeaderFieldMsgId pMessageId)
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

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cEnvelope)}({Sent},{Subject},{BaseSubject},{From},{Sender},{ReplyTo},{To},{CC},{BCC},{InReplyTo},{MessageId})";
    }
}