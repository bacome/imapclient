using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Specifies a set of <see cref="cMessage"/> properties.
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
        envelope = 1 << 0,
        sent = 1 << 1,
        subject = 1 << 2,
        basesubject = 1 << 3,
        from = 1 << 4,
        sender = 1 << 5,
        replyto = 1 << 6,
        to = 1 << 7,
        cc = 1 << 8,
        bcc = 1 << 9,
        inreplyto = 1 << 10,
        messageid = 1 << 11,

        flags = 1 << 12,
        answered = 1 << 13,
        flagged = 1 << 14,
        deleted = 1 << 15,
        seen = 1 << 16,
        draft = 1 << 17,
        recent = 1 << 18,
        forwarded = 1 << 19,
        submitpending = 1 << 20,
        submitted = 1 << 21,

        received = 1 << 22,
        size = 1 << 23,
        uid = 1 << 24,
        modseq = 1 << 25,

        bodystructure = 1 << 26,
        attachments = 1 << 27,
        plaintextsizeinbytes = 1 << 28,

        references = 1 << 29,
        importance = 1 << 30

        // adding << 32 will require conversion to a long AND use of 1L in the shift
        //    public enum fMessageProperties : long
        //         importance = 1L << 31

        // see comments elsewhere as to why this is commented out
        // mdnsent = 1 << xx,

    }
}