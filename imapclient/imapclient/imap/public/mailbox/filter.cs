using System;
using System.Collections.Generic;
using System.Linq;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a filter that can be passed to the server to restrict the set of messages passed back to the client.
    /// </summary>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchCacheItemConfiguration)"/>
    public abstract class cFilter
    {
        /** <summary>A filter that passes back all messages.</summary>*/
        public static readonly cFilter All = new cAll();

        /** <summary>Use the operators of this member to generate message sequence number filters.</summary>*/
        public static readonly cFilterMSN MSN = new cFilterMSN();
        /** <summary>Represents the first message in a mailbox. Use the <see cref="cFilterEnd.MSNOffset(int)"/> method of this member to generate <see cref="cFilterMSNOffset"/> instances to use with <see cref="MSN"/> to generate message sequence number filters.</summary>*/
        public static readonly cFilterEnd First = new cFilterEnd(eFilterEnd.first);
        /** <summary>Represents the last message in a mailbox. Use the <see cref="cFilterEnd.MSNOffset(int)"/> method of this member to generate <see cref="cFilterMSNOffset"/> instances to use with <see cref="MSN"/> to generate message sequence number filters.</summary>*/
        public static readonly cFilterEnd Last = new cFilterEnd(eFilterEnd.last);

        /** <summary>Use the operators of this member to generate UID filters.</summary>*/
        public static readonly cFilterUID UID = new cFilterUID();

        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.Answered"/> flag.</summary>*/
        public static readonly cFilter Answered = new cFilterFlagsContain(kMessageFlag.Answered);
        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.Flagged"/> flag.</summary>*/
        public static readonly cFilter Flagged = new cFilterFlagsContain(kMessageFlag.Flagged);
        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.Deleted"/> flag.</summary>*/
        public static readonly cFilter Deleted = new cFilterFlagsContain(kMessageFlag.Deleted);
        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.Seen"/> flag.</summary>*/
        public static readonly cFilter Seen = new cFilterFlagsContain(kMessageFlag.Seen);
        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.Draft"/> flag.</summary>*/
        public static readonly cFilter Draft = new cFilterFlagsContain(kMessageFlag.Draft);
        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.Recent"/> flag.</summary>*/
        public static readonly cFilter Recent = new cFilterFlagsContain(kMessageFlag.Recent);

        // see comments elsewhere for why this is commented out: note: when re-instating it a "MDNRequested" filter should also be added
        //public static readonly cFilter MDNSent = new cFilterFlagsContain(kMessageFlagName.MDNSent);

        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.Forwarded"/> flag.</summary>*/
        public static readonly cFilter Forwarded = new cFilterFlagsContain(kMessageFlag.Forwarded);
        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.SubmitPending"/> flag.</summary>*/
        public static readonly cFilter SubmitPending = new cFilterFlagsContain(kMessageFlag.SubmitPending);
        /** <summary>A filter that passes back only messages with the <see cref="kMessageFlag.Submitted"/> flag.</summary>*/
        public static readonly cFilter Submitted = new cFilterFlagsContain(kMessageFlag.Submitted);

        /** <summary>Use the <see cref="cFilterPart.Contains(string)"/> method of this member to generate filters on the content of message 'BCC' data. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the generated filters to the server.</summary>*/
        public static readonly cFilterPart BCC = new cFilterPart(eFilterPart.bcc);
        /** <summary>Use the <see cref="cFilterPart.Contains(string)"/> method of this member to generate filters on the content of message 'body' data. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the generated filters to the server.</summary>*/
        public static readonly cFilterPart Body = new cFilterPart(eFilterPart.body);
        /** <summary>Use the <see cref="cFilterPart.Contains(string)"/> method of this member to generate filters on the content of message 'CC' data. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the generated filters to the server.</summary>*/
        public static readonly cFilterPart CC = new cFilterPart(eFilterPart.cc);
        /** <summary>Use the <see cref="cFilterPart.Contains(string)"/> method of this member to generate filters on the content of message 'from' data. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the generated filters to the server.</summary>*/
        public static readonly cFilterPart From = new cFilterPart(eFilterPart.from);
        /** <summary>Use the <see cref="cFilterPart.Contains(string)"/> method of this member to generate filters on the content of message 'subject' data. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the generated filters to the server.</summary>*/
        public static readonly cFilterPart Subject = new cFilterPart(eFilterPart.subject);
        /** <summary>Use the <see cref="cFilterPart.Contains(string)"/> method of this member to generate filters on the content of message 'text' data. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the generated filters to the server.</summary>*/
        public static readonly cFilterPart Text = new cFilterPart(eFilterPart.text);
        /** <summary>Use the <see cref="cFilterPart.Contains(string)"/> method of this member to generate filters on the content of message 'to' data. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the generated filters to the server.</summary>*/
        public static readonly cFilterPart To = new cFilterPart(eFilterPart.to);

        /** <summary>Use the operators of this member to generate filters on the message's received date.</summary>*/
        public static readonly cFilterDate Received = new cFilterDate(eFilterDate.arrival);
        /** <summary>Use the operators of this member to generate filters on the message's sent date.</summary>*/
        public static readonly cFilterDate Sent = new cFilterDate(eFilterDate.sent);

        /** <summary>Use the operators of this member to generate filters on the message's size.</summary>*/
        public static readonly cFilterSize Size = new cFilterSize();

        /** <summary>Use the operators of this member to generate filters on the message's importance.</summary>*/
        public static readonly cFilterImportance Importance = new cFilterImportance();

        /** <summary>A filter that passes back no messages.</summary>*/
        public static readonly cFilter None = Seen & !Seen;

        internal readonly bool ContainsMessageHandles;
        internal readonly uint? UIDValidity;

        internal cFilter()
        {
            ContainsMessageHandles = false;
            UIDValidity = null;
        }

        internal cFilter(uint pUIDValidity)
        {
            ContainsMessageHandles = false;
            UIDValidity = pUIDValidity;
        }

        internal cFilter(bool pContainsMessageHandles, uint? pUIDValidity)
        {
            ContainsMessageHandles = pContainsMessageHandles;
            UIDValidity = pUIDValidity;
        }

        internal cFilter(sCTorParams pParams)
        {
            ContainsMessageHandles = pParams.ContainsMessageHandles;
            UIDValidity = pParams.UIDValidity;
        }

        /// <summary>
        /// Returns a filter that passes back only messages that have all the specified message flags. Message flags are case insensitive.
        /// </summary>
        /// <param name="pFlags"></param>
        /// <returns></returns>
        public static cFilter FlagsContain(params string[] pFlags) => new cFilterFlagsContain(pFlags);

        /// <inheritdoc cref="FlagsContain(string[])"/>
        public static cFilter FlagsContain(cFetchableFlags pFlags) => new cFilterFlagsContain(pFlags);

        /// <summary>
        /// Returns a filter that passes back only messages that have the specified content in the specified header field. Header field names are case insensitive. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the filter to the server.
        /// </summary>
        /// <param name="pHeaderField"></param>
        /// <param name="pContains"></param>
        /// <returns></returns>
        public static cFilter HeaderFieldContains(string pHeaderField, string pContains) => new cFilterHeaderFieldContains(pHeaderField, pContains);

        /// <summary>
        /// Returns a filter that passes back only messages that have the specified header field. Header field names are case insensitive. 
        /// </summary>
        /// <param name="pHeaderField"></param>
        /// <returns></returns>
        public static cFilter HasHeaderField(string pHeaderField) => new cFilterHeaderFieldContains(pHeaderField, string.Empty);





        internal static cFilter CoveringUIDs(uint pUIDValidity, IEnumerable<uint> pUIDs, p)
        {
            bool lFirst = true;

            uint lFrom;

            foreach (var lUID in pUIDs.Distinct().OrderBy(i => i))
            {
                if (lFirst)
                {
                    lFrom = lUID;
                    lFirst = false;
                else if ()
            }



            return new cFilterUIDIn(pUIDValidity, );

        }





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
            public override string ToString() => $"{nameof(cFilter)}.{nameof(cAll)}()";
        }

        /// <summary>
        /// 
        /// </summary>
        internal struct sCTorParams
        {
            /**<summary></summary>*/
            public bool ContainsMessageHandles;
            /**<summary></summary>*/
            public uint? UIDValidity;
        }

        private static List<sUIntRange> ZGetRangesCoveringUInts(IEnumerable<uint> pUInts, int pMaxNumberCount)
        {
            if (pUInts == null) throw new ArgumentNullException(nameof(pUInts));
            if (pMaxNumberCount < 4) throw new ArgumentOutOfRangeException(nameof(pMaxNumberCount));

            // build an initial list of ranges

            var lRanges = new List<sUIntRange>();

            int lNumberCount = 0;

            bool lFirst = true;
            uint lFrom = 0;
            uint lTo = 0;

            foreach (var lUInt in pUInts.Distinct().OrderBy(i => i))
            {
                if (lFirst)
                {
                    lFrom = lUInt;
                    lTo = lUInt;
                    lFirst = false;
                }
                else if (lUInt == lTo + 1) lTo = lUInt;
                else
                {
                    lRanges.Add(new sUIntRange(lFrom, lTo));
                    if (lTo == lFrom) lNumberCount++;
                    else lNumberCount += 2;

                    lFrom = lUInt;
                    lTo = lUInt;
                }
            }

            if (lFirst) throw new ArgumentOutOfRangeException(nameof(pUInts));

            lRanges.Add(new sUIntRange(lFrom, lTo));
            if (lTo == lFrom) lNumberCount++;
            else lNumberCount += 2;

            // built

            while (lNumberCount > pMaxNumberCount)
            {
                // have to coalesce ranges

                uint lSmallestGapSize = uint.MaxValue;
                int lSmallestGapLargestSaving = 0;

                uint lLastTo;
                sUIntRange lSingle1;
                sUIntRange lSingle2;
                uint lSingle1ToSingle2GapSize;

                // find the smallest gap between ranges and the largest number of numbers we can save by closing gaps of that width

                lLastTo = 0;
                lSingle1 = sUIntRange.Zero;
                lSingle2 = sUIntRange.Zero;
                lSingle1ToSingle2GapSize = 0;

                foreach (var lRange in lRanges)
                {
                    uint lGapSize;
                    if (lLastTo == 0) lGapSize = 0;
                    else lGapSize = lRange.From - lLastTo - 1;
                    lLastTo = lRange.To;

                    if (lRange.IsSingle)
                    {
                        if (lSingle1.IsZero)
                        {
                            if (lGapSize != 0 && lGapSize < lSmallestGapSize)
                            {
                                lSmallestGapSize = lGapSize;
                                lSmallestGapLargestSaving = 1;
                            }

                            lSingle1 = lRange;
                        }
                        else if (lSingle2.IsZero)
                        {
                            lSingle2 = lRange;
                            lSingle1ToSingle2GapSize = lGapSize;
                        }
                        else
                        {
                            if (lSingle1ToSingle2GapSize + lGapSize < lSmallestGapSize)
                            {
                                lSmallestGapSize = lSingle1ToSingle2GapSize + lGapSize;
                                lSmallestGapLargestSaving = 1;
                            }

                            lSingle1 = lSingle2;
                            lSingle2 = lRange;
                            lSingle1ToSingle2GapSize = lGapSize;
                        }
                    }
                    else
                    {
                        if (lGapSize != 0)
                        {
                            if (lSingle1.IsZero && lGapSize <= lSmallestGapSize)
                            {
                                lSmallestGapSize = lGapSize;
                                lSmallestGapLargestSaving = 2;
                            }
                            else if (lGapSize < lSmallestGapSize)
                            {
                                lSmallestGapSize = lGapSize;
                                lSmallestGapLargestSaving = 1;
                            }
                        }

                        lSingle1 = sUIntRange.Zero;
                        lSingle2 = sUIntRange.Zero;
                        lSingle1ToSingle2GapSize = 0;
                    }
                }

                // close the highest yielding gaps, stopping if the number of numbers gets to the target

                var lNewRanges = new List<sUIntRange>();

                lLastTo = 0;
                lSingle1 = sUIntRange.Zero;
                lSingle2 = sUIntRange.Zero;
                lSingle1ToSingle2GapSize = 0;
                sUIntRange lLastRange = sUIntRange.Zero;

                foreach (var lRange in lRanges)
                {
                    uint lGapSize;
                    if (lLastTo == 0) lGapSize = 0;
                    else lGapSize = lRange.From - lLastTo - 1;
                    lLastTo = lRange.To;

                    if (lRange.IsSingle)
                    {
                        if (lSingle1.IsZero)
                        {
                            if (lGapSize == 0) lSingle1 = lRange;
                            else
                            {
                                if (lGapSize == lSmallestGapSize && lSmallestGapLargestSaving == 1)
                                {
                                    lNewRanges.Add(new sUIntRange(lLastRange.From, lRange.To));
                                    if (--lNumberCount == pMaxNumberCount) lSmallestGapSize = 0;
                                    lLastTo = 0;
                                    lLastRange = sUIntRange.Zero;
                                }
                                else
                                {
                                    lNewRanges.Add(lLastRange);
                                    lLastRange = sUIntRange.Zero;
                                    lSingle1 = lRange;
                                }
                            }
                        }
                        else if (lSingle2.IsZero)
                        {
                            lSingle2 = lRange;
                            lSingle1ToSingle2GapSize = lGapSize;
                        }
                        else
                        {
                            if (lSingle1ToSingle2GapSize + lGapSize == lSmallestGapSize && lSmallestGapLargestSaving == 1)
                            {
                                lNewRanges.Add(new sUIntRange(lSingle1.From, lRange.To));
                                if (--lNumberCount == pMaxNumberCount) lSmallestGapSize = 0;
                                lLastTo = 0;
                                lSingle1 = sUIntRange.Zero;
                                lSingle2 = sUIntRange.Zero;
                                lSingle1ToSingle2GapSize = 0;
                            }
                            else
                            {
                                lNewRanges.Add(lSingle1);
                                lSingle1 = lSingle2;
                                lSingle2 = lRange;
                                lSingle1ToSingle2GapSize = lGapSize;
                            }
                        }
                    }
                    else
                    {
                        if (lGapSize == 0) lLastRange = lRange;
                        else
                        {
                            if (lGapSize == lSmallestGapSize && (lSingle1.IsZero || lSmallestGapLargestSaving == 1))
                            {
                                if (lSingle1.IsZero)
                                {
                                    lNewRanges.Add(new sUIntRange(lLastRange.From, lRange.To));
                                    lNumberCount -= 2;
                                    if (lNumberCount <= pMaxNumberCount) lSmallestGapSize = 0;
                                    lLastRange = sUIntRange.Zero;
                                }
                                else
                                {
                                    if (!lSingle2.IsZero)
                                    {
                                        lNewRanges.Add(lSingle1);
                                        lSingle1 = lSingle2;
                                        lSingle2 = sUIntRange.Zero;
                                        lSingle1ToSingle2GapSize = 0;
                                    }

                                    lNewRanges.Add(new sUIntRange(lSingle1.From, lRange.To));
                                    if (--lNumberCount == pMaxNumberCount) lSmallestGapSize = 0;
                                    lSingle1 = sUIntRange.Zero;
                                }

                                lLastTo = 0;
                            }
                            else
                            {
                                if (!lSingle1.IsZero) lNewRanges.Add(lSingle1);
                                if (!lSingle2.IsZero) lNewRanges.Add(lSingle2);
                                if (!lLastRange.IsZero) lNewRanges.Add(lLastRange);
                                lSingle1 = sUIntRange.Zero;
                                lSingle2 = sUIntRange.Zero;
                                lSingle1ToSingle2GapSize = 0;
                                lLastRange = lRange;
                            }
                        }
                    }
                }

                if (!lSingle1.IsZero) lNewRanges.Add(lSingle1);
                if (!lSingle2.IsZero) lNewRanges.Add(lSingle2);
                if (!lLastRange.IsZero) lNewRanges.Add(lLastRange);

                lRanges = lNewRanges;
            }

            return lRanges;
        }

        private struct sUIntRange
        {
            public static readonly sUIntRange Zero = new sUIntRange();

            public readonly uint From;
            public readonly uint To;

            public sUIntRange(uint pFrom, uint pTo)
            {
                if (pFrom == 0) throw new ArgumentOutOfRangeException(nameof(pFrom));
                if (pTo < pFrom) throw new ArgumentOutOfRangeException(nameof(pTo));

                From = pFrom;
                To = pTo;
            }

            public bool IsSingle => To == From;
            public bool IsZero => From == 0;
        }
    }

    // suppress the warnings about not implementing == properly: here == is being used as an expression builder
#pragma warning disable 660
#pragma warning disable 661

    /// <summary>
    /// Represents an offset from a specific message or from an end of a mailbox.
    /// </summary>
    /// <remarks>
    /// Use <see cref="cIMAPMessage.MSNOffset(int)"/> or the <see cref="cFilterEnd.MSNOffset(int)"/> method of <see cref="cFilter.First"/> and <see cref="cFilter.Last"/> to generate instances of this class.
    /// Use instances of this class with the operators of <see cref="cFilter.MSN"/> to generate message sequence number filters.
    /// </remarks>
    public class cFilterMSNOffset
    {
        internal readonly iMessageHandle MessageHandle;
        internal readonly eFilterEnd? End;
        internal readonly int Offset;

        internal cFilterMSNOffset(iMessageHandle pMessageHandle, int pOffset)
        {
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
            End = null; 
            Offset = pOffset;
        }

        internal cFilterMSNOffset(eFilterEnd pEnd, int pOffset)
        {
            MessageHandle = null;
            End = pEnd;
            Offset = pOffset;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cFilterMSNOffset)}({MessageHandle},{End},{Offset})";
    }

    /// <summary>
    /// Represents an end of a mailbox.
    /// </summary>
    /// <remarks>
    /// Use <see cref="MSNOffset(int)"/> to generate <see cref="cFilterMSNOffset"/> instances to use with <see cref="cFilter.MSN"/> to generate message sequence number filters.
    /// </remarks>
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
        /// Returns a filter that passes back only messages that have a sequence number less than the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterMSN pFilterMSN, cIMAPMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.MessageHandle, eFilterHandleRelativity.less);
        }

        /// <summary>
        /// Returns a filter that passes back only messages that have a sequence number greater than the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterMSN pFilterMSN, cIMAPMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.MessageHandle, eFilterHandleRelativity.greater);
        }

        /// <summary>
        /// Returns a filter that passes back only messages that have a sequence number less than the specified offset.
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
        /// Returns a filter that passes back only messages that have a sequence number greater than the specified offset.
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
        /// Returns a filter that passes back only messages that have a sequence number less than or equal to the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage"></param>
        /// <returns></returns>
        public static cFilter operator <=(cFilterMSN pFilterMSN, cIMAPMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.MessageHandle, eFilterHandleRelativity.lessequal);
        }

        /// <summary>
        /// Returns a filter that passes back only messages that have a sequence number greater than or equal to the sequence number of the specified message.
        /// </summary>
        /// <param name="pFilterMSN"><see cref="cFilter.MSN"/></param>
        /// <param name="pMessage"></param>
        /// <returns></returns>
        public static cFilter operator >=(cFilterMSN pFilterMSN, cIMAPMessage pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            return new cFilterMSNRelativity(pMessage.MessageHandle, eFilterHandleRelativity.greaterequal);
        }

        /// <summary>
        /// Returns a filter that passes back only messages that have a sequence number less than or equal to the specified offset.
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
        /// Returns a filter that passes back only messages that have a sequence number greater than or equal to the specified offset.
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
        /// Returns a filter that passes back only messages that have a UID less than the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pUID.UID < 2) return cFilter.None;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(1, pUID.UID - 1));
        }

        /// <summary>
        /// Returns a filter that passes back only messages that have a UID greater than the specified UID.
        /// </summary>
        /// <param name="pFilterUID"><see cref="cFilter.UID"/></param>
        /// <param name="pUID"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterUID pFilterUID, cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            if (pUID.UID == uint.MaxValue) return cFilter.None;
            return new cFilterUIDIn(pUID.UIDValidity, new cSequenceSet(pUID.UID + 1, uint.MaxValue));
        }

        /// <summary>
        /// Returns a filter that passes back only messages that have a UID less than or equal to the specified UID.
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
        /// Returns a filter that passes back only messages that have a UID greater than or equal to the specified UID.
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
        /// Returns a filter that passes back only messages that have a UID equal to the specified UID.
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
        /// Returns a filter that passes back only messages that have a UID different to the specified UID.
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
    /// Contains the <see cref="Contains(string)"/> method that generates message content filters. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the generated filters to the server.
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
        /// Returns a filter that passes back only messages that have the specified message content. <see cref="cIMAPClient.Encoding"/> may need to be used when passing the filter to the server.
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
        /// Returns a filter that passes back only messages that have a date less than the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/>.</param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.before, pDate);

        /// <summary>
        /// Returns a filter that passes back only messages that have a date greater than the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/>.</param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.since, pDate.AddDays(1));


        /// <summary>
        /// Returns a filter that passes back only messages that have a date equal to the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/>.</param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator ==(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.on, pDate);

        /// <summary>
        /// Returns a filter that passes back only messages that have a date different to the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/>.</param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator !=(cFilterDate pFilterDate, DateTime pDate) => new cFilterNot(new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.on, pDate));

        /// <summary>
        /// Returns a filter that passes back only messages that have a date greater than or equal to the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/>.</param>
        /// <param name="pDate"></param>
        /// <returns></returns>
        public static cFilter operator >=(cFilterDate pFilterDate, DateTime pDate) => new cFilterDateCompare(pFilterDate.Date, eFilterDateCompare.since, pDate);

        /// <summary>
        /// Returns a filter that passes back only messages that have a date less than or equal to the specified date.
        /// </summary>
        /// <param name="pFilterDate"><see cref="cFilter.Received"/> or <see cref="cFilter.Sent"/>.</param>
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
        /// Returns a filter that passes back only messages that have a size less than the specified size.
        /// </summary>
        /// <param name="pFitlerSize"><see cref="cFilter.Size"/></param>
        /// <param name="pSize"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterSize pFitlerSize, int pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);

        /// <summary>
        /// Returns a filter that passes back only messages that have a size greater than the specified size.
        /// </summary>
        /// <param name="pFitlerSize"><see cref="cFilter.Size"/></param>
        /// <param name="pSize"></param>
        /// <returns></returns>
        public static cFilter operator >(cFilterSize pFitlerSize, int pSize) => new cFilterSizeCompare(eFilterSizeCompare.larger, pSize);

        /// <summary>
        /// Returns a filter that passes back only messages that have a size less than the specified size.
        /// </summary>
        /// <param name="pFitlerSize"><see cref="cFilter.Size"/></param>
        /// <param name="pSize"></param>
        /// <returns></returns>
        public static cFilter operator <(cFilterSize pFitlerSize, uint pSize) => new cFilterSizeCompare(eFilterSizeCompare.smaller, pSize);

        /// <summary>
        /// Returns a filter that passes back only messages that have a size greater than the specified size.
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
        /// Returns a filter that passes back only messages that have an importance equal to the specified importance.
        /// </summary>
        /// <param name="pImportance"><see cref="cFilter.Importance"/></param>
        /// <param name="pValue"></param>
        /// <returns></returns>
        public static cFilter operator ==(cFilterImportance pImportance, eImportance pValue) => new cFilterHeaderFieldContains(kHeaderFieldName.Importance, cHeaderFieldImportance.FieldValue(pValue));

        /// <summary>
        /// Returns a filter that passes back only messages that have an importance different to the specified importance.
        /// </summary>
        /// <param name="pImportance"><see cref="cFilter.Importance"/></param>
        /// <param name="pValue"></param>
        /// <returns></returns>
        public static cFilter operator !=(cFilterImportance pImportance, eImportance pValue) => !new cFilterHeaderFieldContains(kHeaderFieldName.Importance, cHeaderFieldImportance.FieldValue(pValue));
    }
}
