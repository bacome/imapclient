using System;

namespace work.bacome.imapclient
{
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
        mdnsent = 1 << 19,
        forwarded = 1 << 20,
        submitpending = 1 << 21,
        submitted = 1 << 22,

        received = 1 << 23,
        size = 1 << 24,
        uid = 1 << 25,
        modseq = 1 << 26,

        bodystructure = 1 << 27,
        attachments = 1 << 28,
        plaintextsizeinbytes = 1 << 29,

        references = 1 << 30,
        importance = 1 << 31

        // adding one will require conversion to a long AND use of 1L in the shift
        //    public enum fMessageProperties : long
        //         importance = 1L << 31
    }
}