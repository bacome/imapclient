using System;

namespace work.bacome.imapclient.support
{
    public class cMailboxSelectedProperties
    {
        public static readonly cMailboxSelectedProperties NeverBeenSelected = new cMailboxSelectedProperties();

        private bool mBeenSelected;
        private bool mBeenSelectedForUpdate;
        private bool mBeenSelectedReadOnly;
        private cMessageFlags mMessageFlags;
        private cMessageFlags mForUpdatePermanentFlags;
        private cMessageFlags mReadOnlyPermanentFlags;

        private cMailboxSelectedProperties()
        {
            mBeenSelected = false;
            mBeenSelectedForUpdate = false;
            mBeenSelectedReadOnly = false;
            mMessageFlags = null;
            mForUpdatePermanentFlags = null;
            mReadOnlyPermanentFlags = null;
        }

        public cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, cMessageFlags pMessageFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags)
        {
            if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));
            if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));

            mBeenSelected = true;

            if (pSelectedForUpdate)
            {
                mBeenSelectedForUpdate = true;
                mBeenSelectedReadOnly = pSelectedProperties.mBeenSelectedReadOnly;
                mForUpdatePermanentFlags = pPermanentFlags;
                mReadOnlyPermanentFlags = pSelectedProperties.mReadOnlyPermanentFlags;
            }
            else
            {
                mBeenSelectedForUpdate = pSelectedProperties.mBeenSelectedForUpdate;
                mBeenSelectedReadOnly = true;
                mForUpdatePermanentFlags = pSelectedProperties.mForUpdatePermanentFlags;
                mReadOnlyPermanentFlags = pPermanentFlags;
            }

            mMessageFlags = pMessageFlags;
        }

        public cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, cMessageFlags pMessageFlags)
        {
            if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));
            if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));

            mBeenSelected = true;
            mBeenSelectedForUpdate = pSelectedProperties.mBeenSelectedForUpdate;
            mBeenSelectedReadOnly = pSelectedProperties.mBeenSelectedReadOnly;
            mMessageFlags = pMessageFlags;
            mForUpdatePermanentFlags = pSelectedProperties.mForUpdatePermanentFlags;
            mReadOnlyPermanentFlags = pSelectedProperties.mReadOnlyPermanentFlags;
        }

        public cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, bool pSelectedForUpdate, cMessageFlags pPermanentFlags)
        {
            if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));

            mBeenSelected = true;

            if (pSelectedForUpdate)
            {
                mBeenSelectedForUpdate = true;
                mBeenSelectedReadOnly = pSelectedProperties.mBeenSelectedReadOnly;
                mForUpdatePermanentFlags = pPermanentFlags;
                mReadOnlyPermanentFlags = pSelectedProperties.mReadOnlyPermanentFlags;
            }
            else
            {
                mBeenSelectedForUpdate = pSelectedProperties.mBeenSelectedForUpdate;
                mBeenSelectedReadOnly = true;
                mForUpdatePermanentFlags = pSelectedProperties.mForUpdatePermanentFlags;
                mReadOnlyPermanentFlags = pPermanentFlags;
            }

            mMessageFlags = pSelectedProperties.mMessageFlags;
        }

        public bool HasBeenSelected => mBeenSelected;
        public bool HasBeenSelectedForUpdate => mBeenSelectedForUpdate;
        public bool HasBeenSelectedReadOnly => mBeenSelectedReadOnly;
        public cMessageFlags MessageFlags => mMessageFlags;
        public cMessageFlags ForUpdatePermanentFlags => mForUpdatePermanentFlags ?? mMessageFlags;
        public cMessageFlags ReadOnlyPermanentFlags => mReadOnlyPermanentFlags ?? mMessageFlags;

        public override string ToString() => $"{nameof(cMailboxSelectedProperties)}({mBeenSelected},{mBeenSelectedForUpdate},{mBeenSelectedReadOnly},{mMessageFlags},{mForUpdatePermanentFlags},{mReadOnlyPermanentFlags})";

        public static fMailboxProperties Differences(cMailboxSelectedProperties pOld, cMailboxSelectedProperties pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            fMailboxProperties lProperties = 0;

            if (pOld.mBeenSelected != pNew.mBeenSelected) lProperties |= fMailboxProperties.hasbeenselected;
            if (pOld.mBeenSelectedForUpdate != pNew.mBeenSelectedForUpdate) lProperties |= fMailboxProperties.hasbeenselectedforupdate;
            if (pOld.mBeenSelectedReadOnly != pNew.mBeenSelectedReadOnly) lProperties |= fMailboxProperties.hasbeenselectedreadonly;
            if (pOld.mMessageFlags != pNew.mMessageFlags) lProperties |= fMailboxProperties.messageflags;
            if (pOld.ForUpdatePermanentFlags != pNew.ForUpdatePermanentFlags) lProperties |= fMailboxProperties.forupdatepermanentflags;
            if (pOld.ReadOnlyPermanentFlags != pNew.ReadOnlyPermanentFlags) lProperties |= fMailboxProperties.readonlypermanentflags;

            return lProperties;
        }
    }
}