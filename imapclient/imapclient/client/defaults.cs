using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private cMessageCacheItems mDefaultMessageCacheItems = cMessageCacheItems.Empty;
        private cSort mDefaultSort = cSort.None;
        private cStorableFlags mDefaultAppendFlags = cStorableFlags.Empty;

        /// <summary>
        /// Gets and sets the items that are cached by default when message lists are generated.
        /// </summary>
        /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
        /// <seealso cref="cMailbox.Messages(System.Collections.Generic.IEnumerable{support.iMessageHandle}, cMessageCacheItems, cCacheItemFetchConfiguration)"/>
        public cMessageCacheItems DefaultMessageCacheItems
        {
            get => mDefaultMessageCacheItems;
            set => mDefaultMessageCacheItems = value ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Gets and sets the default message sort order.
        /// </summary>
        /// <seealso cref="cMailbox.Messages(cFilter, cSort, cMessageCacheItems, cMessageFetchConfiguration)"/>
        public cSort DefaultSort
        {
            get => mDefaultSort;
            set => mDefaultSort = value ?? throw new ArgumentNullException();
        }

        // TODO: see also

        /// <summary>
        /// Gets and sets the default flags used when appending messages. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// When the source of the appended data is <see cref="cMessageAppendData"/> the default flags come from the message being copied.
        /// </remarks>
        public cStorableFlags DefaultAppendFlags
        {
            get => mDefaultAppendFlags;

            set
            {
                var lContext = mRootContext.NewSetProp(nameof(cIMAPClient), nameof(DefaultAppendFlags), value);
                if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));
                mDefaultAppendFlags = value;
                mSession?.SetAppendDefaultFlags(value, lContext);
            }
        }
    }
}