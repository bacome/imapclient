using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmMailboxDialog : Form
    {
        private const string kPleaseWait = "<please wait>";

        private readonly cIMAPClient mClient;
        private readonly bool mContainer;

        public frmMailboxDialog(cIMAPClient pClient, bool pContainer)
        {
            mClient = pClient;
            mContainer = pContainer;
            InitializeComponent();
        }

        public cMailbox Mailbox { get; private set; }
        public iMailboxContainer MailboxContainer { get; private set; }

        private void frmMailboxDialog_Load(object sender, EventArgs e)
        {
            if (mContainer) Text = "imapclient testharness - choose mailbox container - " + mClient.InstanceName;
            else Text = "imapclient testharness - choose mailbox - " + mClient.InstanceName;

            mClient.PropertyChanged += mClient_PropertyChanged;

            ZAddInbox();

            var lNamespaces = mClient.Namespaces;

            if (lNamespaces != null)
            {
                ZAddNamespaces("Personal", lNamespaces.Personal);
                ZAddNamespaces("Other Users", lNamespaces.OtherUsers);
                ZAddNamespaces("Shared", lNamespaces.Shared);
            }
        }

        private void ZAddInbox()
        {
            var lNode = tvw.Nodes.Add("Inbox");

            TreeNode lPleaseWait;

            if (mClient.Inbox.HasChildren == false) lPleaseWait = null;
            else lPleaseWait = lNode.Nodes.Add(kPleaseWait);

            lNode.Tag = new cNodeTag(mClient.Inbox, lPleaseWait);
        }

        private void ZAddNamespaces(string pClass, ReadOnlyCollection<cNamespace> pNamespaces)
        {
            var lNode = tvw.Nodes.Add(pClass);

            if (pNamespaces == null) return;

            if (pNamespaces.Count == 1)
            {
                lNode.Tag = new cNodeTag(pNamespaces[0], lNode.Nodes.Add(kPleaseWait));
                return;
            }

            foreach (var lNamespace in pNamespaces)
            {
                if (lNamespace.Prefix.Length == 0) lNode.Tag = new cNodeTag(lNamespace, null);
                else
                {
                    var lChildNode = lNode.Nodes.Add(lNamespace.Prefix); // should remove the trailing delimitier if there is one
                    lChildNode.Tag = new cNodeTag(lNamespace, lChildNode.Nodes.Add(kPleaseWait));
                }
            }
        }

        private void ZAddMailbox(TreeNode pNode, cMailbox pMailbox)
        {
            var lNode = pNode.Nodes.Add(pMailbox.Name);

            TreeNode lPleaseWait;

            if (pMailbox.HasChildren == false) lPleaseWait = null;
            else lPleaseWait = lNode.Nodes.Add(kPleaseWait);

            lNode.Tag = new cNodeTag(pMailbox, lPleaseWait);
        }

        private async void tvw_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (!(e.Node.Tag is cNodeTag lTag)) return;

            if (lTag.State != cNodeTag.eState.neverexpanded) return;

            lTag.State = cNodeTag.eState.expanding;

            List<cMailbox> lMailboxes;

            try
            {
                lMailboxes = await lTag.MailboxContainer.MailboxesAsync();
                if (IsDisposed) return;
                foreach (var lMailbox in lMailboxes) ZAddMailbox(e.Node, lMailbox);
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, $"a problem occurred: {ex}");
                return;
            }

            e.Node.Nodes.Remove(lTag.PleaseWait);

            lTag.State = cNodeTag.eState.expanded;
        }

        private void tvw_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!(e.Node.Tag is cNodeTag lTag))
            {
                cmdOK.Enabled = false;
                return;
            }

            if (mContainer)
            {
                cmdOK.Enabled = true;
                MailboxContainer = lTag.MailboxContainer;
            }
            else
            {
                if (lTag.Namespace != null)
                {
                    cmdOK.Enabled = false;
                    return;
                }

                bool lCanSelect;

                try { lCanSelect = lTag.Mailbox.CanSelect; }
                catch { lCanSelect = false; }

                cmdOK.Enabled = lCanSelect;

                if (lCanSelect) Mailbox = lTag.Mailbox;
                else Mailbox = null;
            }
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!mClient.IsConnected) DialogResult = DialogResult.Abort;
        }

        private void frmMailboxDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
        }

        public class cNodeTag
        {
            public enum eState { neverexpanded, expanding, expanded }

            public readonly cNamespace Namespace;
            public readonly cMailbox Mailbox;
            public readonly iMailboxContainer MailboxContainer;
            public readonly bool CanSelect;
            public readonly TreeNode PleaseWait;

            // if it has been expanded then it has to be refreshed to get any new entries
            public eState State = eState.neverexpanded;

            public cNodeTag(cNamespace pNamespace, TreeNode pPleaseWait)
            {
                Namespace = pNamespace ?? throw new ArgumentNullException(nameof(pNamespace));
                Mailbox = null;
                MailboxContainer = pNamespace;
                CanSelect = false;
                PleaseWait = pPleaseWait;
            }

            public cNodeTag(cMailbox pMailbox, TreeNode pPleaseWait)
            {
                Namespace = null;
                Mailbox = pMailbox ?? throw new ArgumentNullException(nameof(pMailbox));
                MailboxContainer = pMailbox;
                CanSelect = pMailbox.CanSelect;
                PleaseWait = pPleaseWait;
            }
        }
    }
}
