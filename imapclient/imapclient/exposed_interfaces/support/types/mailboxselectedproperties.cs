using System;

namespace work.bacome.imapclient.support
{
    public class cMailboxSelectedProperties
    {
        public static readonly cMailboxSelectedProperties NeverBeenSelected = new cMailboxSelectedProperties();

        private bool mBeenSelected;
        private bool mBeenSelectedForUpdate;
        private bool mBeenSelectedReadOnly;
        private bool? mUIDNotSticky;
        private cFetchableFlags mMessageFlags;
        private cPermanentFlags mForUpdatePermanentFlags;
        private cPermanentFlags mReadOnlyPermanentFlags;

        private cMailboxSelectedProperties()
        {
            mBeenSelected = false;
            mBeenSelectedForUpdate = false;
            mBeenSelectedReadOnly = false;
            mUIDNotSticky = null;
            mMessageFlags = null;
            mForUpdatePermanentFlags = null;
            mReadOnlyPermanentFlags = null;
        }

        public cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, bool pUIDNotSticky, cFetchableFlags pMessageFlags, bool pSelectedForUpdate, cPermanentFlags pPermanentFlags)
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

            mUIDNotSticky = pUIDNotSticky;
            mMessageFlags = pMessageFlags;
        }

        public cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, cFetchableFlags pMessageFlags)
        {
            if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));
            if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));

            mBeenSelected = true;
            mBeenSelectedForUpdate = pSelectedProperties.mBeenSelectedForUpdate;
            mBeenSelectedReadOnly = pSelectedProperties.mBeenSelectedReadOnly;
            mUIDNotSticky = pSelectedProperties.mUIDNotSticky;
            mMessageFlags = pMessageFlags;
            mForUpdatePermanentFlags = pSelectedProperties.mForUpdatePermanentFlags;
            mReadOnlyPermanentFlags = pSelectedProperties.mReadOnlyPermanentFlags;
        }

        public cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, bool pSelectedForUpdate, cPermanentFlags pPermanentFlags)
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

            mUIDNotSticky = pSelectedProperties.mUIDNotSticky;
            mMessageFlags = pSelectedProperties.mMessageFlags;
        }

        public bool HasBeenSelected => mBeenSelected;
        public bool HasBeenSelectedForUpdate => mBeenSelectedForUpdate;
        public bool HasBeenSelectedReadOnly => mBeenSelectedReadOnly;
        public bool? UIDNotSticky => mUIDNotSticky;
        public cFetchableFlags MessageFlags => mMessageFlags;
        public cMessageFlags ForUpdatePermanentFlags => (cMessageFlags)mForUpdatePermanentFlags ?? mMessageFlags;
        public cMessageFlags ReadOnlyPermanentFlags => (cMessageFlags)mReadOnlyPermanentFlags ?? mMessageFlags;

        public override string ToString() => $"{nameof(cMailboxSelectedProperties)}({mBeenSelected},{mBeenSelectedForUpdate},{mBeenSelectedReadOnly},{mUIDNotSticky},{mMessageFlags},{mForUpdatePermanentFlags},{mReadOnlyPermanentFlags})";

        public static fMailboxProperties Differences(cMailboxSelectedProperties pOld, cMailboxSelectedProperties pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            fMailboxProperties lProperties = 0;

            if (pOld.mBeenSelected != pNew.mBeenSelected) lProperties |= fMailboxProperties.hasbeenselected;
            if (pOld.mBeenSelectedForUpdate != pNew.mBeenSelectedForUpdate) lProperties |= fMailboxProperties.hasbeenselectedforupdate;
            if (pOld.mBeenSelectedReadOnly != pNew.mBeenSelectedReadOnly) lProperties |= fMailboxProperties.hasbeenselectedreadonly;
            if (pOld.mUIDNotSticky != pNew.mUIDNotSticky) lProperties |= fMailboxProperties.uidnotsticky;
            if (pOld.mMessageFlags != pNew.mMessageFlags) lProperties |= fMailboxProperties.messageflags;
            if (pOld.ForUpdatePermanentFlags != pNew.ForUpdatePermanentFlags) lProperties |= fMailboxProperties.forupdatepermanentflags;
            if (pOld.ReadOnlyPermanentFlags != pNew.ReadOnlyPermanentFlags) lProperties |= fMailboxProperties.readonlypermanentflags;

            return lProperties;
        }
    }
}