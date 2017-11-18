using System;
using System.Collections.Generic;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a filter that can be passed to the server to restrict the set of messages returned to the client.
    /// </summary>
    /// <remarks>
    /// Only the messages that 'pass through' the filter are returned to the client.
    /// Use the <see langword="static"/> members and operators of this class to create and combine filters. 
    /// </remarks>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
    public abstract class cFilter
    {
        /** <summary>A filter that passes all messages through.</summary>*/
        public static readonly cFilter All = new cAll();

        /** <summary>Use this member to generate message sequence number filters.</summary>*/
        public static readonly cFilterMSN MSN = new cFilterMSN();
        /** <summary>Represents the first message in a mailbox.</summary>*/
        public static readonly cFilterEnd First = new cFilterEnd(eFilterEnd.first);
        /** <summary>Represents the last message in a mailbox.</summary>*/
        public static readonly cFilterEnd Last = new cFilterEnd(eFilterEnd.last);

        /** <summary>Use this member to generate UID filters.</summary>*/
        public static readonly cFilterUID UID = new cFilterUID();

        /** <summary>A filter that passes only answered messages through.</summary>*/
        public static readonly cFilter Answered = new cFilterFlagsContain(kMessageFlag.Answered);
        /** <summary>A filter that passes only flagged messages through.</summary>*/
        public static readonly cFilter Flagged = new cFilterFlagsContain(kMessageFlag.Flagged);
        /** <summary>A filter that passes only deleted messages through.</summary>*/
        public static readonly cFilter Deleted = new cFilterFlagsContain(kMessageFlag.Deleted);
        /** <summary>A filter that passes only seen messages through.</summary>*/
        public static readonly cFilter Seen = new cFilterFlagsContain(kMessageFlag.Seen);
        /** <summary>A filter that passes only draft messages through.</summary>*/
        public static readonly cFilter Draft = new cFilterFlagsContain(kMessageFlag.Draft);
        /** <summary>A filter that passes only recent messages through.</summary>*/
        public static readonly cFilter Recent = new cFilterFlagsContain(kMessageFlag.Recent);

        // see comments elsewhere for why this is commented out: note: when re-instating it a "MDNRequested" filter should also be added
        //public static readonly cFilter MDNSent = new cFilterFlagsContain(kMessageFlagName.MDNSent);

        /** <summary>A filter that passes only forwarded messages through.</summary>*/
        public static readonly cFilter Forwarded = new cFilterFlagsContain(kMessageFlag.Forwarded);
        /** <summary>A filter that passes only submitpending messages through.</summary>*/
        public static readonly cFilter SubmitPending = new cFilterFlagsContain(kMessageFlag.SubmitPending);
        /** <summary>A filter that passes only submitted messages through.</summary>*/
        public static readonly cFilter Submitted = new cFilterFlagsContain(kMessageFlag.Submitted);

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

        /** <summary>Use this member to generate filters on the message's received date.</summary>*/
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
        /// <param name="pFlags"></param>
        /// <returns></returns>
        public static cFilter FlagsContain(params string[] pFlags) => new cFilterFlagsContain(pFlags);

        /// <summary>
        /// Returns a filter that passes through only messages with the specified flags.
        /// </summary>
        /// <param name="pFlags"></param>
        /// <returns></returns>
        public static cFilter FlagsContain(cFetchableFlags pFlags) => new cFilterFlagsContain(pFlags);

        /// <summary>
        /// Returns a filter that passes through messages with the specified content in the specified header field.
        /// </summary>
        /// <param name="pHeaderField">Header field names are case insensitive.</param>
        /// <param name="pContains"></param>
        /// <returns></returns>
        public static cFilter HeaderFieldContains(string pHeaderField, string pContains) => new cFilterHeaderFieldContains(pHeaderField, pContains);

        /// <summary>
        /// Returns a filter that passes through messages that have the specified header field.
        /// </summary>
        /// <param name="pHeaderField">Header field names are case insensitive.</param>
        /// <returns></returns>
        public static cFilter HasHeaderField(string pHeaderField) => new cFilterHeaderFieldContains(pHeaderField, string.Empty);

        /// <summary>
        /// Returns a filter that is the logical AND of the two specified filters.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
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
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static cFilter operator |(cFilter pA, cFilter pB) => new cFilterOr(pA, pB);

        /// <summary>
        /// Returns a filter that is the logical NOT of the specified filter.
        /// </summary>
        /// <param name="pNot"></param>
        /// <returns></returns>
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
    /// Represents an offset from a specific message or from the ends of a mailbox.
    /// </summary>
    /// <remarks>
    /// Use <see cref="cMessage.MSNOffset(int)"/>, <see cref="cFilter.First"/> or <see cref="cFilter.Last"/> to generate instances of this class.
    /// Use instances of this class with <see cref="cFilter.MSN"/> to generate message sequence number filters.
    /// </remarks>
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

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cFilterMSNOffset)}({Handle},{End},{Offset})";
    }

    /// <summary>
    /// Represents an end of the mailbox.
    /// </summary>
    /// <seealso cref="cFilter.First"/>
    /// <seealso cref="cFilter.Last"/>
    public class cFilterEnd
    {
        internal readonly eFilterEnd End;

        internal cFilterEnd(eFilterEnd pEnd) { End = pEnd; }

        /// <summary>
        /// Generates an offset from the end of the mailbox that the instance represents.
        /// </summary>
        /// <param name="pOffset"></param>
        /// <returns></returns>
        public cFilterMSNOffset MSNOffset(int pOffset) => new cFilterMSNOffset(End, pOffset);

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cFilterEnd)}({End})";
    }

    /// <summary>
    /// Contains operators that generate message sequence number filters.
    /// </summary>
    /// <seealso cref="cFilter.MSN"/>
    public class cFilterMSN
    {
        internal cFilterMSN() { }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number less than the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.less);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number greater than the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.greater);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number less than the specified offset.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pOffset"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.less);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number greater than the specified offset.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pOffset"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.greater);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number less than or equal to the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage"></param>
        /// <returns></returns>
        public static cFilter operator <=(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.lessequal);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number greater than or equal to the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage"></param>
        /// <returns></returns>
        public static cFilter operator >=(cFilterMSN pFilterMSN, cMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.Handle, eFilterHandleRelativity.greaterequal);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number less than or equal to the specified offset.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pOffset"></param>
        /// <returns></returns>
        public static cFilter operator <=(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.lessequal);
        }

        /// <summary>
        /// Returns a filter that passes through messages with a sequence number greater than or equal to the specified offset.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pOffset"></param>
        /// <returns></returns>
        public static cFilter operator >=(cFilterMSN pFilterMSN, cFilterMSNOffset pOffset)
        {
            if (pOffset == null) throw new ArgumentNullException(nameof(pOffset));
            return new cFilterMSNRelativity(pOffset, eFilterHandleRelativity.greaterequal);
        }
    }

    /// <summary>
    /// Contains operators that generate message UID filters.
    /// </summary>
    /// <seealso cref="cFilter.UID"/>
    public class cFilterUID
    {
        internal cFilterUID() { }

        /// <summary>
        /// Returns a filter that passes through messages with a UID less than the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID"></param>
        /// <returns></returns>
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
        /// <param name="pUID"></param>
        /// <returns></returns>
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
        /// <param name="pUID"></param>
        /// <returns></returns>
        public static cFilter operator <=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID));
        }

        /// <summary>
        /// Returns a filter that passes through messages with a UID greater than or equal to the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID"></param>
        /// <returns></returns>
        public static cFilter operator >=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID, uint.MaxValue));
        }

        /// <summary>
        /// Returns a filter that passes through messages with a UID equal to the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID"></param>
        /// <returns></returns>
        public static cFilter operator ==(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID));
        }

        /// <summary>
        /// Returns a filter that passes through messages with a UID different to the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID"></param>
        /// <returns></returns>
        public static cFilter operator !=(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cFilterNot(new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID)));
        }
    }

    /// <summary>
    /// Contains a method that generates message content filters.
    /// </summary>
    /// <seealso cref="cFilter.BCC"/>
    /// <seealso cref="cFilter.Body"/>
    /// <seealso cref="cFilter.CC"/>
    /// <seealso cref="cFilter.From"/>
    /// <seealso cref="cFilter.Subject"/>
    /// <seealso cref="cFilter.Text"/>
    /// <seealso cref="cFilter.To"/>
    public class cFilterPart
    {
        private readonly eFilterPart Part;
        internal cFilterPart(eFilterPart pPart) { Part = pPart; }
    
        /// <summary>
        /// Generates a filter on message content.
        /// </summary>
        /// <param name="pContains"></param>
        /// <returns></returns>
        public cFilter Contains(string pContains) => new cFilterPartContains(Part, pContains);
    }

    /// <summary>
    /// Contains operators that generate message date filters.
    /// </summary>
    /// <seealso cref="cFilter.Received"/>
    /// <seealso cref="cFilter.Sent"/>
    public class cFilterDate
    {
        private readonly eFilterDate Date;

        internal cFilterDate(eFilterDate pDate) { Date = pDate; }

        /// <summary>
        /// Returns a filter that passes through messages with a date less than the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/></param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.before, pDate);

        /// <summary>
        /// Returns a filter that passes through messages with a date greater than the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/></param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.since, pDate.AddDays(1));


        /// <summary>
        /// Returns a filter that passes through messages with a date equal to the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/></param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator ==(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.on, pDate);

        /// <summary>
        /// Returns a filter that passes through messages with a date different to the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/></param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator !=(cFilterDate pFilterDate, DateTime pDate) => new cFilterNot(new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.on, pDate));

        /// <summary>
        /// Returns a filter that passes through messages with a date greater than or equal to the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/></param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator >=(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.since, pDate);

        /// <summary>
        /// Returns a filter that passes through messages with a date less than or equal to the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/></param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator <=(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.before, pDate.AddDays(1));
    }

    /// <summary>
    /// Contains operators that generate message size filters.
    /// </summary>
    /// <seealso cref="cFilter.Size"/>
    public class cFilterSize
    {
        internal cFilterSize() { }

        /// <summary>
        /// Returns a filter that passes through messages with a size less than the specified size.
        /// </summary>
        /// <param name="pFitlerSize"><see cref="cFilter.Size"/></param>
        /// <param name="pSize"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterSize pFitlerSize, int pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);

        /// <summary>
        /// Returns a filter that passes through messages with a size greater than the specified size.
        /// </summary>
        /// <param name="pFitlerSize"><see cref="cFilter.Size"/></param>
        /// <param name="pSize"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterSize pFitlerSize, int pSize) => new cFilterSizeCompare(eFilterSizeCompare.larger, pSize);

        /// <summary>
        /// Returns a filter that passes through messages with a size less than the specified size.
        /// </summary>
        /// <param name="pFitlerSize"><see cref="cFilter.Size"/></param>
        /// <param name="pSize"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);

        /// <summary>
        /// Returns a filter that passes through messages with a size greater than the specified size.
        /// </summary>
        /// <param name="pFitlerSize"><see cref="cFilter.Size"/></param>
        /// <param name="pSize"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.larger, pSize);
    }

    /// <summary>
    /// Contains operators that generate message importance filters.
    /// </summary>
    /// <seealso cref="cFilter.Importance"/>
    public class cFilterImportance
    {
        internal cFilterImportance() { }

        /// <summary>
        /// Returns a filter that passes through messages with an importance equal to the specified importance.
        /// </summary>
        /// <param name="pImportance"><see cref="cFilter.Importance"/></param>
        /// <param name="pValue"></param>
        /// <returns></returns>
        public static cFilter operator ==(cFilterImportance pImportance, eImportance pValue) => new cFilterHeaderFieldContains(kHeaderFieldName.Importance, cHeaderFieldImportance.FieldValue(pValue));

        /// <summary>
        /// Returns a filter that passes through messages with an importance different to the specified importance.
        /// </summary>
        /// <param name="pImportance"><see cref="cFilter.Importance"/></param>
        /// <param name="pValue"></param>
        /// <returns></returns>
        public static cFilter operator !=(cFilterImportance pImportance, eImportance pValue) => !new cFilterHeaderFieldContains(kHeaderFieldName.Importance, cHeaderFieldImportance.FieldValue(pValue));
    }
}
