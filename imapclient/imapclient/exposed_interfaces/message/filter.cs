using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a filter that can be passed to the server to filter the set of messages in a mailbox.
    /// Only the messages that 'pass through' the filter are returned to the client.
    /// Use the static members and operators of this class to create and combine filters. 
    /// </summary>
    public abstract class cFilter
    {
        /** <summary>A filter that passes all messages through.</summary>*/
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

        /// <summary>
        /// Returns a filter that passes through only messages with the specified flags.
        /// </summary>
        /// <param name="pFlags">The flags that the message should have set.</param>
        /// <returns>A message flags filter.</returns>
        public static cFilter FlagsContain(params string[] pFlags) => new cFilterFlagsContain(pFlags);

        /// <summary>
        /// Returns a filter that passes through only messages with the specified flags.
        /// </summary>
        /// <param name="pFlags">The flags that the message should have set.</param>
        /// <returns>A message flags filter.</returns>
        public static cFilter FlagsContain(cFetchableFlags pFlags) => new cFilterFlagsContain(pFlags);

        /// <summary>
        /// Returns a filter that passes through messages with the specified content in the specified header field.
        /// </summary>
        /// <param name="pHeaderField">The header field name. (Header field names are case insensitive.)</param>
        /// <param name="pContains">The content to check for.</param>
        /// <returns>A header field contents filter.</returns>
        public static cFilter HeaderFieldContains(string pHeaderField, string pContains) => new cFilterHeaderFieldContains(pHeaderField, pContains);

        /// <summary>
        /// Returns a filter that passes through messages with the specified header field.
        /// </summary>
        /// <param name="pHeaderField">The header field name. (Header field names are case insensitive.)</param>
        /// <returns>A header field existence filter.</returns>
        public static cFilter HasHeaderField(string pHeaderField) => new cFilterHeaderFieldContains(pHeaderField, string.Empty);

        /// <summary>
        /// Returns a filter that is the logical AND of the two specified filters.
        /// </summary>
        /// <param name="pA">One of the filters to AND.</param>
        /// <param name="pB">One of the filters to AND.</param>
        /// <returns>An AND combination filters.</returns>
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

        /// <summary>
        /// Returns a filter that is the logical OR of the two specified filters.
        /// </summary>
        /// <param name="pA">One of the filters to OR.</param>
        /// <param name="pB">One of the filters to OR.</param>
        /// <returns>An OR combination filter.</returns>
        public static cFilter operator |(cFilter pA, cFilter pB) => new cFilterOr(pA, pB);

        /// <summary>
        /// Returns a filter that is the logical NOT of the specified filter.
        /// </summary>
        /// <param name="pNot">The filter to invert.</param>
        /// <returns>The logical NOT of the specified filter.</returns>
        public static cFilter operator !(cFilter pNot) => new cFilterNot(pNot);

        private class cAll : cFilter
        {
            public cAll() { }
        }

        protected struct sCTorParams
        {
            public bool ContainsMessageHandles;
            public uint? UIDValidity;
        }
    }

    // suppress the warnings about not implementing == properly: here == is being used as an expression builder
    #pragma warning disable 660
    #pragma warning disable 661

    /// <summary>
    /// Specifies an offset from a specific message or from the first message in the mailbox or from the last message in the mailbox.
    /// Use <see cref="cMessage.MSNOffset(int)"/> or the static members <see cref="cFilter.First"/> or <see cref="cFilter.Last"/> to generate instances of this class.
    /// Use instances of this class with the <see cref="cFilter.MSN"/> static member to generate message sequence number filters.
    /// </summary>
    public class cFilterMSNOffset
    {
        internal readonly iMessageHandle Handle;
        internal readonly eFilterEnd? End;
        internal readonly int Offset;

        internal cFilterMSNOffset(iMessageHandle pHandle, int pOffset)
        {
            Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
            End = null; 
            Offset = pOffset;
        }

        internal cFilterMSNOffset(eFilterEnd pEnd, int pOffset)
        {
            Handle = null;
            End = pEnd;
            Offset = pOffset;
        }

        public override string ToString() => $"{nameof(cFilterMSNOffset)}({Handle},{End},{Offset})";
    }

    /// <summary>
    /// Represents either the first message in the mailbox or the last message in the mailbox.
    /// Use the <see cref="cFilter.First"/> and <see cref="cFilter.Last"/> static instances of this class to generate offsets to use with the static <see cref="cFilter.MSN"/> to generate message sequence number filters.
    /// </summary>
    public class cFilterEnd
    {
        internal readonly eFilterEnd End;

        internal cFilterEnd(eFilterEnd pEnd) { End = pEnd; }

        /// <summary>
        /// Generates an offset from the end of the mailbox that the instance represents.
        /// </summary>
        /// <param name="pOffset">The number of messages to offset by.</param>
        /// <returns>The offset.</returns>
        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(End, pOffset);

        public override string ToString() => $"{nameof(cFilterEnd)}({End})";
    }

    /// <summary>
    /// The operators defined on this class generate message sequence number filters.
    /// Use the <see cref="cFilter.MSN"/> static instance of this class to do this.
    /// </summary>
    public class cFilterMSN
    {
        internal cFilterMSN() { }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number less than the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage">The message to get the sequence number from.</param>
        /// <returns>A message sequence number filter.</returns>
        public static cFilter operator <(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.less);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number greater than the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage">The message to get the sequence number from.</param>
        /// <returns>A message sequence number filter.</returns>
        public static cFilter operator >(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.greater);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number less than the specified sequence number offset.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pOffset">The sequence number offset.</param>
        /// <returns>A message sequence number filter.</returns>
        public static cFilter operator <(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.less);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number greater than the specified sequence number offset.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pOffset">The sequence number offset.</param>
        /// <returns>A message sequence number filter.</returns>
        public static cFilter operator >(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.greater);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number less than or equal to the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage">The message to get the sequence number from.</param>
        /// <returns>A message sequence number filter.</returns>
        public static cFilter operator <=(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.lessequal);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number greater than or equal to the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage">The message to get the sequence number from.</param>
        /// <returns>A message sequence number filter.</returns>
        public static cFilter operator >=(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.greaterequal);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number less than or equal to the specified sequence number offset.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pOffset">The sequence number offset.</param>
        /// <returns>A message sequence number filter.</returns>
        public static cFilter operator <=(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.lessequal);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number greater than or equal to the specified sequence number offset.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pOffset">The sequence number offset.</param>
        /// <returns>A message sequence number filter.</returns>
        public static cFilter operator >=(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.greaterequal);
        }
    }

    /// <summary>
    /// The operators defined on this class generate message UID filters. Use the <see cref="cFilter.UID"/> static instance of this class to do this.
    /// </summary>
    public class cFilterUID
    {
        internal cFilterUID() { }

        /// <summary>
        /// Returns a filter that passes through messages with a UID less than the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID">The UID.</param>
        /// <returns>A UID filter.</returns>
        public static cFilter operator <(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pUID.UID < 2) return cFilter.False;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID - 1));
        }

        /// <summary>
        /// Returns a filter that passes through messages with a UID greater than the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID">The UID.</param>
        /// <returns>A UID filter.</returns>
        public static cFilter operator >(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pUID.UID == uint.MaxValue) return cFilter.False;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID + 1, uint.MaxValue));
        }

        /// <summary>
        /// Returns a filter that passes through messages with a UID less than or equal to the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID">The UID.</param>
        /// <returns>A UID filter.</returns>
        public static cFilter operator <=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID));
        }

        /// <summary>
        /// Returns a filter that passes through messages with a UID greater than or equal to the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID">The UID.</param>
        /// <returns>A UID filter.</returns>
        public static cFilter operator >=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID, uint.MaxValue));
        }

        /// <summary>
        /// Returns a filter that passes through messages with a UID equal to the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID">The UID.</param>
        /// <returns>A UID filter.</returns>
        public static cFilter operator ==(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID));
        }

        /// <summary>
        /// Returns a filter that passes through messages with a UID different to the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID">The UID.</param>
        /// <returns>A UID filter.</returns>
        public static cFilter operator !=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterNot(new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID)));
        }
    }

    /// <summary>
    /// Use the <see cref="Contains(string)"/> method on the following static instances of this class to generate a message content filters;
    /// <list type="bullet">
    /// <item><see cref="cFilter.BCC"/></item>
    /// <item><see cref="cFilter.Body"/></item>
    /// <item><see cref="cFilter.CC"/></item>
    /// <item><see cref="cFilter.From"/></item>
    /// <item><see cref="cFilter.Subject"/></item>
    /// <item><see cref="cFilter.Text"/></item>
    /// <item><see cref="cFilter.To"/></item>
    /// </list>
    /// </summary>
    public class cFilterPart
    {
        private readonly eFilterPart Part;
        internal cFilterPart(eFilterPart pPart) { Part = pPart; }
    
        /// <summary>
        /// Generates an object that represents a filter on message content.
        /// </summary>
        /// <param name="pContains"></param>
        /// <returns>A message content filter.</returns>
        public cFilter Contains(string pContains) => new cFilterPartContains(Part, pContains);
    }

    /// <summary>
    /// The operators defined on this class generate message date filters.
    /// Use the static instances of this class, <see cref="cFilter.Received"/> and <see cref="cFilter.Sent"/>, to do this.
    /// </summary>
    public class cFilterDate
    {
        private readonly eFilterDate Date;

        internal cFilterDate(eFilterDate pDate) { Date = pDate; }

        ;?;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pFilterDate"></param>
        /// <param name="pDate"></param>
        /// <returns></returns>
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
