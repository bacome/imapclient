using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmMessageData : Form
    {
        private cUID mUID;
        private cSection mSection;
        private eDecodingRequired mDecoding;
        private string mData;

        public frmMessageData(cUID pUID, cSection pSection, eDecodingRequired pDecoding, string pData)
        {
            mUID = pUID;
            mSection = pSection;
            mDecoding = pDecoding;
            mData = pData;

            InitializeComponent();
        }

        private void frmMessageData_Load(object sender, EventArgs e)
        {
            Text = $"imapclient testharness - message data - {mUID} {mSection} {mDecoding}";
            rtx.Text = mData;
        }
    }
}
