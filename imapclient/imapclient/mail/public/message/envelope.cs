using System;
using work.bacome.imapclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Contains message envelope data.
    /// </summary>
    [Serializable]
    public class cEnvelope
    {
        /** <summary>The message sent date. May be <see langword="null"/>.</summary> */
        public readonly cTimestamp Sent;

        /** <summary>The message subject. May be <see langword="null"/>.</summary> */
        public readonly cCulturedString Subject;

        /** <summary>The message 'from' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses From;

        /** <summary>The message 'sender' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses Sender;

        /** <summary>The message 'reply-to' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses ReplyTo;

        /** <summary>The message 'to' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses To;

        /** <summary>The message 'CC' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses CC;

        /** <summary>The message 'BCC' address(s). May be <see langword="null"/>.</summary> */
        public readonly cAddresses BCC;

        /** <summary>The normalised (delimiters, quoting, comments and white space removed) 'in-reply-to' message-ids. May be <see langword="null"/>.</summary> */
        public readonly cStrings InReplyTo;

        /** <summary>The normalised (delimiters, quoting, comments and white space removed) 'message-id' of the message. May be <see langword="null"/>.</summary> */
        public readonly string MessageId;

        [NonSerialized]
        private bool mBaseSubjectCalculated = false;

        [NonSerialized]
        private string mBaseSubject = null;

        internal cEnvelope(cTimestamp pSent, cCulturedString pSubject, cAddresses pFrom, cAddresses pSender, cAddresses pReplyTo, cAddresses pTo, cAddresses pCC, cAddresses pBCC, cStrings pInReplyTo, string pMessageId)
        {
            Sent = pSent;
            Subject = pSubject;
            From = pFrom;
            Sender = pSender;
            ReplyTo = pReplyTo;
            To = pTo;
            CC = pCC;
            BCC = pBCC;
            InReplyTo = pInReplyTo;
            MessageId = pMessageId;
        }

        /// <summary>
        /// The message base subject. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// The base subject is defined RFC 5256 and is the subject with the RE: FW: etc artifacts removed.
        /// </remarks>
        public string BaseSubject
        {
            get
            {
                if (!mBaseSubjectCalculated)
                {
                    mBaseSubject = cParsing.CalculateBaseSubject(Subject);
                    mBaseSubjectCalculated = true;
                }

                return mBaseSubject;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cEnvelope)}({Sent},{Subject},{From},{Sender},{ReplyTo},{To},{CC},{BCC},{InReplyTo},{MessageId},{mBaseSubjectCalculated},{mBaseSubject})";
    }
}