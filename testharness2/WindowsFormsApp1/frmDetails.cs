using System;
using System.Collections.Generic;
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
    public partial class frmDetails : Form
    {
        private readonly cIMAPClient mClient;
        private readonly StringBuilder mBuilder = new StringBuilder();

        public frmDetails(cIMAPClient pClient)
        {
            mClient = pClient;
            InitializeComponent();
        }

        private void frmDetails_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - details - " + mClient.InstanceName;
            mClient.PropertyChanged += mClient_PropertyChanged;
            ZDisplay();
        }

        private void mClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ZDisplay();
        }

        private void frmDetails_FormClosed(object sender, FormClosedEventArgs e)
        {
            mClient.PropertyChanged -= mClient_PropertyChanged;
        }

        private void ZDisplay()
        {
            if (mClient.Capabilities == null) mBuilder.AppendLine("Capability not set yet.");
            else
            {
                mBuilder.AppendLine("All capabilities:");
                ZDisplayStrings(mBuilder, mClient.Capabilities.Capabilities);
                mBuilder.AppendLine();
                mBuilder.AppendLine("Authentication mechanisms:");
                ZDisplayStrings(mBuilder, mClient.Capabilities.AuthenticationMechanisms);
                mBuilder.AppendLine();
                mBuilder.AppendLine("Effective capabilities:");
                mBuilder.AppendLine(mClient.Capabilities.EffectiveCapabilities.ToString());
            }

            mBuilder.AppendLine();

            mBuilder.AppendLine("Enabled extensions:");
            mBuilder.AppendLine(mClient.EnabledExtensions.ToString());

            mBuilder.AppendLine();

            if (mClient.ConnectedAccountId == null) mBuilder.AppendLine("No connected account yet.");
            else
            {
                mBuilder.AppendLine("Connected account:");
                mBuilder.AppendLine(mClient.ConnectedAccountId.ToString());
            }

            mBuilder.AppendLine();

            if (mClient.HomeServerReferral == null) mBuilder.AppendLine("No home server referral.");
            else
            {
                mBuilder.AppendLine("Home server referral:");
                mBuilder.AppendLine(mClient.HomeServerReferral.ToString());
            }

            mBuilder.AppendLine();

            if (mClient.ServerId == null) mBuilder.AppendLine("No server id.");
            else
            {
                mBuilder.AppendLine("Server Id:");
                mBuilder.AppendLine(mClient.ServerId.ToString());
            }

            rtx.Text = mBuilder.ToString();

            mBuilder.Clear();
        }

        private void ZDisplayStrings(StringBuilder pBuilder, ICollection<string> pStrings)
        {
            bool lFirst = true;

            foreach (var lString in pStrings)
            {
                if (lFirst) lFirst = false;
                else pBuilder.Append(", ");
                pBuilder.Append(lString);
            }

            pBuilder.AppendLine();
        }

        private void cmdRefresh_Click(object sender, EventArgs e)
        {
            ZDisplay();
        }
    }
}
