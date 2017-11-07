using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private cCacheItems mDefaultCacheItems = cCacheItems.None;
        private cSort mDefaultSort = cSort.None;

        /// <summary>
        /// Specifies the cache items that are fetched by default when message lists are generated using <see cref="cMailbox.Messages(cFilter, cSort, cCacheItems, cMessageFetchConfiguration)"/>.
        /// </summary>
        public cCacheItems DefaultCacheItems
        {
            get => mDefaultCacheItems;
            set => mDefaultCacheItems = value ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Specifies the sort that is used by default when message lists are generated using <see cref="cMailbox.Messages(cFilter, cSort, cCacheItems, cMessageFetchConfiguration)"/>.
        /// </summary>
        public cSort DefaultSort
        {
            get => mDefaultSort;
            set => mDefaultSort = value ?? throw new ArgumentNullException();
        }
    }
}