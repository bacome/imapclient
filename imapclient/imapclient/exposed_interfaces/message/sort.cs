﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /** <summary>Items that can be sorted by.</summary> */
    public enum eSortItem { received, cc, sent, from, size, subject, to, displayfrom, displayto }

    /// <summary>
    /// <para>An item to sort messages by.</para>
    /// <para>Use the static instances that are members of the class to improve readability of your sort specification.</para>
    /// </summary>
    public class cSortItem
    {
        /** <summary>Ascending by message internal date.</summary> */
        public static readonly cSortItem Received = new cSortItem(eSortItem.received, false);
        /** <summary>Ascending by the first address in the message CC.</summary> */
        public static readonly cSortItem CC = new cSortItem(eSortItem.cc, false);
        /** <summary>Ascending by the message sent date.</summary> */
        public static readonly cSortItem Sent = new cSortItem(eSortItem.sent, false);
        /** <summary>Ascending by the first address in the message 'from'.</summary> */
        public static readonly cSortItem From = new cSortItem(eSortItem.from, false);
        /** <summary>Ascending by the message size.</summary> */
        public static readonly cSortItem Size = new cSortItem(eSortItem.size, false);
        /** <summary>Ascending by the message subject.</summary> */
        public static readonly cSortItem Subject = new cSortItem(eSortItem.subject, false);
        /** <summary>Ascending by the first address in the message 'to'.</summary> */
        public static readonly cSortItem To = new cSortItem(eSortItem.to, false);
        /** <summary>Ascending by the display version (see RFC 5957) of the first address in the message 'from'.</summary> */
        public static readonly cSortItem DisplayFrom = new cSortItem(eSortItem.displayfrom, false);
        /** <summary>Ascending by the display version (see RFC 5957) of the first address in the message 'to'.</summary> */
        public static readonly cSortItem DisplayTo = new cSortItem(eSortItem.displayto, false);

        /** <summary>Descending by message internal date.</summary> */
        public static readonly cSortItem ReceivedDesc = new cSortItem(eSortItem.received, true);
        /** <summary>Descending by the first address in the message CC.</summary> */
        public static readonly cSortItem CCDesc = new cSortItem(eSortItem.cc, true);
        /** <summary>Descending by the message sent date.</summary> */
        public static readonly cSortItem SentDesc = new cSortItem(eSortItem.sent, true);
        /** <summary>Descending by the first address in the message 'from'.</summary> */
        public static readonly cSortItem FromDesc = new cSortItem(eSortItem.from, true);
        /** <summary>Descending by the message size.</summary> */
        public static readonly cSortItem SizeDesc = new cSortItem(eSortItem.size, true);
        /** <summary>Descending by the message subject.</summary> */
        public static readonly cSortItem SubjectDesc = new cSortItem(eSortItem.subject, true);
        /** <summary>Descending by the first address in the message 'to'.</summary> */
        public static readonly cSortItem ToDesc = new cSortItem(eSortItem.to, true);
        /** <summary>Descending by the display version (see RFC 5957) of the first address in the message 'from'.</summary> */
        public static readonly cSortItem DisplayFromDesc = new cSortItem(eSortItem.displayfrom, true);
        /** <summary>Descending by the display version (see RFC 5957) of the first address in the message 'to'.</summary> */
        public static readonly cSortItem DisplayToDesc = new cSortItem(eSortItem.displayto, true);

        /// <summary>
        /// The item being sorted by.
        /// </summary>
        public readonly eSortItem Item;

        /// <summary>
        /// If sorting is to be done client-side this is the message cache attribute that is required.
        /// </summary>
        public readonly fCacheAttributes Attribute;

        /// <summary>
        /// Indicates descending sort.
        /// </summary>
        public readonly bool Desc;

        public cSortItem(eSortItem pItem, bool pDesc)
        {
            Item = pItem;

            switch (pItem)
            {
                case eSortItem.received:

                    Attribute = fCacheAttributes.received;
                    break;

                case eSortItem.cc:
                case eSortItem.sent:
                case eSortItem.from:
                case eSortItem.subject:
                case eSortItem.to:
                case eSortItem.displayfrom:
                case eSortItem.displayto:

                    Attribute = fCacheAttributes.envelope;
                    break;

                case eSortItem.size:

                    Attribute = fCacheAttributes.size;
                    break;

                default:

                    throw new ArgumentOutOfRangeException(nameof(pItem));
            }

            Desc = pDesc;
        }

        public override string ToString() => $"{nameof(cSortItem)}({Item},{Attribute},{Desc})";
    }

    /// <summary>
    /// Defines a sort order for message lists.
    /// </summary>
    public class cSort : IComparer<iMessageHandle>, IComparer<cMessage>
    {
        /// <summary>
        /// An instance representing that no sorting is required.
        /// </summary>
        public static readonly cSort None = new cSort("none");

        // partial thread implementation removed - still to understand the responses and implement references algorithm
        //public static readonly cSort ThreadOrderedSubject = new cSort("thread:orderedsubject");
        //public static readonly cSort ThreadReferences = new cSort("thread:references");

        private readonly string mName;

        /// <summary>
        /// A collection of the items in this sort.
        /// </summary>
        public readonly ReadOnlyCollection<cSortItem> Items;

        private cSort(string pName)
        {
            mName = pName ?? throw new ArgumentNullException(nameof(pName));
            Items = null;
        }

        public cSort(IEnumerable<cSortItem> pItems)
        {
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            List<cSortItem> lItems = new List<cSortItem>();

            foreach (var lItem in pItems)
            {
                if (lItem == null) throw new ArgumentOutOfRangeException(nameof(pItems));
                lItems.Add(lItem);
            }

            if (lItems.Count == 0) throw new ArgumentOutOfRangeException(nameof(pItems));

            mName = null;
            Items = new ReadOnlyCollection<cSortItem>(lItems);
        }

        public cSort(params cSortItem[] pItems)
        {
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));
            if (pItems.Length == 0) throw new ArgumentOutOfRangeException(nameof(pItems));

            foreach (var lItem in pItems) if (lItem == null) throw new ArgumentOutOfRangeException(nameof(pItems));

            mName = null;
            Items = new ReadOnlyCollection<cSortItem>(pItems);
        }

        /// <summary>
        /// Compares two message handles according to the sort definition.
        /// If the attributes required for the comparision are not in the message cache the results are undefined.
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <returns></returns>
        public int Compare(iMessageHandle pX, iMessageHandle pY)
        {
            if (Items == null) throw new InvalidOperationException();

            if (pX == null)
            {
                if (pY == null) return 0;
                return -1;
            }

            if (pY == null) return 1;

            foreach (var lItem in Items)
            {
                int lCompareTo;

                switch (lItem.Item)
                {
                    case eSortItem.received:

                        lCompareTo = ZCompareTo(pX.Received, pY.Received);
                        break;

                    case eSortItem.cc:

                        lCompareTo = ZCompareTo(pX.Envelope?.CC?.SortString, pY.Envelope?.CC?.SortString);
                        break;

                    case eSortItem.sent:

                        // rfc 5256 says to use the internal date if the sent date is null
                        lCompareTo = ZCompareTo(pX.Envelope?.Sent ?? pX.Received, pY.Envelope?.Sent ?? pY.Received);
                        break;

                    case eSortItem.from:

                        lCompareTo = ZCompareTo(pX.Envelope?.From?.SortString, pY.Envelope?.From?.SortString);
                        break;

                    case eSortItem.size:

                        lCompareTo = ZCompareTo(pX.Size, pY.Size);
                        break;

                    case eSortItem.subject:

                        lCompareTo = ZCompareTo(pX.Envelope?.BaseSubject, pY.Envelope?.BaseSubject);
                        break;

                    case eSortItem.to:

                        lCompareTo = ZCompareTo(pX.Envelope?.To?.SortString, pY.Envelope?.To?.SortString);
                        break;

                    case eSortItem.displayfrom:

                        lCompareTo = ZCompareTo(pX.Envelope?.From?.DisplaySortString, pY.Envelope?.From?.DisplaySortString);
                        break;

                    case eSortItem.displayto:

                        lCompareTo = ZCompareTo(pX.Envelope?.To?.DisplaySortString, pY.Envelope?.To?.DisplaySortString);
                        break;

                    default:

                        lCompareTo = 0;
                        break;
                }

                if (lCompareTo != 0)
                {
                    if (lItem.Desc) return -lCompareTo;
                    else return lCompareTo;
                }
            }

            return pX.CacheSequence.CompareTo(pY.CacheSequence);
        }

        /// <summary>
        /// Compares two messages according to the sort definition.
        /// If the attributes required for the comparision are not in the message cache the attributes are fetched.
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <returns></returns>
        public int Compare(cMessage pX, cMessage pY)
        {
            if (Items == null) throw new InvalidOperationException();

            if (pX == null)
            {
                if (pY == null) return 0;
                return -1;
            }

            if (pY == null) return 1;

            cCacheItems lItems = Attributes(out _);
            pX.Fetch(lItems);
            pY.Fetch(lItems);
            return Compare(pX.Handle, pY.Handle);
        }

        /// <summary>
        /// Returns the set of message attributes required by this sort and whether SORT=DISPLAY (RFC 5957) support is required for the server to do the sort.
        /// </summary>
        /// <param name="rDisplay">Returns true if SORT=DISPLAY (RFC 5957) support is required for the server to do the sort.</param>
        /// <returns>The set of message attributes required by this sort.</returns>
        public fCacheAttributes Attributes(out bool rDisplay)
        {
            if (Items == null) throw new InvalidOperationException();

            rDisplay = false;

            fCacheAttributes lAttributes = 0;

            foreach (var lItem in Items)
            {
                lAttributes |= lItem.Attribute;
                if (lItem.Item == eSortItem.displayfrom || lItem.Item == eSortItem.displayto) rDisplay = true;
            }

            return lAttributes;
        }

        private int ZCompareTo(uint? pX, uint? pY)
        {
            if (pX == null)
            {
                if (pY == null) return 0;
                return -1;
            }

            if (pY == null) return 1;

            return pX.Value.CompareTo(pY.Value);
        }

        private int ZCompareTo(DateTime? pX, DateTime? pY)
        {
            if (pX == null)
            {
                if (pY == null) return 0;
                return -1;
            }

            if (pY == null) return 1;

            return pX.Value.CompareTo(pY.Value);
        }

        private int ZCompareTo(string pX, string pY)
        {
            if (pX == null)
            {
                if (pY == null) return 0;
                return -1;
            }

            if (pY == null) return 1;

            return pX.CompareTo(pY);
        }

        public override string ToString()
        {
            if (mName != null) return $"{nameof(cSort)}({mName})";

            cListBuilder lBuilder = new cListBuilder(nameof(cSort));

            foreach (var lItem in Items)
            {
                if (lItem.Desc) lBuilder.Append(lItem.Item + " desc");
                else lBuilder.Append(lItem.Item);
            }

            return lBuilder.ToString();
        }
    }
}