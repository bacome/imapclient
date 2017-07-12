using System;

namespace work.bacome.imapclient.support
{
    public class cSelectedMailboxProperties
    {
        public enum fProperties
        {
            isselected = 1 << 0,
            isselectedforupdate = 1 << 1,
            isaccessreadonly = 1 << 2,
            hasbeenselected = 1 << 3,
            hasbeenselectedforupdate = 1 << 5,
            hasbeenselectedreadonly = 1 << 7,
            messageflags = 1 << 4,
            forupdatepermanentflags = 1 << 6,
            readonlypermanentflags = 1 << 8
        }

        public static readonly cSelectedMailboxProperties NonExistent = new cSelectedMailboxProperties();

        private bool mSelected;
        private bool mSelectedForUpdate;
        private bool mAccessReadOnly;
        private bool mBeenSelected;
        private bool mBeenSelectedForUpdate;
        private bool mBeenSelectedReadOnly;
        private cMessageFlags mMessageFlags;
        private cMessageFlags mForUpdatePermanentFlags;
        private cMessageFlags mReadOnlyPermanentFlags;

        private cSelectedMailboxProperties()
        {
            mSelected = false;
            mSelectedForUpdate = false;
            mAccessReadOnly = false;
            mBeenSelected = false;
            mBeenSelectedForUpdate = false;
            mBeenSelectedReadOnly = false;
            mMessageFlags = null;
            mForUpdatePermanentFlags = null;
            mReadOnlyPermanentFlags = null;
        }

        public cSelectedMailboxProperties(cSelectedMailboxProperties pOldProperties, bool pSelectedForUpdate, bool pAccessReadOnly, cMessageFlags pMessageFlags, cMessageFlags pPermanentFlags)
        {
            if (pOldProperties == null) throw new ArgumentNullException(nameof(pOldProperties));
            if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));

            mSelected = true;
            mSelectedForUpdate = pSelectedForUpdate;
            mAccessReadOnly = pAccessReadOnly;
            mBeenSelected = true;

            if (mSelectedForUpdate)
            {
                mBeenSelectedForUpdate = true;
                mBeenSelectedReadOnly = pOldProperties.mBeenSelectedReadOnly;
                mForUpdatePermanentFlags = pPermanentFlags;
                mReadOnlyPermanentFlags = pOldProperties.mReadOnlyPermanentFlags;
            }
            else
            {
                mBeenSelectedForUpdate = pOldProperties.mBeenSelectedForUpdate;
                mBeenSelectedReadOnly = true;
                mForUpdatePermanentFlags = pOldProperties.mForUpdatePermanentFlags;
                mReadOnlyPermanentFlags = pPermanentFlags;
            }

            mMessageFlags = pMessageFlags;
        }

        public cSelectedMailboxProperties(cSelectedMailboxProperties pOldProperties)
        {
            if (pOldProperties == null) throw new ArgumentNullException(nameof(pOldProperties));

            mSelected = false;
            mSelectedForUpdate = false;
            mAccessReadOnly = false;
            mBeenSelected = pOldProperties.mBeenSelected;
            mBeenSelectedForUpdate = pOldProperties.mBeenSelectedForUpdate;
            mBeenSelectedReadOnly = pOldProperties.mBeenSelectedReadOnly;
            mMessageFlags = pOldProperties.mMessageFlags;
            mForUpdatePermanentFlags = pOldProperties.mForUpdatePermanentFlags;
            mReadOnlyPermanentFlags = pOldProperties.mReadOnlyPermanentFlags;
        }

        public bool IsSelected => mSelected;
        public bool IsSelectedForUpdate => mSelectedForUpdate;
        public bool IsAccessReadOnly => mAccessReadOnly;
        public bool HasBeenSelected => mBeenSelected;
        public bool HasBeenSelectedForUpdate => mBeenSelectedForUpdate;
        public bool HasBeenSelectedReadOnly => mBeenSelectedReadOnly;
        public cMessageFlags MessageFlags => mMessageFlags;
        public cMessageFlags ForUpdatePermanentFlags => mForUpdatePermanentFlags ?? mMessageFlags;
        public cMessageFlags ReadOnlyPermanentFlags => mReadOnlyPermanentFlags ?? mMessageFlags;

        public override string ToString() => $"{nameof(cSelectedMailboxProperties)}({mSelected},{mSelectedForUpdate},{mAccessReadOnly},{mBeenSelected},{mBeenSelectedForUpdate},{mBeenSelectedReadOnly},{mMessageFlags},{mForUpdatePermanentFlags},{mReadOnlyPermanentFlags})";

        public static fProperties Differences(cSelectedMailboxProperties pOld, cSelectedMailboxProperties pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            if (ReferenceEquals(pOld, NonExistent)) return 0;

            fProperties lProperties = 0;

            if (pOld.mSelected != pNew.mSelected) lProperties |= fProperties.isselected;
            if (pOld.mSelectedForUpdate != pNew.mSelectedForUpdate) lProperties |= fProperties.isselectedforupdate;
            if (pOld.mAccessReadOnly != pNew.mAccessReadOnly) lProperties |= fProperties.isaccessreadonly;
            if (pOld.mBeenSelected != pNew.mBeenSelected) lProperties |= fProperties.hasbeenselected;
            if (pOld.mBeenSelectedForUpdate != pNew.mBeenSelectedForUpdate) lProperties |= fProperties.hasbeenselectedforupdate;
            if (pOld.mBeenSelectedReadOnly != pNew.mBeenSelectedReadOnly) lProperties |= fProperties.hasbeenselectedreadonly;
            if (pOld.mMessageFlags != pNew.mMessageFlags) lProperties |= fProperties.messageflags;
            if (pOld.ForUpdatePermanentFlags != pNew.ForUpdatePermanentFlags) lProperties |= fProperties.forupdatepermanentflags;
            if (pOld.ReadOnlyPermanentFlags != pNew.ReadOnlyPermanentFlags) lProperties |= fProperties.readonlypermanentflags;

            return lProperties;
        }
    }
}