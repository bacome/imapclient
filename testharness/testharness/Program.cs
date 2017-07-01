using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;
using work.bacome.trace;

namespace testharness
{
    static class Program
    {
        public static cTrace Trace = new cTrace("testharness");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static void DisplayAddresses(RichTextBox pRTX, string pAddressType, cAddresses pAddresses)
        {
            if (pAddresses == null) return;

            pRTX.AppendText(pAddressType);

            foreach (var lAddress in pAddresses)
            {
                if (lAddress.DisplayName != null) pRTX.AppendText(lAddress.DisplayName);
                if (lAddress is cEmailAddress lEmailAddress) pRTX.AppendText($"<{lEmailAddress.DisplayAddress}>");
                pRTX.AppendText(", ");
            }

            pRTX.AppendText("\n");
        }
    }
}
