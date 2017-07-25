using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cMailboxSelected
    {
        public static readonly cMailboxSelected NotSelected = new cMailboxSelected();

        public readonly bool IsSelected;
        public readonly bool IsSelectedForUpdate;
        public readonly bool IsAccessReadOnly;

        private cMailboxSelected()
        {
            IsSelected = false;
            IsSelectedForUpdate = false;
            IsAccessReadOnly = false;
        }

        public cMailboxSelected(bool pSelectedForUpdate, bool pAccessReadOnly)
        {
            IsSelected = true;
            IsSelectedForUpdate = pSelectedForUpdate;
            IsAccessReadOnly = pAccessReadOnly;
        }

        public override string ToString() => $"{nameof(cMailboxSelected)}({IsSelected},{IsSelectedForUpdate},{IsAccessReadOnly})";

        public static fMailboxProperties Differences(cMailboxSelected pOld, cMailboxSelected pNew)
        {
            if (pOld == null) throw new ArgumentNullException(nameof(pOld));
            if (pNew == null) throw new ArgumentNullException(nameof(pNew));

            fMailboxProperties lProperties = 0;

            if (pOld.IsSelected != pNew.IsSelected) lProperties |= fMailboxProperties.isselected;
            if (pOld.IsSelectedForUpdate != pNew.IsSelectedForUpdate) lProperties |= fMailboxProperties.isselectedforupdate;
            if (pOld.IsAccessReadOnly != pNew.IsAccessReadOnly) lProperties |= fMailboxProperties.isaccessreadonly;

            if (lProperties != 0) lProperties |= fMailboxProperties.selected;

            return lProperties;
        }
    }
}