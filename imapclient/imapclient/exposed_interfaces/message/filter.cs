using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>Represents a filter that can be passed to the server to restrict the set of messages returned.</para>
    /// <para>Use the static members of the class to create cFilter instances and the &amp;, | and ! operators to combine the filters together.</para>
    /// </summary>
    public abstract class cFilter
    {
        /** <summary>A filter that passes everything through.</summary>*/
        public static readonly cFilter All = new cAll();

        /** <summary>Use this member to generate message sequence number filters.</summary>*/
        public static readonly cFilterMSN MSN = new cFilterMSN();

        /** <summary>Use this member to generate UID filters.</summary>*/
        public static readonly cFilterUID UID = new cFilterUID();

        /** <summary>Use this member to help generate message sequence number filters.</summary>*/
        public static readonly cFilterEnd First = new cFilterEnd(eFilterEnd.first);
        /** <summary>Use this member to help generate message sequence number filters.</summary>*/
        public static readonly cFilterEnd Last = new cFilterEnd(eFilterEnd.last);

        /** <summary>A filter that passes only answered messages through.</summary>*/
        public static readonly cFilter Answered = new cFilterFlagsContain(kMessageFlagName.Answered);
        /** <summary>A filter that passes only flagged messages through.</summary>*/
        public static readonly cFilter Flagged = new cFilterFlagsContain(kMessageFlagName.Flagged);
        /** <summary>A filter that passes only deleted messages through.</summary>*/
        public static readonly cFilter Deleted = new cFilterFlagsContain(kMessageFlagName.Deleted);
        /** <summary>A filter that passes only seen messages through.</summary>*/
        public static readonly cFilter Seen = new cFilterFlagsContain(kMessageFlagName.Seen);
        /** <summary>A filter that passes only draft messages through.</summary>*/
        public static readonly cFilter Draft = new cFilterFlagsContain(kMessageFlagName.Draft);
        /** <summary>A filter that passes only recent messages through.</summary>*/
        public static readonly cFilter Recent = new cFilterFlagsContain(kMessageFlagName.Recent);

        // see comments elsewhere for why this is commented out: note: when re-instating it a "MDNRequested" filter should also be added
        //public static readonly cFilter MDNSent = new cFilterFlagsContain(kMessageFlagName.MDNSent);

        /** <summary>A filter that passes only forwarded messages through.</summary>*/
        public static readonly cFilter Forwarded = new cFilterFlagsContain(kMessageFlagName.Forwarded);
        /** <summary>A filter that passes only submitpending messages through.</summary>*/
        public static readonly cFilter SubmitPending = new cFilterFlagsContain(kMessageFlagName.SubmitPending);
        /** <summary>A filter that passes only submitted messages through.</summary>*/
        public static readonly cFilter Submitted = new cFilterFlagsContain(kMessageFlagName.Submitted);

        /** <summary>Use this member to generate filters on the content of the message's BCC data.</summary>*/
        public static readonly cFilterPart BCC = new cFilterPart(eFilterPart.bcc);
        /** <summary>Use this member to generate filters on the content of the message's 'body' data.</summary>*/
        public static readonly cFilterPart Body = new cFilterPart(eFilterPart.body);
        /** <summary>Use this member to generate filters on the content of the message's CC data.</summary>*/
        public static readonly cFilterPart CC = new cFilterPart(eFilterPart.cc);
        /** <summary>Use this member to generate filters on the content of the message's 'from' data.</summary>*/
        public static readonly cFilterPart From = new cFilterPart(eFilterPart.from);
        /** <summary>Use this member to generate filters on the content of the message's 'subject' data.</summary>*/
        public static readonly cFilterPart Subject = new cFilterPart(eFilterPart.subject);
        /** <summary>Use this member to generate filters on the content of the message's 'text' data.</summary>*/
        public static readonly cFilterPart Text = new cFilterPart(eFilterPart.text);
        /** <summary>Use this member to generate filters on the content of the message's 'to' data.</summary>*/
        public static readonly cFilterPart To = new cFilterPart(eFilterPart.to);

        /** <summary>Use this member to generate filters on the message's internal date.</summary>*/
        public static readonly cFilterDate Received = new cFilterDate(eFilterDate.arrival);
        /** <summary>Use this member to generate filters on the message's sent date.</summary>*/
        public static readonly cFilterDate Sent = new cFilterDate(eFilterDate.sent);

        /** <summary>Use this member to generate filters on the message's size.</summary>*/
        public static readonly cFilterSize Size = new cFilterSize();

        /** <summary>Use this member to generate filters on the message's importance.</summary>*/
        public static readonly cFilterImportance Importance = new cFilterImportance();

        /** <summary>A filter that passes nothing through.</summary>*/
        public static readonly cFilter False = Seen & !Seen;

        internal readonly bool ContainsMessageHandles;
        internal readonly uint? UIDValidity;

        protected cFilter()
        {
            ContainsMessageHandles = false;
            UIDValidity = null;
        }

        protected cFilter(uint pUIDValidity)
        {
            ContainsMessageHandles = false;
            UIDValidity = pUIDValidity;
        }

        protected cFilter(bool pContainsMessageHandles, uint? pUIDValidity)
        {
            ContainsMessageHandles = pContainsMessageHandles;
            UIDValidity = pUIDValidity;
        }

        protected cFilter(sCTorParams pParams)
        {
            ContainsMessageHandles = pParams.ContainsMessageHandles;
            UIDValidity = pParams.UIDValidity;
        }

        /** <summary>Use this member to generate filters on the message's flags.</summary>*/
        public static cFilter FlagsContain(params string[] pFlags) => new cFilterFlagsContain(pFlags);
        /** <summary>Use this member to generate filters on the message's flags.</summary>*/
        public static cFilter FlagsContain(cFetchableFlags pFlags) => new cFilterFlagsContain(pFlags);

        /** <summary>Use this member to generate filters on the contents of a specified header field.</summary>*/
        public static cFilter HeaderFieldContains(string pHeaderField, string pContains) => new cFilterHeaderFieldContains(pHeaderField, pContains);
        /** <summary>Use this member to generate filters on the existence of a specified header field.</summary>*/
        public static cFilter HasHeaderField(string pHeaderField) => new cFilterHeaderFieldContains(pHeaderField, string.Empty);

        /** <summary>Use this operator to combine two filters.</summary>*/
        public static cFilter operator &(cFilter pA, cFilter pB)
        {
            if (pA == null) throw new ArgumentNullException(nameof(pA));
            if (pB == null) throw new ArgumentNullException(nameof(pB));

            if (pA is cFilterFlagsContain lFCA && pB is cFilterFlagsContain lFCB)
            {
                cFetchableFlagList lFlags = new cFetchableFlagList();
                lFlags.Add(lFCA.Flags);
                lFlags.Add(lFCB.Flags);
                return new cFilterFlagsContain(lFlags);
            }

            List<cFilter> lItems = new List<cFilter>();

            if (pA is cFilterAnd lAA) lItems.AddRange(lAA.Terms);
            else lItems.Add(pA);

            if (pB is cFilterAnd lAB) lItems.AddRange(lAB.Terms);
            else lItems.Add(pB);

            return new cFilterAnd(lItems);
        }

        /** <summary>Use this operator to combine two filters.</summary>*/
        public static cFilter operator |(cFilter pA, cFilter pB) => new cFilterOr(pA, pB);
        /** <summary>Use this operator to negate a filter.</summary>*/
        public static cFilter operator !(cFilter pNot) => new cFilterNot(pNot);

        private class cAll : cFilter
        {
            public cAll() { }
        }

        /** <summary>Intended for internal use.</summary>*/
        protected struct sCTorParams
        {
            public bool ContainsMessageHandles;
            public uint? UIDValidity;
        }
    }

    /** <summary>The type of message sequence number comparison. Intended for internal use.</summary>*/
    public enum eFilterHandleRelativity { less, lessequal, greaterequal, greater }
    /** <summary>The message attribute being filtered by. Intended for internal use.</summary>*/
    public enum eFilterPart { bcc, body, cc, from, subject, text, to }
    /** <summary>The message date being filtered by. Intended for internal use.</summary>*/
    public enum eFilterDate { arrival, sent }
    /** <summary>The type of date comparison. Intended for internal use.</summary>*/
    public enum eFilterDateCompare { before, on, since }
    /** <summary>The type of size comparison. Intended for internal use.</summary>*/
    public enum eFilterSizeCompare { smaller, larger }
    /** <summary>The end of the message sequence. Intended for internal use.</summary>*/
    public enum eFilterEnd { first, last }

    // suppress the warnings about not implementing == properly: here == is being used as an expression builder
    #pragma warning disable 660
    #pragma warning disable 661

    /// <summary>
    /// <para>Represents a message sequence number message filter.</para>
    /// <para>Use the static member <see cref="cFilter.MSN"/> to generate these.</para>
    /// </summary>
    public class cFilterMSNRelativity : cFilter
    {
        public readonly iMessageHandle Handle;
        public readonly eFilterEnd? End;
        public readonly int Offset;
        public readonly eFilterHandleRelativity Relativity;

        public cFilterMSNRelativity(iMessageHandle pHandle, eFilterHandleRelativity pRelativity) : base(true, pHandle.Cache.UIDValidity)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            End = null;
            Offset = 0;
            Relativity = pRelativity;
        }

        public cFilterMSNRelativity(cFilterMSNOffset pOffset, eFilterHandleRelativity pRelativity)
        {
            End = pOffset.End;
            Handle = pOffset.Handle;
            Offset = pOffset.Offset;
            Relativity = pRelativity;
        }

        public override string ToString() => $"{nameof(cFilterMSNRelativity)}({UIDValidity},{Handle},{End},{Offset},{Relativity})";
    }

    /// <summary>
    /// <para>Specifies an offset from a specific message or from the first message in the mailbox or from the last message in the mailbox.</para>
    /// <para>Use <see cref="cMessage.MSNOffset(int)"/> or the static members <see cref="cFilter.First"/> or <see cref="cFilter.Last"/> to generate instances of this class.</para>
    /// <para>Use instances of this class with the <see cref="cFilter.MSN"/> static member to generate message sequence number filters.</para>
    /// </summary>
    public class cFilterMSNOffset
    {
        public readonly iMessageHandle Handle;
        public readonly eFilterEnd? End;
        public readonly int Offset;

        public cFilterMSNOffset(iMessageHandle pHandle, int pOffset)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            End = null; 
            Offset = pOffset;
        }

        public cFilterMSNOffset(eFilterEnd pEnd, int pOffset)
        {
            Handle = null;
            End = pEnd;
            Offset = pOffset;
        }

        public override string ToString() => $"{nameof(cFilterMSNOffset)}({Handle},{End},{Offset})";
    }

    /// <summary>
    /// <para>Represents either the first message in the mailbox or the last message in the mailbox.</para>
    /// <para>Use the <see cref="cFilter.First"/> and <see cref="cFilter.Last"/> static instances of this class to generate offsets to use with the static <see cref="cFilter.MSN"/> to generate message sequence number filters.</para>
    /// </summary>
    public class cFilterEnd
    {
        public readonly eFilterEnd End;

        public cFilterEnd(eFilterEnd pEnd) { End = pEnd; }

        /// <summary>
        /// Generates an offset from the end of the mailbox that the instance represents.
        /// </summary>
        /// <param name="pOffset">The number of messages to offset by.</param>
        /// <returns>The offset.</returns>
        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(End, pOffset);

        public override string ToString() => $"{nameof(cFilterEnd)}({End})";
    }

    /// <summary>
    /// <para>The operators defined on this class generate message sequence number filters.</para>
    /// <para>Use the <see cref="cFilter.MSN"/> static instance of this class to do this.</para>
    /// <para>The operators defined are; &lt;, &gt;, &lt;= and &gt;=.</para>
    /// <para>Use the operators to compare to a <see cref="cMessage"/> or to a <see cref="cFilterMSNOffset"/>.</para>
    /// </summary>
    public class cFilterMSN
    {
        public cFilterMSN() { }

        public static cFilter operator <(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.less);
        }

        public static cFilter operator >(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.greater);
        }

        public static cFilter operator <(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.less);
        }

        public static cFilter operator >(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.greater);
        }

        public static cFilter operator <=(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.lessequal);
        }

        public static cFilter operator >=(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.greaterequal);
        }

        public static cFilter operator <=(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.lessequal);
        }

        public static cFilter operator >=(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.greaterequal);
        }
    }

    /// <summary>
    /// <para>Represents a UID message filter.</para>
    /// <para>Use the static member <see cref="cFilter.UID"/> to generate these.</para>
    /// </summary>
    public class cFilterUIDIn : cFilter
    {
        public readonly cSequenceSet SequenceSet;
        public cFilterUIDIn(uint pUIDValidity, cSequenceSet pSequenceSet) : base(pUIDValidity) { SequenceSet = pSequenceSet ?? throw new ArgumentNullException(nameof(pSequenceSet)); }
        public override string ToString() => $"{nameof(cFilterUIDIn)}({UIDValidity},{SequenceSet})";
    }

    /// <summary>
    /// <para>The operators defined on this class generate message UID filters.</para>
    /// <para>Use the <see cref="cFilter.UID"/> static instance of this class to do this.</para>
    /// <para>The operators defined are; &lt;, &gt;, &lt;=, &gt;=, == and !=.</para>
    /// <para>Use the operators to compare to a <see cref="cUID"/> instance.</para>
    /// </summary>
    public class cFilterUID
    {
        public cFilterUID() { }

        public static cFilter operator <(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pUID.UID < 2) return cFilter.False;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID - 1));
        }

        public static cFilter operator >(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pUID.UID == uint.MaxValue) return cFilter.False;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID + 1, uint.MaxValue));
        }

        public static cFilter operator <=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID));
        }

        public static cFilter operator >=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID, uint.MaxValue));
        }

        public static cFilter operator ==(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID));
        }

        public static cFilter operator !=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterNot(new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID)));
        }
    }

    /// <summary>
    /// <para>Represents a message flag filter.</para>
    /// <para>Use the static methods <see cref="cFilter.FlagsContain(cFetchableFlags)"/> or <see cref="cFilter.FlagsContain(string[])"/> to generate these.</para>
    /// </summary>
    public class cFilterFlagsContain : cFilter
    {
        public readonly cFetchableFlags Flags;

        public cFilterFlagsContain(params string[] pFlags)
        {
            if (pFlags == null || pFlags.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
            Flags = new cFetchableFlags(pFlags);
        }

        public cFilterFlagsContain(cFetchableFlags pFlags)
        {
            if (pFlags == null || pFlags.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFlags));
            Flags = pFlags;
        }

        public override string ToString() => $"{nameof(cFilterFlagsContain)}({Flags})";
    }

    /// <summary>
    /// <para>Represents a filter on the content of a message part.</para>
    /// <para>Use the <see cref="cFilterPart.Contains(string)"/> method on the following static members of <see cref="cFilter"/> to generate these;
    /// <list type="bullet">
    /// <item><description><see cref="cFilter.BCC"/></description></item>
    /// <item><description><see cref="cFilter.Body"/></description></item>
    /// <item><description><see cref="cFilter.CC"/></description></item>
    /// <item><description><see cref="cFilter.From"/></description></item>
    /// <item><description><see cref="cFilter.Subject"/></description></item>
    /// <item><description><see cref="cFilter.Text"/></description></item>
    /// <item><description><see cref="cFilter.To"/></description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class cFilterPartContains : cFilter
    {
        public readonly eFilterPart Part;
        public readonly string Contains; // have to convert to an astring

        public cFilterPartContains(eFilterPart pPart, string pContains)
        {
            Part = pPart;
            Contains = pContains ?? throw new ArgumentNullException(nameof(pContains));
        }

        public override string ToString() => $"{nameof(cFilterPartContains)}({Part},{Contains})";
    }

    /// <summary>
    /// <para>The <see cref="Contains(string)"/> method of this class generates a message content filter.</para>
    /// <para>Use the following static instances of this class to do this;
    /// <list type="bullet">
    /// <item><description><see cref="cFilter.BCC"/></description></item>
    /// <item><description><see cref="cFilter.Body"/></description></item>
    /// <item><description><see cref="cFilter.CC"/></description></item>
    /// <item><description><see cref="cFilter.From"/></description></item>
    /// <item><description><see cref="cFilter.Subject"/></description></item>
    /// <item><description><see cref="cFilter.Text"/></description></item>
    /// <item><description><see cref="cFilter.To"/></description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class cFilterPart
    {
        private readonly eFilterPart Part;
        public cFilterPart(eFilterPart pPart) { Part = pPart; }

        /// <summary>
        /// Generates an object that represents a filter on message content.
        /// </summary>
        /// <param name="pContains"></param>
        /// <returns>An object that represents a filter on message content.</returns>
        public cFilter Contains(string pContains) => new cFilterPartContains(Part, pContains);
    }

    /// <summary>
    /// <para>Represents a filter on a message date.</para>
    /// <para>Use the following static members of <see cref="cFilter"/> to generate these;
    /// <list type="bullet">
    /// <item><description><see cref="cFilter.Received"/></description></item>
    /// <item><description><see cref="cFilter.Sent"/></description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class cFilterDateCompare : cFilter
    {
        public readonly eFilterDate Date;
        public readonly eFilterDateCompare Compare;
        public readonly DateTime WithDate;

        public cFilterDateCompare(eFilterDate pDate, eFilterDateCompare pCompare, DateTime pWithDate)
        {
            Date = pDate;
            Compare = pCompare;
            WithDate = pWithDate;
        }

        public override string ToString() => $"{nameof(cFilterDateCompare)}({Date},{Compare},{WithDate})";
    }

    /// <summary>
    /// <para>The operators defined on this class generate message date filters.</para>
    /// <para>Use the static instances of this class, <see cref="cFilter.Received"/> and <see cref="cFilter.Sent"/>, to do this.</para>
    /// <para>The operators defined are; &lt;, &gt;, &lt;=, &gt;=, == and !=.</para>
    /// </summary>
    public class cFilterDate
    {
        private readonly eFilterDate Date;

        public cFilterDate(eFilterDate pDate) { Date = pDate; }

        public static cFilter operator <(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.before, pDate);
        public static cFilter operator >(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.since, pDate.AddDays(1));

        public static cFilter operator ==(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.on, pDate);
        public static cFilter operator !=(cFilterDate pFilterDate, DateTime pDate) => new cFilterNot(new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.on, pDate));

        public static cFilter operator >=(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.since, pDate);
        public static cFilter operator <=(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.before, pDate.AddDays(1));
    }

    /// <summary>
    /// <para>Represents a message header field content filter.</para>
    /// <para>Use the <see cref="cFilter.HeaderFieldContains(string, string)"/> static method to generate these.</para>
    /// </summary>
    public class cFilterHeaderFieldContains : cFilter
    {
        public readonly string HeaderField;
        public readonly string Contains; // have to convert to an astring

        public cFilterHeaderFieldContains(string pHeaderField, string pContains)
        {
            HeaderField = pHeaderField ?? throw new ArgumentNullException(nameof(HeaderField));
            if (!cCommandPartFactory.TryAsASCIIAString(HeaderField, out _)) throw new ArgumentOutOfRangeException(nameof(HeaderField));
            Contains = pContains ?? throw new ArgumentNullException(nameof(pContains));
        }

        public override string ToString() => $"{nameof(cFilterHeaderFieldContains)}({HeaderField},{Contains})";
    }

    /// <summary>
    /// <para>Represents a message size filter.</para>
    /// <para>Use the <see cref="cFilter.Size"/> static member to generate these.</para>
    /// </summary>
    public class cFilterSizeCompare : cFilter
    {
        public readonly eFilterSizeCompare Compare;
        public readonly uint WithSize;

        public cFilterSizeCompare(eFilterSizeCompare pCompare, int pSize)
        {
            if (pSize < 0) throw new ArgumentOutOfRangeException(nameof(pSize));
            Compare = pCompare;
            WithSize = (uint)pSize;
        }

        public cFilterSizeCompare(eFilterSizeCompare pCompare, uint pSize)
        {
            Compare = pCompare;
            WithSize = pSize;
        }

        public override string ToString() => $"{nameof(cFilterSizeCompare)}({Compare},{WithSize})";
    }

    /// <summary>
    /// <para>The operators defined on this class generate message size filters.</para>
    /// <para>Use the <see cref="cFilter.Size"/> static instance of this class to do this.</para>
    /// <para>The &lt; and &gt; operators are defined.</para>
    /// </summary>
    public class cFilterSize
    {
        public cFilterSize() { }
        public static cFilter operator <(cFilterSize pFitlerSize, int pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);
        public static cFilter operator >(cFilterSize pFitlerSize, int pSize) => new cFilterSizeCompare(eFilterSizeCompare.larger, pSize);
        public static cFilter operator <(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);
        public static cFilter operator >(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.larger, pSize);
    }

    /// <summary>
    /// <para>The operators defined on this class generate message importance filters.</para>
    /// <para>Use the <see cref="cFilter.Importance"/> static instance of this class to do this.</para>
    /// <para>The == and != operators are defined.</para>
    /// <para>Use the operators to compare to a <see cref="eImportance"/> value.</para>
    /// </summary>
    public class cFilterImportance
    {
        public cFilterImportance() { }
        public static cFilter operator ==(cFilterImportance pImportance, eImportance pValue) => new cFilterHeaderFieldContains(kHeaderFieldName.Importance, cHeaderFieldImportance.FieldValue(pValue));
        public static cFilter operator !=(cFilterImportance pImportance, eImportance pValue) => !new cFilterHeaderFieldContains(kHeaderFieldName.Importance, cHeaderFieldImportance.FieldValue(pValue));
    }

    /// <summary>
    /// <para>Represents the logical and combination of a number of filters.</para>
    /// <para>Use the &amp; operator defined on the <see cref="cFilter"/> class to generate these.</para>
    /// </summary>
    public class cFilterAnd : cFilter
    {
        public readonly ReadOnlyCollection<cFilter> Terms;

        public cFilterAnd(IList<cFilter> pTerms) : base(ZCTorParams(pTerms))
        {
            Terms = new ReadOnlyCollection<cFilter>(new List<cFilter>(pTerms));
        }

        private static sCTorParams ZCTorParams(IList<cFilter> pTerms)
        {
            if (pTerms == null) throw new ArgumentNullException(nameof(pTerms));
            if (pTerms.Count < 2) throw new ArgumentOutOfRangeException(nameof(pTerms));

            sCTorParams lParams = new sCTorParams();

            foreach (var lTerm in pTerms)
            {
                if (lTerm == null) throw new ArgumentOutOfRangeException(nameof(pTerms), "null list elements");
                if (lTerm.ContainsMessageHandles) lParams.ContainsMessageHandles = true;
                if (lParams.UIDValidity == null) lParams.UIDValidity = lTerm.UIDValidity;
                else if (lTerm.UIDValidity != null && lTerm.UIDValidity != lParams.UIDValidity) throw new ArgumentOutOfRangeException(nameof(pTerms));
            }

            return lParams;
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cFilterAnd));
            foreach (var lTerm in Terms) lBuilder.Append(lTerm);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// <para>Represents the logical or combination of two filters.</para>
    /// <para>Use the | operator defined on the <see cref="cFilter"/> class to generate these.</para>
    /// </summary>
    public class cFilterOr : cFilter
    {
        public readonly cFilter A;
        public readonly cFilter B;

        public cFilterOr(cFilter pA, cFilter pB) : base(ZCTorParams(pA, pB))
        {
            A = pA;
            B = pB;
        }

        private static sCTorParams ZCTorParams(cFilter pA, cFilter pB)
        {
            if (pA == null) throw new ArgumentNullException(nameof(pA));
            if (pB == null) throw new ArgumentNullException(nameof(pB));

            sCTorParams lParams = new sCTorParams();

            if (pA.ContainsMessageHandles || pB.ContainsMessageHandles) lParams.ContainsMessageHandles = true;

            if (pA.UIDValidity == null) lParams.UIDValidity = pB.UIDValidity;
            else if (pB.UIDValidity == null) lParams.UIDValidity = pA.UIDValidity;
            else if (pA.UIDValidity != pB.UIDValidity) throw new ArgumentOutOfRangeException();
            else lParams.UIDValidity = pA.UIDValidity;

            return lParams;
        }

        public override string ToString() => $"{nameof(cFilterOr)}({A},{B})";
    }

    /// <summary>
    /// <para>Represents the negation of a filter.</para>
    /// <para>Use the ! operator defined on the <see cref="cFilter"/> class to generate these.</para>
    /// </summary>
    public class cFilterNot : cFilter
    {
        public readonly cFilter Not;

        public cFilterNot(cFilter pNot) : base(pNot.ContainsMessageHandles, pNot.UIDValidity)
        {
            Not = pNot ?? throw new ArgumentNullException(nameof(pNot));
        }

        public override string ToString() => $"{nameof(cFilterNot)}({Not})";
    }
}
