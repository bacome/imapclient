using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of <see cref="cIMAPMessage"/> properties.
    /// </summary>
    /// <remarks>
    /// The <see cref="cMessageCacheItems"/> class defines an implicit conversion from this type, so you can use values of this type in places that take a <see cref="cMessageCacheItems"/>.
    /// </remarks>
    [Flags]
    public enum fIMAPMessageProperties
    {
        /**<summary><see cref="cIMAPMessage.MessageUID"/></summary>*/
        messageuid = 1 << 0,
        /**<summary><see cref="cIMAPMessage.UID"/></summary>*/
        uid = 1 << 1,

        /**<summary><see cref="cIMAPMessage.ModSeqFlags"/></summary>*/
        modseqflags = 1 << 2,
        /**<summary><see cref="cIMAPMessage.Flags"/></summary>*/
        flags = 1 << 3,
        /**<summary><see cref="cIMAPMessage.Answered"/></summary>*/
        answered = 1 << 4,
        /**<summary><see cref="cIMAPMessage.Flagged"/></summary>*/
        flagged = 1 << 5,
        /**<summary><see cref="cIMAPMessage.Deleted"/></summary>*/
        deleted = 1 << 6,
        /**<summary><see cref="cIMAPMessage.Seen"/></summary>*/
        seen = 1 << 7,
        /**<summary><see cref="cIMAPMessage.Draft"/></summary>*/
        draft = 1 << 8,
        /**<summary><see cref="cIMAPMessage.Recent"/></summary>*/
        recent = 1 << 9,
        /**<summary><see cref="cIMAPMessage.Forwarded"/></summary>*/
        forwarded = 1 << 10,
        /**<summary><see cref="cIMAPMessage.SubmitPending"/></summary>*/
        submitpending = 1 << 11,
        /**<summary><see cref="cIMAPMessage.Submitted"/></summary>*/
        submitted = 1 << 12,

        /**<summary><see cref="cIMAPMessage.Envelope"/></summary>*/
        envelope = 1 << 13,
        /**<summary><see cref="cIMAPMessage.Sent"/></summary>*/
        sent = 1 << 14,
        /**<summary><see cref="cIMAPMessage.Subject"/></summary>*/
        subject = 1 << 15,
        /**<summary><see cref="cIMAPMessage.BaseSubject"/></summary>*/
        basesubject = 1 << 16,
        /**<summary><see cref="cIMAPMessage.From"/></summary>*/
        from = 1 << 17,
        /**<summary><see cref="cIMAPMessage.Sender"/></summary>*/
        sender = 1 << 18,
        /**<summary><see cref="cIMAPMessage.ReplyTo"/></summary>*/
        replyto = 1 << 19,
        /**<summary><see cref="cIMAPMessage.To"/></summary>*/
        to = 1 << 20,
        /**<summary><see cref="cIMAPMessage.CC"/></summary>*/
        cc = 1 << 21,
        /**<summary><see cref="cIMAPMessage.BCC"/></summary>*/
        bcc = 1 << 22,
        /**<summary><see cref="cIMAPMessage.InReplyTo"/></summary>*/
        inreplyto = 1 << 23,
        /**<summary><see cref="cIMAPMessage.MessageId"/></summary>*/
        messageid = 1 << 24,

        /**<summary><see cref="cIMAPMessage.Received"/></summary>*/
        received = 1 << 25,
        /**<summary><see cref="cIMAPMessage.Size"/></summary>*/
        size = 1 << 26,

        /**<summary><see cref="cIMAPMessage.BodyStructure"/></summary>*/
        bodystructure = 1 << 27,


        format = 1 << 28,

        /**<summary><see cref="cIMAPMessage.PlainTextSizeInBytes"/></summary>*/
        plaintextsizeinbytes = 1 << 29,

        /**<summary><see cref="cIMAPMessage.References"/></summary>*/
        references = 1 << 30,
        /**<summary><see cref="cIMAPMessage.Importance"/></summary>*/
        importance = 1 << 31

        // adding << 32 will require conversion to a long AND use of 1L in the shift
        //    public enum fMessageProperties : long
        //         importance = 1L << 31
    }
}