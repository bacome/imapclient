using System;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cSortItem
    {
        public static readonly cSortItem Received = new cSortItem(fMessageProperties.received, false, false);
        public static readonly cSortItem CC = new cSortItem(fMessageProperties.cc, false, false);
        public static readonly cSortItem Sent = new cSortItem(fMessageProperties.sent, false, false);
        public static readonly cSortItem From = new cSortItem(fMessageProperties.from, false, false);
        public static readonly cSortItem Size = new cSortItem(fMessageProperties.size, false, false);
        public static readonly cSortItem Subject = new cSortItem(fMessageProperties.subject, false, false);
        public static readonly cSortItem To = new cSortItem(fMessageProperties.to, false, false);
        public static readonly cSortItem DisplayFrom = new cSortItem(fMessageProperties.from, true, false);
        public static readonly cSortItem DisplayTo = new cSortItem(fMessageProperties.to, true, false);

        public static readonly cSortItem ReceivedDesc = new cSortItem(fMessageProperties.received, false, true);
        public static readonly cSortItem CCDesc = new cSortItem(fMessageProperties.cc, false, true);
        public static readonly cSortItem SentDesc = new cSortItem(fMessageProperties.sent, false, true);
        public static readonly cSortItem FromDesc = new cSortItem(fMessageProperties.from, false, true);
        public static readonly cSortItem SizeDesc = new cSortItem(fMessageProperties.size, false, true);
        public static readonly cSortItem SubjectDesc = new cSortItem(fMessageProperties.subject, false, true);
        public static readonly cSortItem ToDesc = new cSortItem(fMessageProperties.to, false, true);
        public static readonly cSortItem DisplayFromDesc = new cSortItem(fMessageProperties.from, true, true);
        public static readonly cSortItem DisplayToDesc = new cSortItem(fMessageProperties.to, true, true);

        public readonly fMessageProperties Property;
        public readonly bool Display;
        public readonly bool Desc;

        private cSortItem(fMessageProperties pProperty, bool pDisplay, bool pDesc)
        {
            Property = pProperty;
            Display = pDisplay;
            Desc = pDesc;
        }

        public override string ToString() => $"{nameof(cSortItem)}({Property},{Display},{Desc})";
    }

    public class cSort
    {
        public static readonly cSort None = new cSort("none");
        public static readonly cSort ClientDefault = new cSort("clientdefault");
        public static readonly cSort OrderedSubject = new cSort("orderedsubject");
        public static readonly cSort References = new cSort("references");

        private readonly string mName;
        public readonly ReadOnlyCollection<cSortItem> Items;

        private cSort(string pName)
        {
            mName = pName;
            Items = null;
        }

        public cSort(params cSortItem[] pItems)
        {
            mName = null;
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));
            if (pItems.Length == 0) throw new ArgumentOutOfRangeException(nameof(pItems));
            foreach (var lItem in pItems) if (lItem == null) throw new ArgumentOutOfRangeException(nameof(pItems));
            Items = new ReadOnlyCollection<cSortItem>(pItems);
        }

        public int Comparison(iMessageHandle pX, iMessageHandle pY)
        {
            foreach (var lItem in Items)
            {
                int lCompareTo;

                switch (lItem.Property)
                {
                    case fMessageProperties.received:

                        lCompareTo = ZCompareTo(pX.Received, pY.Received);
                        break;

                    case fMessageProperties.cc:

                        lCompareTo = ZCompareTo(pX.Envelope?.CC?.SortString, pY.Envelope?.CC?.SortString);
                        break;

                    case fMessageProperties.sent:

                        // rfc 5256 says to use the internal date if the sent date is null
                        lCompareTo = ZCompareTo(pX.Envelope?.Sent ?? pX.Received, pY.Envelope?.Sent ?? pY.Received);
                        break;

                    case fMessageProperties.from:

                        if (lItem.Display) lCompareTo = ZCompareTo(pX.Envelope?.From?.DisplaySortString, pY.Envelope?.From?.DisplaySortString);
                        else lCompareTo = ZCompareTo(pX.Envelope?.From?.SortString, pY.Envelope?.From?.SortString);
                        break;

                    case fMessageProperties.size:

                        lCompareTo = ZCompareTo(pX.Size, pY.Size);
                        break;

                    case fMessageProperties.subject:

                        lCompareTo = ZCompareTo(pX.Envelope?.BaseSubject, pY.Envelope?.BaseSubject);
                        break;

                    case fMessageProperties.to:

                        if (lItem.Display) lCompareTo = ZCompareTo(pX.Envelope?.To?.DisplaySortString, pY.Envelope?.To?.DisplaySortString);
                        else lCompareTo = ZCompareTo(pX.Envelope?.To?.SortString, pY.Envelope?.To?.SortString);
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

        public int Comparison(cMessage pX, cMessage pY)
        {
            var lProperties = Properties(out _);
            pX.Fetch(lProperties);
            pY.Fetch(lProperties);
            return Comparison(pX.Handle, pY.Handle);
        }

        public fMessageProperties Properties(out bool rDisplay)
        {
            rDisplay = false;

            fMessageProperties lProperties = 0;

            foreach (var lItem in Items)
            {
                lProperties |= lItem.Property;
                if (lItem.Display) rDisplay = true;
            }

            return lProperties;
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
            foreach (var lItem in Items) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }
    }
}