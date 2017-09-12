using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMessageProperties
    {
        isexpunged = 1 << 0,

        envelope = 1 << 1,
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
        modseq = 1 << 27,

        bodystructure = 1 << 28,
        attachments = 1 << 29,
        plaintextsizeinbytes = 1 << 30,

        importance = 1 << 31

        // NOTE: if adding one more then the type needs conversion to long and the shifted 1 to a long
        //  e.g.
        //
        //     public enum fMessageProperties : long
        //     {
        //          ...
        //          newattribute = 1L << 32
        //     }
    }

    public class cMessageProperties
    {
        public static readonly cMessageProperties None = new cMessageProperties(0, cHeaderNames.None);
        public static readonly cMessageProperties Envelope = new cMessageProperties(fMessageProperties.envelope, cHeaderNames.None);
        public static readonly cMessageProperties Flags = new cMessageProperties(fMessageProperties.flags, cHeaderNames.None);
        public static readonly cMessageProperties Received = new cMessageProperties(fMessageProperties.received, cHeaderNames.None);
        public static readonly cMessageProperties Size = new cMessageProperties(fMessageProperties.size, cHeaderNames.None);
        public static readonly cMessageProperties UID = new cMessageProperties(fMessageProperties.uid, cHeaderNames.None);
        public static readonly cMessageProperties ModSeq = new cMessageProperties(fMessageProperties.modseq, cHeaderNames.None);
        public static readonly cMessageProperties BodyStructure = new cMessageProperties(fMessageProperties.bodystructure, cHeaderNames.None);
        public static readonly cMessageProperties Importance = new cMessageProperties(fMessageProperties.importance, cHeaderNames.None);

        public readonly fMessageProperties Properties;
        public readonly cHeaderNames Names;

        public cMessageProperties(fMessageProperties pProperties, cHeaderNames pNames)
        {
            Properties = pProperties;
            Names = pNames ?? throw new ArgumentNullException(nameof(pNames));
        }

        public override string ToString() => $"{nameof(cMessageProperties)}({Properties},{Names})";
    }
}