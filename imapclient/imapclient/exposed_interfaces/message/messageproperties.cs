using System;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fMessageProperties : long
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

        references = 1 << 31,
        importance = 1L << 32
    }

    public class cMessageProperties
    {
        public static readonly cMessageProperties None = new cMessageProperties(0, cHeaderFieldNames.None);
        public static readonly cMessageProperties Envelope = fMessageProperties.envelope;
        public static readonly cMessageProperties Flags = fMessageProperties.flags;
        public static readonly cMessageProperties Received = fMessageProperties.received;
        public static readonly cMessageProperties Size = fMessageProperties.size;
        public static readonly cMessageProperties UID = fMessageProperties.uid;
        public static readonly cMessageProperties ModSeq = fMessageProperties.modseq;
        public static readonly cMessageProperties BodyStructure = fMessageProperties.bodystructure;
        public static readonly cMessageProperties References = fMessageProperties.references;
        public static readonly cMessageProperties Importance = fMessageProperties.importance;

        public readonly fMessageProperties Properties;
        public readonly cHeaderFieldNames Names;

        public cMessageProperties(fMessageProperties pProperties, cHeaderFieldNames pNames)
        {
            Properties = pProperties;
            Names = pNames ?? throw new ArgumentNullException(nameof(pNames));
        }

        public bool IsNone => Properties == 0 && Names.Count == 0;

        public static cMessageProperties operator |(cMessageProperties pProperties, fMessageProperties pPropertiesToAdd)
        {
            if (pProperties == null) return null;
            fMessageProperties lProperties = pProperties.Properties | pPropertiesToAdd;
            if (lProperties == pProperties.Properties) return pProperties;
            return new cMessageProperties(lProperties, pProperties.Names);
        }

        public override string ToString() => $"{nameof(cMessageProperties)}({Properties},{Names})";

        public static implicit operator cMessageProperties(fMessageProperties pProperties) => new cMessageProperties(pProperties, cHeaderFieldNames.None);
    }
}