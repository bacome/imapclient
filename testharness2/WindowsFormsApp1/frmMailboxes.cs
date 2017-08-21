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
        private readonly fMailboxCacheDataSets mDataSets;
        private readonly bool mUseChildren;

        public frmMailboxes(cIMAPClient pClient, bool pSubscriptions, fMailboxCacheDataSets pDataSets, bool pUseChildren, cTrace.cContext pParentContext)
        {
            mRootContext = pParentContext.NewRootObject(nameof(frmMailboxes));
            mClient = pClient;
            mSubscriptions = pSubscriptions;
            mDataSets = pDataSets;
            mUseChildren = pUseChildren;
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

            ZAddNamespaces("Personal", lNamespaces.Personal);
            ZAddNamespaces("Other Users", lNamespaces.OtherUsers);
            ZAddNamespaces("Shared", lNamespaces.Shared);
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

        private async void tvw_AfterExpand(object sender, TreeViewEventArgs e)
        {
            var lContext = mRootContext.NewMethod(nameof(frmMailboxes), nameof(tvw_AfterExpand));

            if (!(e.Node.Tag is cNodeTag lTag)) return;

            if (lTag.State != cNodeTag.eState.neverexpanded) return;

            lTag.State = cNodeTag.eState.expanding;

            List<cMailbox> lMailboxes;

            try
            {
                if (mSubscriptions) lMailboxes = await lTag.ChildMailboxes.SubscribedAsync(false, mDataSets);
                else lMailboxes = await lTag.ChildMailboxes.MailboxesAsync(mDataSets);

                foreach (var lMailbox in lMailboxes)
                {
                    var lNode = e.Node.Nodes.Add(lMailbox.Name);

                    TreeNode lPleaseWait;

                    if (mUseChildren && lMailbox.HasChildren == false) lPleaseWait = null;
                    else lPleaseWait = lNode.Nodes.Add(kPleaseWait);

                    lNode.Tag = new cNodeTag(lMailbox, lMailbox.CanSelect, lPleaseWait);
                }
            }
            catch (Exception ex)
            {
                lContext.TraceException(ex);
                MessageBox.Show($"a problem occurred: {ex}");
            }

            e.Node.Nodes.Remove(lTag.PleaseWait);

            lTag.State = cNodeTag.eState.expanded;
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
