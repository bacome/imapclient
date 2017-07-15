using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMessageProperties
    {
        clientdefault = 1 << 0,

        sent = 1 << 1,
        subject = 1 << 2,
        from = 1 << 3,
        sender = 1 << 4,
        replyto = 1 << 5,
        to = 1 << 6,
        cc = 1 << 7,
        bcc = 1 << 8,
        inreplyto = 1 << 9,
        messageid = 1 << 10,
        isanswered = 1 << 11,
        isflagged = 1 << 12,
        isdeleted = 1 << 13,
        isseen = 1 << 14,
        isdraft = 1 << 15,
        isrecent = 1 << 16,
        ismdnsent = 1 << 17,
        isforwarded = 1 << 18,
        issubmitpending = 1 << 19,
        issubmitted = 1 << 20,
        received = 1 << 21,
        size = 1 << 22,
        uid = 1 << 23,
        references = 1 << 24,
        modseq = 1 << 25,
        plaintext = 1 << 26,
        attachments = 1 << 27,

        bodystructure = plaintext | attachments,
        envelope = sent | subject | from | sender | replyto | to | cc | bcc | inreplyto | messageid,
        flags = isanswered | isflagged | isdeleted | isseen | isdraft | isrecent | ismdnsent | isforwarded | issubmitpending | issubmitted,
    }
}