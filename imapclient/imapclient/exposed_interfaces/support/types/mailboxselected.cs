using System;

namespace work.bacome.imapclient.support
{
    public class cMailboxSelected
    {
        public static readonly cMailboxSelected New = new cMailboxSelected();

        private bool mSelected;
        private bool mSelectedForUpdate;
        private bool mAccessReadOnly;
        private bool mBeenSelected;
        private bool mBeenSelectedForUpdate;
        private bool mBeenSelectedReadOnly;
        private cMessageFlags mMessageFlags;
        private cMessageFlags mForUpdatePermanentFlags;
        private cMessageFlags mReadOnlyPermanentFlags;

        private cMailboxSelected()
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

        private cMailboxSelected(cMailboxSelected pSelected, bool pSelectedForUpdate, bool pAccessReadOnly, cMessageFlags pMessageFlags, cMessageFlags pPermanentFlags)
        {
            mSelected = true;
            mSelectedForUpdate = pSelectedForUpdate;
            mAccessReadOnly = pAccessReadOnly;
            mBeenSelected = true;

            if (mSelectedForUpdate)
            {
                mBeenSelectedForUpdate = true;
                mBeenSelectedReadOnly = pSelected.mBeenSelectedReadOnly;
                mForUpdatePermanentFlags = pPermanentFlags;
                mReadOnlyPermanentFlags = pSelected.mReadOnlyPermanentFlags;
            }
            else
            {
                mBeenSelectedForUpdate = pSelected.mBeenSelectedForUpdate;
                mBeenSelectedReadOnly = true;
                mForUpdatePermanentFlags = pSelected.mForUpdatePermanentFlags;
                mReadOnlyPermanentFlags = pPermanentFlags;
            }

            mMessageFlags = pMessageFlags;
        }

        private cMailboxSelected(cMailboxSelected pSelected)
        {
            if (pSelected == null) throw new ArgumentNullException(nameof(pSelected));

            mSelected = false;
            mSelectedForUpdate = false;
            mAccessReadOnly = false;
            mBeenSelected = pSelected.mBeenSelected;
            mBeenSelectedForUpdate = pSelected.mBeenSelectedForUpdate;
            mBeenSelectedReadOnly = pSelected.mBeenSelectedReadOnly;
            mMessageFlags = pSelected.mMessageFlags;
            mForUpdatePermanentFlags = pSelected.mForUpdatePermanentFlags;
            mReadOnlyPermanentFlags = pSelected.mReadOnlyPermanentFlags;
        }

        public cMailboxSelected Select(bool pForUpdate, bool pAccessReadOnly, cMessageFlags pMessageFlags, cMessageFlags pPermanentFlags)
        {
            if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
            if (mSelected) throw new InvalidOperationException();
            return new cMailboxSelected(this, pForUpdate, pAccessReadOnly, pMessageFlags, pPermanentFlags);
        }

        public cMailboxSelected Deselect()
        {
            if (!mSelected) throw new InvalidOperationException();
            return new cMailboxSelected(this);
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

        public override string ToString() => $"{nameof(cMailboxSelected)}({mSelected},{mSelectedForUpdate},{mAccessReadOnly},{mBeenSelected},{mBeenSelectedForUpdate},{mBeenSelectedReadOnly},{mMessageFlags},{mForUpdatePermanentFlags},{mReadOnlyPermanentFlags})";

        public static fMailboxProperties Differences(cMailboxSelected pOld, cMailboxSelected pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            fMailboxProperties lProperties = 0;

            if (pOld.mSelected != pNew.mSelected) lProperties |= fMailboxProperties.isselected;
            if (pOld.mSelectedForUpdate != pNew.mSelectedForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
            if (pOld.mAccessReadOnly != pNew.mAccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;
            if (pOld.mBeenSelected != pNew.mBeenSelected) lProperties |= fMailboxProperties.hasbeenselected;
            if (pOld.mBeenSelectedForUpdate != pNew.mBeenSelectedForUpdate) lProperties |= fMailboxProperties.hasbeenselectedforupdate;
            if (pOld.mBeenSelectedReadOnly != pNew.mBeenSelectedReadOnly) lProperties |= fMailboxProperties.hasbeenselectedreadonly;
            if (pOld.mMessageFlags != pNew.mMessageFlags) lProperties |= fMailboxProperties.messageflags;
            if (pOld.ForUpdatePermanentFlags != pNew.ForUpdatePermanentFlags) lProperties |= fMailboxProperties.forupdatepermanentflags;
            if (pOld.ReadOnlyPermanentFlags != pNew.ReadOnlyPermanentFlags) lProperties |= fMailboxProperties.readonlypermanentflags;

            if (lProperties != 0) lProperties |= fMailboxProperties.mailboxselected;

            return lProperties;
        }
    }
}