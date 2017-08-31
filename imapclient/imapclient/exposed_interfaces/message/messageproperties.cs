using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMessageProperties
    {
        clientdefault = 1 << 0,

        isexpunged = 1 << 1,

        envelope = 1 << 2,
        sent = 1 << 3,
        subject = 1 << 4,
        basesubject = 1 << 5,
        from = 1 << 6,
        sender = 1 << 7,
        replyto = 1 << 8,
        to = 1 << 9,
        cc = 1 << 10,
        bcc = 1 << 11,
        inreplyto = 1 << 12,
        messageid = 1 << 13,

        flags = 1 << 14,
        isanswered = 1 << 15,
        isflagged = 1 << 16,
        isdeleted = 1 << 17,
        isseen = 1 << 18,
        isdraft = 1 << 19,
        isrecent = 1 << 20,
        ismdnsent = 1 << 21,
        isforwarded = 1 << 22,
        issubmitpending = 1 << 23,
        issubmitted = 1 << 24,

        received = 1 << 25,
        size = 1 << 26,
        uid = 1 << 27,
        references = 1 << 28,
        modseq = 1 << 29,

        bodystructure = 1 << 30,
        attachments = 1 << 31,
        plaintextsizeinbytes = 1 << 32
    }
}