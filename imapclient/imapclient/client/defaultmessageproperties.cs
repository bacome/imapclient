using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private cCacheItems mDefaultCacheItems = cCacheItems.None;

        public cCacheItems DefaultCacheItems
        {
            get => mDefaultCacheItems;
            set => mDefaultCacheItems = value ?? throw new ArgumentNullException();
        }
    }
}