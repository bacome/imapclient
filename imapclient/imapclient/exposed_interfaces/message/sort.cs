using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public enum eSortItem { received, cc, sent, from, size, subject, to, displayfrom, displayto }

    public class cSortItem
    {
        public static readonly cSortItem Received = new cSortItem(eSortItem.received, false);
        public static readonly cSortItem CC = new cSortItem(eSortItem.cc, false);
        public static readonly cSortItem Sent = new cSortItem(eSortItem.sent, false);
        public static readonly cSortItem From = new cSortItem(eSortItem.from, false);
        public static readonly cSortItem Size = new cSortItem(eSortItem.size, false);
        public static readonly cSortItem Subject = new cSortItem(eSortItem.subject, false);
        public static readonly cSortItem To = new cSortItem(eSortItem.to, false);
        public static readonly cSortItem DisplayFrom = new cSortItem(eSortItem.displayfrom, false);
        public static readonly cSortItem DisplayTo = new cSortItem(eSortItem.displayto, false);

        public static readonly cSortItem ReceivedDesc = new cSortItem(eSortItem.received, true);
        public static readonly cSortItem CCDesc = new cSortItem(eSortItem.cc, true);
        public static readonly cSortItem SentDesc = new cSortItem(eSortItem.sent, true);
        public static readonly cSortItem FromDesc = new cSortItem(eSortItem.from, true);
        public static readonly cSortItem SizeDesc = new cSortItem(eSortItem.size, true);
        public static readonly cSortItem SubjectDesc = new cSortItem(eSortItem.subject, true);
        public static readonly cSortItem ToDesc = new cSortItem(eSortItem.to, true);
        public static readonly cSortItem DisplayFromDesc = new cSortItem(eSortItem.displayfrom, true);
        public static readonly cSortItem DisplayToDesc = new cSortItem(eSortItem.displayto, true);

        public readonly eSortItem Item;
        public readonly fCacheAttributes Attribute;
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

    public class cSort : IComparer<iMessageHandle>, IComparer<cMessage>
    {
        public static readonly cSort None = new cSort("none");
        public static readonly cSort ThreadOrderedSubject = new cSort("thread:orderedsubject");
        public static readonly cSort ThreadReferences = new cSort("thread:references");

        private readonly string mName;
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