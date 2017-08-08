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
        basesubject = 1 << 4,
        from = 1 << 5,
        sender = 1 << 6,
        replyto = 1 << 7,
        to = 1 << 8,
        cc = 1 << 9,
        bcc = 1 << 10,
        inreplyto = 1 << 11,
        messageid = 1 << 12,

        flags = 1 << 13,
        isanswered = 1 << 14,
        isflagged = 1 << 15,
        isdeleted = 1 << 16,
        isseen = 1 << 17,
        isdraft = 1 << 18,
        isrecent = 1 << 19,
        ismdnsent = 1 << 20,
        isforwarded = 1 << 21,
        issubmitpending = 1 << 22,
        issubmitted = 1 << 23,

        received = 1 << 24,
        size = 1 << 25,
        uid = 1 << 26,
        references = 1 << 27,
        modseq = 1 << 28,

        bodystructure = 1 << 29,
        attachments = 1 << 30
    }
}