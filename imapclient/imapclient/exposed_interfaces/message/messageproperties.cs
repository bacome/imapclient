using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of <see cref="cMessage"/> properties.
    /// </summary>
    /// <remarks>
    /// The <see cref="cMessageCacheItems"/> class defines an implicit conversion from this type, so you can use values of this type in places that take a <see cref="cMessageCacheItems"/>.
    /// </remarks>
    /// <seealso cref="cMessageCacheItems"/>
    /// <seealso cref="cIMAPClient.DefaultMessageCacheItems"/>
    /// <seealso cref="cMessage.Fetch(cMessageCacheItems)"/>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
    /// <seealso cref="cMailbox.Message(cUID, cMessageCacheItems)"/>
    /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{cUID}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    /// <seealso cref="cIMAPClient.Fetch(System.Collections.Generic.IEnumerable{cMessage}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
    [Flags]
    public enum fMessageProperties
    {
        /**<see cref="cMessage.Envelope"/>*/
        envelope = 1 << 0,
        /**<see cref="cMessage.Sent"/>*/
        sent = 1 << 1,
        /**<see cref="cMessage.Subject"/>*/
        subject = 1 << 2,
        /**<see cref="cMessage.BaseSubject"/>*/
        basesubject = 1 << 3,
        /**<see cref="cMessage.From"/>*/
        from = 1 << 4,
        /**<see cref="cMessage.Sender"/>*/
        sender = 1 << 5,
        /**<see cref="cMessage.ReplyTo"/>*/
        replyto = 1 << 6,
        /**<see cref="cMessage.To"/>*/
        to = 1 << 7,
        /**<see cref="cMessage.CC"/>*/
        cc = 1 << 8,
        /**<see cref="cMessage.BCC"/>*/
        bcc = 1 << 9,
        /**<see cref="cMessage.InReplyTo"/>*/
        inreplyto = 1 << 10,
        /**<see cref="cMessage.MessageId"/>*/
        messageid = 1 << 11,

        /**<see cref="cMessage.Flags"/>*/
        flags = 1 << 12,
        /**<see cref="cMessage.Answered"/>*/
        answered = 1 << 13,
        /**<see cref="cMessage.Flagged"/>*/
        flagged = 1 << 14,
        /**<see cref="cMessage.Deleted"/>*/
        deleted = 1 << 15,
        /**<see cref="cMessage.Seen"/>*/
        seen = 1 << 16,
        /**<see cref="cMessage.Draft"/>*/
        draft = 1 << 17,
        /**<see cref="cMessage.Recent"/>*/
        recent = 1 << 18,
        /**<see cref="cMessage.Forwarded"/>*/
        forwarded = 1 << 19,
        /**<see cref="cMessage.SubmitPending"/>*/
        submitpending = 1 << 20,
        /**<see cref="cMessage.Submitted"/>*/
        submitted = 1 << 21,

        /**<see cref="cMessage.Received"/>*/
        received = 1 << 22,
        /**<see cref="cMessage.Size"/>*/
        size = 1 << 23,
        /**<see cref="cMessage.UID"/>*/
        uid = 1 << 24,
        /**<see cref="cMessage.ModSeq"/>*/
        modseq = 1 << 25,

        /**<see cref="cMessage.BodyStructure"/>*/
        bodystructure = 1 << 26,
        /**<see cref="cMessage.Attachments"/>*/
        attachments = 1 << 27,
        /**<see cref="cMessage.PlainTextSizeInBytes"/>*/
        plaintextsizeinbytes = 1 << 28,

        /**<see cref="cMessage.References"/>*/
        references = 1 << 29,
        /**<see cref="cMessage.Importance"/>*/
        importance = 1 << 30

        // adding << 32 will require conversion to a long AND use of 1L in the shift
        //    public enum fMessageProperties : long
        //         importance = 1L << 31

        // see comments elsewhere as to why this is commented out
        // mdnsent = 1 << xx,

    }
}