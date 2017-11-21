using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an item that messages can be sorted by.
    /// </summary>
    /// <seealso cref="cSortItem"/>
    public enum eSortItem
    {
        /**<summary>The IMAP INTERNALDATE of the message.</summary>*/
        received,
        /**<summary>The group-name or local-part of the first 'CC' address.</summary>*/
        cc,
        /**<summary>The UTC normalised sent date of the message.</summary>*/
        sent,
        /**<summary>The group-name or local-part of the first 'from' address.</summary>*/
        from,
        /**<summary>The size of the message in bytes.</summary>*/
        size,
        /**<summary>The base subject. The base subject is defined in RFC 5256 and is the subject with the RE: FW: etc artifacts removed.</summary>*/
        subject,
        /**<summary>The group-name or local-part of the first 'to' address.</summary>*/
        to,
        /**<summary>The display-name of the first 'from' address. Defined in RFC 5957.</summary>*/
        displayfrom,
        /**<summary>The display-name of the first 'to' address. Defined in RFC 5957.</summary>*/
        displayto
    }

    /// <summary>
    /// Represents an item in a message sort specification.
    /// </summary>
    /// <seealso cref="cSort"/>
    public class cSortItem
    {
        /** <summary>Sort ascending by <see cref="eSortItem.received"/>.</summary> */
        public static readonly cSortItem Received = new cSortItem(eSortItem.received, false);
        /** <summary>Sort ascending by <see cref="eSortItem.cc"/>.</summary> */
        public static readonly cSortItem CC = new cSortItem(eSortItem.cc, false);
        /** <summary>Sort ascending by <see cref="eSortItem.sent"/>.</summary> */
        public static readonly cSortItem Sent = new cSortItem(eSortItem.sent, false);
        /** <summary>Sort ascending by <see cref="eSortItem.from"/>.</summary> */
        public static readonly cSortItem From = new cSortItem(eSortItem.from, false);
        /** <summary>Sort ascending by <see cref="eSortItem.size"/>.</summary> */
        public static readonly cSortItem Size = new cSortItem(eSortItem.size, false);
        /** <summary>Sort ascending by <see cref="eSortItem.subject"/>.</summary> */
        public static readonly cSortItem Subject = new cSortItem(eSortItem.subject, false);
        /** <summary>Sort ascending by <see cref="eSortItem.to"/>.</summary> */
        public static readonly cSortItem To = new cSortItem(eSortItem.to, false);
        /** <summary>Sort ascending by <see cref="eSortItem.displayfrom"/>.</summary> */
        public static readonly cSortItem DisplayFrom = new cSortItem(eSortItem.displayfrom, false);
        /** <summary>Sort ascending by <see cref="eSortItem.displayto"/>.</summary> */
        public static readonly cSortItem DisplayTo = new cSortItem(eSortItem.displayto, false);

        /** <summary>Sort descending by <see cref="eSortItem.received"/>.</summary> */
        public static readonly cSortItem ReceivedDesc = new cSortItem(eSortItem.received, true);
        /** <summary>Sort descending by <see cref="eSortItem.cc"/>.</summary> */
        public static readonly cSortItem CCDesc = new cSortItem(eSortItem.cc, true);
        /** <summary>Sort descending by <see cref="eSortItem.sent"/>.</summary> */
        public static readonly cSortItem SentDesc = new cSortItem(eSortItem.sent, true);
        /** <summary>Sort descending by <see cref="eSortItem.from"/>.</summary> */
        public static readonly cSortItem FromDesc = new cSortItem(eSortItem.from, true);
        /** <summary>Sort descending by <see cref="eSortItem.size"/>.</summary> */
        public static readonly cSortItem SizeDesc = new cSortItem(eSortItem.size, true);
        /** <summary>Sort descending by <see cref="eSortItem.subject"/>.</summary> */
        public static readonly cSortItem SubjectDesc = new cSortItem(eSortItem.subject, true);
        /** <summary>Sort descending by <see cref="eSortItem.to"/>.</summary> */
        public static readonly cSortItem ToDesc = new cSortItem(eSortItem.to, true);
        /** <summary>Sort descending by <see cref="eSortItem.displayfrom"/>.</summary> */
        public static readonly cSortItem DisplayFromDesc = new cSortItem(eSortItem.displayfrom, true);
        /** <summary>Sort descending by <see cref="eSortItem.displayto"/>.</summary> */
        public static readonly cSortItem DisplayToDesc = new cSortItem(eSortItem.displayto, true);

        /// <summary>
        /// The item being sorted by.
        /// </summary>
        public readonly eSortItem Item;

        /// <summary>
        /// The attribute that is required if the sorting is to be done client-side.
        /// </summary>
        public readonly fMessageCacheAttributes Attribute;

        /// <summary>
        /// Indicates a descending sort.
        /// </summary>
        public readonly bool Desc;

        /// <summary>
        /// Initialises a new instance with the specified item and sort direction.
        /// </summary>
        /// <param name="pItem"></param>
        /// <param name="pDesc">Indicates a descending sort.</param>
        public cSortItem(eSortItem pItem, bool pDesc)
        {
            Item = pItem;

            switch (pItem)
            {
                case eSortItem.received:

                    Attribute = fMessageCacheAttributes.received;
                    break;

                case eSortItem.cc:
                case eSortItem.sent:
                case eSortItem.from:
                case eSortItem.subject:
                case eSortItem.to:
                case eSortItem.displayfrom:
                case eSortItem.displayto:

                    Attribute = fMessageCacheAttributes.envelope;
                    break;

                case eSortItem.size:

                    Attribute = fMessageCacheAttributes.size;
                    break;

                default:

                    throw new ArgumentOutOfRangeException(nameof(pItem));
            }

            Desc = pDesc;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSortItem)}({Item},{Attribute},{Desc})";
    }

    /// <summary>
    /// Represents a message sort order.
    /// </summary>
    /// <remarks>
    /// You can use the <see langword="static"/> instances of <see cref="cSortItem"/> when creating message sort orders.
    /// For example;
    /// <code>
    /// var lSort = new cSort(cSortItem.Subject, cSortItem.Sent);
    /// </code>
    /// </remarks>
    /// <seealso cref="cIMAPClient.DefaultSort"/>
    /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
    public class cSort : IComparer<iMessageHandle>, IComparer<cMessage>
    {
        /// <summary>
        /// Specifies that no sorting is required.
        /// </summary>
        public static readonly cSort None = new cSort("none");

        // partial thread implementation removed - still to understand the responses and implement references algorithm
        //public static readonly cSort ThreadOrderedSubject = new cSort("thread:orderedsubject");
        //public static readonly cSort ThreadReferences = new cSort("thread:references");

        private readonly string mName;

        /// <summary>
        /// The items in this sort specification.
        /// </summary>
        public readonly ReadOnlyCollection<cSortItem> Items;

        private cSort(string pName)
        {
            mName = pName ?? throw new ArgumentNullException(nameof(pName));
            Items = null;
        }

        /// <summary>
        /// Initialises a new instance with the specified items.
        /// </summary>
        /// <param name="pItems"></param>
        public cSort(params cSortItem[] pItems)
        {
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));
            if (pItems.Length == 0) throw new ArgumentOutOfRangeException(nameof(pItems));

            foreach (var lItem in pItems) if (lItem == null) throw new ArgumentOutOfRangeException(nameof(pItems));

            mName = null;
            Items = new ReadOnlyCollection<cSortItem>(pItems);
        }

        /// <inheritdoc cref="cSort(cSortItem[])"/>
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

        /// <summary>
        /// Compares two messages using the comparision implied by the value of this instance.
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <returns></returns>
        /// <remarks>
        /// If the attributes required for the comparision are not in the message cache the result may be misleading.
        /// </remarks>
        /// <seealso cref="Attributes"/>
        /// <seealso cref="iMessageHandle.Attributes"/>
        /// <seealso cref="cMailbox.Messages(IEnumerable{iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
        public int Compare(iMessageHandle pX, iMessageHandle pY)
        {
            if (pX == null)
            {
                if (pY == null) return 0;
                return -1;
            }

            if (pY == null) return 1;

            if (Items != null)
            {
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
            }

            return pX.CacheSequence.CompareTo(pY.CacheSequence);
        }

        /// <summary>
        /// Compares two messages using the comparision implied by the value of this instance.
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <returns></returns>
        /// <remarks>
        /// If the attributes required for the comparision are not in the message cache they will be fetched from the server.
        /// </remarks>
        public int Compare(cMessage pX, cMessage pY)
        {
            if (pX == null)
            {
                if (pY == null) return 0;
                return -1;
            }

            if (pY == null) return 1;

            cMessageCacheItems lItems = Attributes(out _);
            pX.Fetch(lItems);
            pY.Fetch(lItems);
            return Compare(pX.Handle, pY.Handle);
        }

        /// <summary>
        /// Gets the client-side and server-side requirements for this sort order.
        /// </summary>
        /// <param name="rSortDisplay">Gets set to <see langword="true"/> if <see cref="cCapabilities.SortDisplay"/> must be in use for the server to do the sort.</param>
        /// <returns>The set of message attributes required for the comparison implied by the value of this instance.</returns>
        public fMessageCacheAttributes Attributes(out bool rSortDisplay)
        {
            rSortDisplay = false;

            fMessageCacheAttributes lAttributes = 0;

            if (Items != null)
            {
                foreach (var lItem in Items)
                {
                    lAttributes |= lItem.Attribute;
                    if (lItem.Item == eSortItem.displayfrom || lItem.Item == eSortItem.displayto) rSortDisplay = true;
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

        /// <inheritdoc />
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