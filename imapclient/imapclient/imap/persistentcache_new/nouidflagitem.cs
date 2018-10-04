using System;

namespace work.bacome.imapclient
{
    internal class cNoUIDFlagItem : iFlagItem
    {
        private cModSeqFlags mModSeqFlags = null;

        public fMessageCacheAttributes Attributes
        {
            get
            {
                if (mModSeqFlags == null) return 0;
                return fMessageCacheAttributes.flags;
            }
        }

        public cModSeqFlags ModSeqFlags
        {
            get => mModSeqFlags;

            set
            {
                if (value == null) throw new ArgumentNullException();
                if (mModSeqFlags == null) mModSeqFlags = value;
            }
        }
    }
}