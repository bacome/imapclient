using System;

namespace work.bacome.imapclient
{
    /*
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
        modseq = 1 << 26,

        bodystructure = 1 << 27,
        attachments = 1 << 28,
        plaintextsizeinbytes = 1 << 29,

        references = 1 << 30,
        importance = 1 << 31

        // addig one will require conversion to a long AND use of 1L in the shift
        //    public enum fMessageProperties : long
        //         importance = 1L << 31
    }

    public class cCacheProperties
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
    } */
}