using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public enum eNetworkActivitySource { Client, Server }

    public class cNetworkActivityEventArgs : EventArgs
    {
        public readonly eNetworkActivitySource Source;
        public readonly string Message;

        public cNetworkActivityEventArgs(eResponseTextType pTextType, cResponseText pText)
        {
            TextType = pTextType;
            Text = pText;
        }

        public override string ToString()
        {
            return $"{nameof(cNetworkActivityEventArgs)}({Source},{Text})";
        }
    }
}
