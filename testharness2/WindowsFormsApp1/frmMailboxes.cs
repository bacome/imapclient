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
        private iMailboxParent mCurrentParent = null;
        private cMailbox mSubscribedMailbox = null;

        public frmMailboxes(cIMAPClient pClient, bool pSubscriptions, fMailboxCacheDataSets pDataSets, cTrace.cContext pParentContext)
        {
            mRootContext = pParentContext.NewRootObject(nameof(frmMailboxes));
            mClient = pClient;
            mSubscriptions = pSubscriptions;
            mDataSets = pDataSets;
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

                    if (lMailbox.HasChildren == false) lPleaseWait = null;
                    else lPleaseWait = lNode.Nodes.Add(kPleaseWait);

                    lNode.Tag = new cNodeTag(lMailbox, lPleaseWait);
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

        private void tvw_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ZUnsubscribeMailbox();

            rtx.Clear();

            if (!(e.Node.Tag is cNodeTag lTag))
            {
                mCurrentParent = null;
                gbxMailbox.Enabled = false;
                gbxCreate.Enabled = false;
                return;
            }

            mCurrentParent = lTag.ChildMailboxes;

            if (lTag.Namespace == null)
            {
                ZSubscribeMailbox(lTag.Mailbox);
                gbxMailbox.Enabled = true;
                ZSubscribedMailboxDisplay();
            }
            else 
            {
                StringBuilder lBuilder = new StringBuilder();
                lBuilder.AppendLine("Prefix: '" + lTag.Namespace.Prefix + "'");
                lBuilder.AppendLine("Delimiter: '" + lTag.Namespace.Delimiter + "'");
                // TODO more here once we get the namespace translations ...
                rtx.Text = lBuilder.ToString();

                gbxMailbox.Enabled = false;
                gbxCreate.Enabled = true;
            }
        }

        private void ZSubscribeMailbox(cMailbox pMailbox)
        {
            mSubscribedMailbox = pMailbox;
            mSubscribedMailbox.MessageDelivery += ZSubscribedMailboxMessageDelivery;
            mSubscribedMailbox.PropertyChanged += ZSubscribedMailboxPropertyChanged;
        }

        private void ZUnsubscribeMailbox()
        {
            if (mSubscribedMailbox == null) return;
            mSubscribedMailbox.MessageDelivery -= ZSubscribedMailboxMessageDelivery;
            mSubscribedMailbox.PropertyChanged -= ZSubscribedMailboxPropertyChanged;
            mSubscribedMailbox = null;
        }

        private void ZSubscribedMailboxPropertyChanged(object sender, PropertyChangedEventArgs e) => ZSubscribedMailboxDisplay();

        private void ZSubscribedMailboxMessageDelivery(object sender, cMessageDeliveryEventArgs e) => ZSubscribedMailboxDisplay();

        private void ZSubscribedMailboxDisplay()
        {
            StringBuilder lBuilder = new StringBuilder();

            try
            {
                lBuilder.AppendLine("Name: '" + mSubscribedMailbox.Name + "'");
                lBuilder.AppendLine("Delimiter: '" + mSubscribedMailbox.Delimiter + "'");
                lBuilder.AppendLine("Exists: " + mSubscribedMailbox.Exists);
                lBuilder.AppendLine("Marked: " + mSubscribedMailbox.IsMarked);
                lBuilder.AppendLine("Remote: " + mSubscribedMailbox.IsRemote);
                lBuilder.AppendLine("HasChildren: " + mSubscribedMailbox.HasChildren);
                lBuilder.AppendLine();

                if (mSubscribedMailbox.ContainsAll == true) lBuilder.AppendLine("Contains All");
                if (mSubscribedMailbox.IsArchive == true) lBuilder.AppendLine("Is Archive");
                if (mSubscribedMailbox.ContainsDrafts == true) lBuilder.AppendLine("Contains Drafts");
                if (mSubscribedMailbox.ContainsFlagged == true) lBuilder.AppendLine("Contains Flagged");
                if (mSubscribedMailbox.ContainsJunk == true) lBuilder.AppendLine("Contains Junk");
                if (mSubscribedMailbox.ContainsSent == true) lBuilder.AppendLine("Contains Sent");
                if (mSubscribedMailbox.ContainsTrash == true) lBuilder.AppendLine("Contains Trash");
                lBuilder.AppendLine();

                if (mSubscribedMailbox.MessageCount != null) lBuilder.AppendLine("Messages: " + mSubscribedMailbox.MessageCount);

                if (mSubscribedMailbox.RecentCount != null) lBuilder.AppendLine("Recent: " + mSubscribedMailbox.RecentCount);

                if (mSubscribedMailbox.UIDNext != null)
                {
                    lBuilder.AppendLine("UIDNext: " + mSubscribedMailbox.UIDNext);
                    lBuilder.AppendLine("UIDNextUnknownCount: " + mSubscribedMailbox.UIDNextUnknownCount);
                }

                if (mSubscribedMailbox.UIDValidity != null) lBuilder.AppendLine("UIDValidity: " + mSubscribedMailbox.UIDValidity);

                if (mSubscribedMailbox.UnseenCount != null)
                {
                    lBuilder.AppendLine("Unseen: " + mSubscribedMailbox.UnseenCount);
                    lBuilder.AppendLine("UnseenUnknownCount: " + mSubscribedMailbox.UnseenUnknownCount);
                }

                if (mSubscribedMailbox.HighestModSeq != null) lBuilder.AppendLine("HighestModSeq: " + mSubscribedMailbox.HighestModSeq);

                lBuilder.AppendLine();

                if (mSubscribedMailbox.MessageFlags != null)
                {
                    lBuilder.AppendLine("Flags: " + mSubscribedMailbox.MessageFlags.ToString());
                    lBuilder.AppendLine();

                    if (mSubscribedMailbox.ForUpdatePermanentFlags != null)
                    {
                        lBuilder.AppendLine("PermanentFlags: " + mSubscribedMailbox.ForUpdatePermanentFlags.ToString());
                        lBuilder.AppendLine();
                    }

                    if (mSubscribedMailbox.ReadOnlyPermanentFlags != null && mSubscribedMailbox.ReadOnlyPermanentFlags.Count != 0)
                    {
                        lBuilder.AppendLine("PermanentFlags (read only): " + mSubscribedMailbox.ReadOnlyPermanentFlags.ToString());
                        lBuilder.AppendLine();
                    }
                }

                if (mSubscribedMailbox.IsSelected)
                {
                    if (mSubscribedMailbox.IsSelectedForUpdate) lBuilder.AppendLine("Selected for update");
                    else lBuilder.AppendLine("Selected readonly");

                    if (mSubscribedMailbox.IsAccessReadOnly) lBuilder.AppendLine("Read only access");
                    else lBuilder.AppendLine("Read write access");

                    lBuilder.AppendLine();
                }

                cmdExamine.Enabled = mSubscribedMailbox.CanSelect;
                cmdSelect.Enabled = mSubscribedMailbox.CanSelect;

                cmdSubscribe.Enabled = !mSubscribedMailbox.IsSubscribed;
                cmdUnsubscribe.Enabled = mSubscribedMailbox.IsSubscribed;

                cmdDelete.Enabled = mSubscribedMailbox.CanSelect;

                gbxCreate.Enabled = mSubscribedMailbox.CanHaveChildren == true && mSubscribedMailbox.Delimiter != null;
            }
            catch (Exception e)
            {
                lBuilder.AppendLine("An error occurred:");
                lBuilder.AppendLine(e.ToString());
            }

            rtx.Text = lBuilder.ToString();
        }

        private void frmMailboxes_FormClosed(object sender, FormClosedEventArgs e)
        {
            ZUnsubscribeMailbox();
        }

        private void cmdExamine_Click(object sender, EventArgs e)
        {
            mSubscribedMailbox.Select();
        }

        private void cmdSelect_Click(object sender, EventArgs e)
        {
            mSubscribedMailbox.Select(true);
        }

        private void cmdSubscribe_Click(object sender, EventArgs e)
        {
            mSubscribedMailbox.SubscribeAsync();
        }

        private void cmdUnsubscribe_Click(object sender, EventArgs e)
        {
            mSubscribedMailbox.UnsubscribeAsync();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            // do create first
            //if (MessageBox.Show("Delete " + mSubscribedMailbox.Name, "Delete", MessageBoxButtons.OKCancel) == DialogResult.OK) await mSubscribedMailbox.DeleteAsync();
        }

        public class cNodeTag
        {
            public enum eState { neverexpanded, expanding, expanded }

            public readonly cNamespace Namespace;
            public readonly cMailbox Mailbox;
            public readonly iMailboxParent ChildMailboxes;
            public readonly bool CanSelect;
            public readonly TreeNode PleaseWait;

            // if it has been expanded then it has to be refreshed to get any new entries
            public eState State = eState.neverexpanded;

            public cNodeTag(cNamespace pNamespace, TreeNode pPleaseWait)
            {
                Namespace = pNamespace ?? throw new ArgumentNullException(nameof(pNamespace));
                Mailbox = null;
                ChildMailboxes = pNamespace;
                CanSelect = false;
                PleaseWait = pPleaseWait;
            }

            public cNodeTag(cMailbox pMailbox, TreeNode pPleaseWait)
            {
                Namespace = null;
                Mailbox = pMailbox ?? throw new ArgumentNullException(nameof(pMailbox));
                ChildMailboxes = pMailbox;
                CanSelect = pMailbox.CanSelect;
                PleaseWait = pPleaseWait;
            }
        }
    }
}
