using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMessageProperties
    {
        clientdefault = 1 << 0,

        isexpunged = 1 << 1,

        sent = 1 << 2,
        subject = 1 << 3,
        from = 1 << 4,
        sender = 1 << 5,
        replyto = 1 << 6,
        to = 1 << 7,
        cc = 1 << 8,
        bcc = 1 << 9,
        inreplyto = 1 << 10,
        messageid = 1 << 11,

        flags = 1 << 12,
        isanswered = 1 << 13,
        isflagged = 1 << 14,
        isdeleted = 1 << 15,
        isseen = 1 << 16,
        isdraft = 1 << 17,
        isrecent = 1 << 18,
        ismdnsent = 1 << 19,
        isforwarded = 1 << 20,
        issubmitpending = 1 << 21,
        issubmitted = 1 << 22,

        received = 1 << 23,
        size = 1 << 24,
        uid = 1 << 25,
        references = 1 << 26,
        modseq = 1 << 27,

        plaintext = 1 << 28,
        attachments = 1 << 29
    }
}