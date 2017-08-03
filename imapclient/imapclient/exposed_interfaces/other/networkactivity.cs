using System;

namespace work.bacome.imapclient
{
    public enum eNetworkActivitySource { Client, Server }

    public class cNetworkActivityEventArgs : EventArgs
    {
        public readonly eNetworkActivitySource Source;
        public readonly string Text;

        public cNetworkActivityEventArgs(eNetworkActivitySource pSource, string pText)
        {
            Source = pSource;
            Text = pText;
        }

        public override string ToString()
        {
            return $"{nameof(cNetworkActivityEventArgs)}({Source},{Text})";
        }
    }
}
