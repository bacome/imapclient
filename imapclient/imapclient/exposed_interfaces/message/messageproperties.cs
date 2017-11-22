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
        /**<summary><see cref="cMessage.Envelope"/></summary>*/
        envelope = 1 << 0,
        /**<summary><see cref="cMessage.Sent"/></summary>*/
        sent = 1 << 1,
        /**<summary><see cref="cMessage.Subject"/></summary>*/
        subject = 1 << 2,
        /**<summary><see cref="cMessage.BaseSubject"/></summary>*/
        basesubject = 1 << 3,
        /**<summary><see cref="cMessage.From"/></summary>*/
        from = 1 << 4,
        /**<summary><see cref="cMessage.Sender"/></summary>*/
        sender = 1 << 5,
        /**<summary><see cref="cMessage.ReplyTo"/></summary>*/
        replyto = 1 << 6,
        /**<summary><see cref="cMessage.To"/></summary>*/
        to = 1 << 7,
        /**<summary><see cref="cMessage.CC"/></summary>*/
        cc = 1 << 8,
        /**<summary><see cref="cMessage.BCC"/></summary>*/
        bcc = 1 << 9,
        /**<summary><see cref="cMessage.InReplyTo"/></summary>*/
        inreplyto = 1 << 10,
        /**<summary><see cref="cMessage.MessageId"/></summary>*/
        messageid = 1 << 11,

        /**<summary><see cref="cMessage.Flags"/></summary>*/
        flags = 1 << 12,
        /**<summary><see cref="cMessage.Answered"/></summary>*/
        answered = 1 << 13,
        /**<summary><see cref="cMessage.Flagged"/></summary>*/
        flagged = 1 << 14,
        /**<summary><see cref="cMessage.Deleted"/></summary>*/
        deleted = 1 << 15,
        /**<summary><see cref="cMessage.Seen"/></summary>*/
        seen = 1 << 16,
        /**<summary><see cref="cMessage.Draft"/></summary>*/
        draft = 1 << 17,
        /**<summary><see cref="cMessage.Recent"/></summary>*/
        recent = 1 << 18,
        /**<summary><see cref="cMessage.Forwarded"/></summary>*/
        forwarded = 1 << 19,
        /**<summary><see cref="cMessage.SubmitPending"/></summary>*/
        submitpending = 1 << 20,
        /**<summary><see cref="cMessage.Submitted"/></summary>*/
        submitted = 1 << 21,

        /**<summary><see cref="cMessage.Received"/></summary>*/
        received = 1 << 22,
        /**<summary><see cref="cMessage.Size"/></summary>*/
        size = 1 << 23,
        /**<summary><see cref="cMessage.UID"/></summary>*/
        uid = 1 << 24,
        /**<summary><see cref="cMessage.ModSeq"/></summary>*/
        modseq = 1 << 25,

        /**<summary><see cref="cMessage.BodyStructure"/></summary>*/
        bodystructure = 1 << 26,
        /**<summary><see cref="cMessage.Attachments"/></summary>*/
        attachments = 1 << 27,
        /**<summary><see cref="cMessage.PlainTextSizeInBytes"/></summary>*/
        plaintextsizeinbytes = 1 << 28,

        /**<summary><see cref="cMessage.References"/></summary>*/
        references = 1 << 29,
        /**<summary><see cref="cMessage.Importance"/></summary>*/
        importance = 1 << 30

        // adding << 32 will require conversion to a long AND use of 1L in the shift
        //    public enum fMessageProperties : long
        //         importance = 1L << 31

        // see comments elsewhere as to why this is commented out
        // mdnsent = 1 << xx,

    }
}