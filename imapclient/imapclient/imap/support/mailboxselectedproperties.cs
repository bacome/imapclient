using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Contains some cached mailbox data. Intended for internal use.
    /// </summary>
    public class cMailboxSelectedProperties
    {
        internal static readonly cMailboxSelectedProperties NeverBeenSelected = new cMailboxSelectedProperties();

        private cFetchableFlags mMessageFlags;
        private bool mBeenSelected;
        private bool mBeenSelectedForUpdate;
        private bool mBeenSelectedReadOnly;
        private cPermanentFlags mForUpdatePermanentFlags;
        private cPermanentFlags mReadOnlyPermanentFlags;
        private bool? mUIDsAreSticky;

        private cMailboxSelectedProperties()
        {
            mBeenSelected = false;
            mBeenSelectedForUpdate = false;
            mBeenSelectedReadOnly = false;
            mMessageFlags = null;
            mForUpdatePermanentFlags = null;
            mReadOnlyPermanentFlags = null;
            mUIDsAreSticky = null;
        }

        internal cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, cFetchableFlags pMessageFlags, bool pForUpdate, cPermanentFlags pPermanentFlags, bool pUIDsAreSticky)
        {
            if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));

            mMessageFlags = pMessageFlags ?? throw new ArgumentNullException(nameof(pMessageFlags));

            mBeenSelected = true;

            if (pForUpdate)
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

            mUIDsAreSticky = pUIDsAreSticky;
        }

        internal cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, cFetchableFlags pMessageFlags)
        {
            if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));

            mMessageFlags = pMessageFlags ?? throw new ArgumentNullException(nameof(pMessageFlags));
            mBeenSelected = true;
            mBeenSelectedForUpdate = pSelectedProperties.mBeenSelectedForUpdate;
            mBeenSelectedReadOnly = pSelectedProperties.mBeenSelectedReadOnly;
            mForUpdatePermanentFlags = pSelectedProperties.mForUpdatePermanentFlags;
            mReadOnlyPermanentFlags = pSelectedProperties.mReadOnlyPermanentFlags;
            mUIDsAreSticky = pSelectedProperties.mUIDsAreSticky;
        }

        internal cMailboxSelectedProperties(cMailboxSelectedProperties pSelectedProperties, bool pForUpdate, cPermanentFlags pPermanentFlags)
        {
            if (pSelectedProperties == null) throw new ArgumentNullException(nameof(pSelectedProperties));

            mMessageFlags = pSelectedProperties.mMessageFlags;

            mBeenSelected = true;

            if (pForUpdate)
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

            mUIDsAreSticky = pSelectedProperties.mUIDsAreSticky;
        }

        internal cFetchableFlags MessageFlags => mMessageFlags;
        internal bool HasBeenSelected => mBeenSelected;
        internal bool HasBeenSelectedForUpdate => mBeenSelectedForUpdate;
        internal bool HasBeenSelectedReadOnly => mBeenSelectedReadOnly;
        internal cMessageFlags ForUpdatePermanentFlags => (cMessageFlags)mForUpdatePermanentFlags ?? mMessageFlags;
        internal cMessageFlags ReadOnlyPermanentFlags => (cMessageFlags)mReadOnlyPermanentFlags ?? mMessageFlags;
        internal bool? UIDsAreSticky => mUIDsAreSticky;

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxSelectedProperties)}({mMessageFlags},{mBeenSelected},{mBeenSelectedForUpdate},{mBeenSelectedReadOnly},{mForUpdatePermanentFlags},{mReadOnlyPermanentFlags},{mUIDsAreSticky})";

        internal static fMailboxProperties Differences(cMailboxSelectedProperties pOld, cMailboxSelectedProperties pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            fMailboxProperties lProperties = 0;

            if (pOld.mMessageFlags != pNew.mMessageFlags) lProperties |= fMailboxProperties.messageflags;
            if (pOld.mBeenSelected != pNew.mBeenSelected) lProperties |= fMailboxProperties.hasbeenselected;
            if (pOld.mBeenSelectedForUpdate != pNew.mBeenSelectedForUpdate) lProperties |= fMailboxProperties.hasbeenselectedforupdate;
            if (pOld.mBeenSelectedReadOnly != pNew.mBeenSelectedReadOnly) lProperties |= fMailboxProperties.hasbeenselectedreadonly;
            if (pOld.ForUpdatePermanentFlags != pNew.ForUpdatePermanentFlags) lProperties |= fMailboxProperties.forupdatepermanentflags;
            if (pOld.ReadOnlyPermanentFlags != pNew.ReadOnlyPermanentFlags) lProperties |= fMailboxProperties.readonlypermanentflags;
            if (pOld.mUIDsAreSticky != pNew.mUIDsAreSticky) lProperties |= fMailboxProperties.uidsaresticky;

            return lProperties;
        }
    }
}