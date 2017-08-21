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
using work.bacome.trace;

namespace testharness2
{
    public partial class frmMailboxes : Form
    {
        private const string kPleaseWait = "<please wait>";

        private readonly cTrace.cContext mRootContext;
        private readonly cIMAPClient mClient;
        private readonly bool mSubscriptions;

        public frmMailboxes(cIMAPClient pClient, bool pSubscriptions, cTrace.cContext pParentContext)
        {
            ;?; // new root object
            mRootContext = pParentContext.NewRootMethod(nameof(frmMailboxes), )
            mClient = pClient;
            mSubscriptions = pSubscriptions;
            InitializeComponent();
        }

        private void frmMailboxes_Load(object sender, EventArgs e)
        {
            if (mSubscriptions) Text = "imapclient testharness - subscriptions - " + mClient.InstanceName;
            else Text = "imapclient testharness - mailboxes - " + mClient.InstanceName;

            var lNamespaces = mClient.Namespaces;

            if (lNamespaces == null)
            {
                tvw.Enabled = false;
                return;
            }

            tvw.BeginUpdate();
            ZAddNamespaces("Personal", lNamespaces.Personal);
            ZAddNamespaces("Other Users", lNamespaces.OtherUsers);
            ZAddNamespaces("Shared", lNamespaces.Shared);
            tvw.EndUpdate();
        }

        private void ZAddNamespaces(string pClass, ReadOnlyCollection<cNamespace> pNamespaces)
        {
            var lNode = tvw.Nodes.Add(pClass);

            if (pNamespaces == null) return;

            if (pNamespaces.Count == 1)
            {
                lNode.Tag = new cNodeTag(pNamespaces[0], false, lNode.Nodes.Add(kPleaseWait));
                return;
            }

            foreach (var lNamespace in pNamespaces)
            {
                if (lNamespace.Prefix.Length == 0) lNode.Tag = new cNodeTag(lNamespace, false, null);
                else
                {
                    var lChildNode = lNode.Nodes.Add(lNamespace.Prefix); // should remove the trailing delimitier if there is one
                    lChildNode.Tag = new cNodeTag(lNamespace, false, lChildNode.Nodes.Add(kPleaseWait));
                }
            }
        }

        private void tvw_AfterExpand(object sender, TreeViewEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(Form1), nameof(tvwMailboxes_AfterExpand));

            if (!(e.Node.Tag is cTVWMailboxesNodeTag lTag)) return;

            if (lTag.State != cTVWMailboxesNodeTag.eState.neverexpanded) return;

            lTag.State = cTVWMailboxesNodeTag.eState.expanding;

            List<cMailboxListItem> lMailboxes;

            try
            {
                if (lTag.Namespace != null) lMailboxes = await lTag.Namespace.MailboxesAsync();
                else if (lTag.Mailbox != null) lMailboxes = await lTag.Mailbox.MailboxesAsync();
                else lMailboxes = null;
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
                return;
            }

            if (lMailboxes != null && lMailboxes.Count != 0)
            {
                foreach (var lListItem in lMailboxes)
                {
                    var lNode = e.Node.Nodes.Add(lListItem.Name);

                    TreeNode lPleaseWait;

                    if (lListItem.HasChildren != false) lPleaseWait = lNode.Nodes.Add(kTVWPleaseWait);
                    else lPleaseWait = null;

                    lNode.Tag = new cTVWMailboxesNodeTag(lListItem.Mailbox, lListItem.CanSelect ?? false, lPleaseWait);
                }
            }

            e.Node.Nodes.Remove(lTag.PleaseWait);

            lTag.State = cTVWMailboxesNodeTag.eState.expanded;
        }





        public class cNodeTag
        {
            public enum eState { neverexpanded, expanding, expanded }

            public readonly iChildMailboxes ChildMailboxes;
            public readonly bool CanSelect;
            public readonly TreeNode PleaseWait;

            // if it has been expanded then it has to be refreshed to get any new entries
            public eState State = eState.neverexpanded;

            public cNodeTag(iChildMailboxes pChildMailboxes, bool pCanSelect, TreeNode pPleaseWait)
            {
                ChildMailboxes = pChildMailboxes ?? throw new ArgumentNullException(nameof(pChildMailboxes));
                CanSelect = pCanSelect;
                PleaseWait = pPleaseWait;
            }
        }
    }
}
