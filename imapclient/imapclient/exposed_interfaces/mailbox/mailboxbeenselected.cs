using System;

namespace work.bacome.imapclient
{
    public class cMailboxBeenSelected
    {
        public static readonly cMailboxBeenSelected No = new cMailboxBeenSelected();

        private bool mBeenSelected;
        private bool mBeenSelectedForUpdate;
        private bool mBeenSelectedReadOnly;
        private cMessageFlags mMessageFlags;
        private cMessageFlags mForUpdatePermanentFlags;
        private cMessageFlags mReadOnlyPermanentFlags;

        private cMailboxBeenSelected()
        {
            mBeenSelected = false;
            mBeenSelectedForUpdate = false;
            mBeenSelectedReadOnly = false;
            mMessageFlags = null;
            mForUpdatePermanentFlags = null;
            mReadOnlyPermanentFlags = null;
        }

        private cMailboxBeenSelected(cMailboxBeenSelected pHasBeenSelected, cMessageFlags pMessageFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags)
        {
            mBeenSelected = true;

            if (pSelectedForUpdate)
            {
                mBeenSelectedForUpdate = true;
                mBeenSelectedReadOnly = pHasBeenSelected.mBeenSelectedReadOnly;
                mForUpdatePermanentFlags = pPermanentFlags;
                mReadOnlyPermanentFlags = pHasBeenSelected.mReadOnlyPermanentFlags;
            }
            else
            {
                mBeenSelectedForUpdate = pHasBeenSelected.mBeenSelectedForUpdate;
                mBeenSelectedReadOnly = true;
                mForUpdatePermanentFlags = pHasBeenSelected.mForUpdatePermanentFlags;
                mReadOnlyPermanentFlags = pPermanentFlags;
            }

            mMessageFlags = pMessageFlags;
        }

        public cMailboxBeenSelected Update(cMessageFlags pMessageFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags)
        {
            if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
            return new cMailboxBeenSelected(this, pMessageFlags, pSelectedForUpdate, pPermanentFlags);
        }

        public bool HasBeenSelected => mBeenSelected;
        public bool HasBeenSelectedForUpdate => mBeenSelectedForUpdate;
        public bool HasBeenSelectedReadOnly => mBeenSelectedReadOnly;
        public cMessageFlags MessageFlags => mMessageFlags;
        public cMessageFlags ForUpdatePermanentFlags => mForUpdatePermanentFlags ?? mMessageFlags;
        public cMessageFlags ReadOnlyPermanentFlags => mReadOnlyPermanentFlags ?? mMessageFlags;

        public override string ToString() => $"{nameof(cMailboxBeenSelected)}({mBeenSelected},{mBeenSelectedForUpdate},{mBeenSelectedReadOnly},{mMessageFlags},{mForUpdatePermanentFlags},{mReadOnlyPermanentFlags})";

        public static fMailboxProperties Differences(cMailboxBeenSelected pOld, cMailboxBeenSelected pNew)
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

            if (lProperties != 0) lProperties |= fMailboxProperties.mailboxbeenselected;

            return lProperties;
        }
    }
}