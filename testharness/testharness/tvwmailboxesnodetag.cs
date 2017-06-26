using System;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness
{
    public class cTVWMailboxesNodeTag
    {
        public enum eState { neverexpanded, expanding, expanded }

        public readonly cNamespace Namespace;
        public readonly cMailbox Mailbox;
        public readonly bool CanSelect;
        public readonly TreeNode PleaseWait;

        // if it has been expanded then it has to be refreshed to get any new entries
        public eState State = eState.neverexpanded;

        public cTVWMailboxesNodeTag(cNamespace pNamespace, TreeNode pPleaseWait)
        {
            Namespace = pNamespace ?? throw new ArgumentNullException(nameof(pNamespace));
            Mailbox = null;
            CanSelect = false;
            PleaseWait = pPleaseWait;
        }

        public cTVWMailboxesNodeTag(cMailbox pMailbox, bool pCanSelect, TreeNode pPleaseWait)
        {
            Namespace = null;
            Mailbox = pMailbox ?? throw new ArgumentNullException(nameof(pMailbox));
            CanSelect = pCanSelect;
            PleaseWait = pPleaseWait;
        }
    }
}