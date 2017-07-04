using System;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cSortItem
    {
        public enum eType { received, cc, sent, from, size, subject, to, displayfrom, displayto }

        public static readonly cSortItem Received = new cSortItem(eType.received, false);
        public static readonly cSortItem CC = new cSortItem(eType.cc, false);
        public static readonly cSortItem Sent = new cSortItem(eType.sent, false);
        public static readonly cSortItem From = new cSortItem(eType.from, false);
        public static readonly cSortItem Size = new cSortItem(eType.size, false);
        public static readonly cSortItem Subject = new cSortItem(eType.subject, false);
        public static readonly cSortItem To = new cSortItem(eType.to, false);
        public static readonly cSortItem DisplayFrom = new cSortItem(eType.displayfrom, false);
        public static readonly cSortItem DisplayTo = new cSortItem(eType.displayto, false);

        public static readonly cSortItem ReceivedDesc = new cSortItem(eType.received, true);
        public static readonly cSortItem CCDesc = new cSortItem(eType.cc, true);
        public static readonly cSortItem SentDesc = new cSortItem(eType.sent, true);
        public static readonly cSortItem FromDesc = new cSortItem(eType.from, true);
        public static readonly cSortItem SizeDesc = new cSortItem(eType.size, true);
        public static readonly cSortItem SubjectDesc = new cSortItem(eType.subject, true);
        public static readonly cSortItem ToDesc = new cSortItem(eType.to, true);
        public static readonly cSortItem DisplayFromDesc = new cSortItem(eType.displayfrom, true);
        public static readonly cSortItem DisplayToDesc = new cSortItem(eType.displayto, true);

        public readonly eType Type;
        public readonly bool Desc;

        private cSortItem(eType pType, bool pDesc)
        {
            Type = pType;
            Desc = pDesc;
        }

        public override string ToString() => $"{nameof(cSortItem)}({Type},{Desc})";
    }

    public class cSort
    {
        public static readonly cSort OrderedSubject = new cSort("orderedsubject");
        public static readonly cSort References = new cSort("references");
        public static readonly cSort Refs = new cSort("refs");

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

                switch (lItem.Type)
                {
                    case cSortItem.eType.received:

                        lCompareTo = ZCompareTo(pX.Received, pY.Received);
                        break;

                    case cSortItem.eType.cc:

                        lCompareTo = ZCompareTo(pX.Envelope?.CC?.SortString, pY.Envelope?.CC?.SortString);
                        break;

                    case cSortItem.eType.sent:

                        // rfc 5256 says to use the internal date if the sent date is null
                        lCompareTo = ZCompareTo(pX.Envelope?.Sent ?? pX.Received, pY.Envelope?.Sent ?? pY.Received);
                        break;

                    case cSortItem.eType.from:

                        lCompareTo = ZCompareTo(pX.Envelope?.From?.SortString, pY.Envelope?.From?.SortString);
                        break;

                    case cSortItem.eType.size:

                        lCompareTo = ZCompareTo(pX.Size, pY.Size);
                        break;

                    case cSortItem.eType.subject:

                        lCompareTo = ZCompareTo(pX.Envelope?.BaseSubject, pY.Envelope?.BaseSubject);
                        break;

                    case cSortItem.eType.to:

                        lCompareTo = ZCompareTo(pX.Envelope?.To?.SortString, pY.Envelope?.To?.SortString);
                        break;

                    case cSortItem.eType.displayfrom:

                        lCompareTo = ZCompareTo(pX.Envelope?.From?.DisplaySortString, pY.Envelope?.From?.DisplaySortString);
                        break;

                    case cSortItem.eType.displayto:

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

        public int Comparison(cMessage pX, cMessage pY)
        {
            var lAttributes = Attributes(out _);
            pX.Fetch(lAttributes);
            pY.Fetch(lAttributes);
            return Comparison(pX.Handle, pY.Handle);
        }

        public fFetchAttributes Attributes(out bool rSortDisplay)
        {
            rSortDisplay = false;

            fFetchAttributes lAttributes = 0;

            foreach (var lItem in Items)
            {
                switch (lItem.Type)
                {
                    case cSortItem.eType.received:

                        lAttributes |= fFetchAttributes.received;
                        break;

                    case cSortItem.eType.cc:
                    case cSortItem.eType.from:
                    case cSortItem.eType.subject:
                    case cSortItem.eType.to:

                        lAttributes |= fFetchAttributes.envelope;
                        break;

                    case cSortItem.eType.sent:

                        lAttributes |= fFetchAttributes.envelope | fFetchAttributes.received;
                        break;

                    case cSortItem.eType.size:

                        lAttributes |= fFetchAttributes.size;
                        break;

                    case cSortItem.eType.displayfrom:
                    case cSortItem.eType.displayto:

                        lAttributes |= fFetchAttributes.envelope;
                        rSortDisplay = true;
                        break;

                    default:

                        throw new cInternalErrorException();
                }
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
            foreach (var lItem in Items) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }
    }
}