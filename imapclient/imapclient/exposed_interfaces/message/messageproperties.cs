using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMessageProperties
    {
        sent = 1 << 0,
        subject = 1 << 1,
        from = 1 << 2,
        sender = 1 << 3,
        replyto = 1 << 4,
        to = 1 << 5,
        cc = 1 << 6,
        bcc = 1 << 7,
        inreplyto = 1 << 8,
        messageid = 1 << 9,
        isanswered = 1 << 10,
        isflagged = 1 << 11,
        isdeleted = 1 << 12,
        isseen = 1 << 13,
        isdraft = 1 << 14,
        isrecent = 1 << 15,
        ismdnsent = 1 << 16,
        isforwarded = 1 << 17,
        issubmitpending = 1 << 18,
        issubmitted = 1 << 19,
        received = 1 << 20,
        size = 1 << 21,
        uid = 1 << 22,
        references = 1 << 23,
        modseq = 1 << 24,
        plaintext = 1 << 25,
        attachments = 1 << 26,

        bodystructure = plaintext | attachments,
        envelope = sent | subject | from | sender | replyto | to | cc | bcc | inreplyto | messageid,
        flags = isanswered | isflagged | isdeleted | isseen | isdraft | isrecent | ismdnsent | isforwarded | issubmitpending | issubmitted,
    }
}