using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private cMessageCacheItems mDefaultMessageCacheItems = cMessageCacheItems.None;
        private cSort mDefaultSort = cSort.None;

        /// <summary>
        /// Gets and sets the cache items that are fetched by default when message lists are generated.
        /// </summary>
        /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
        /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{support.iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
        public cMessageCacheItems DefaultMessageCacheItems
        {
            get => mDefaultMessageCacheItems;
            set => mDefaultMessageCacheItems = value ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Gets and sets the sort that is used by default when message lists are generated using <see cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>.
        /// </summary>
        public cSort DefaultSort
        {
            get => mDefaultSort;
            set => mDefaultSort = value ?? throw new ArgumentNullException();
        }
    }
}