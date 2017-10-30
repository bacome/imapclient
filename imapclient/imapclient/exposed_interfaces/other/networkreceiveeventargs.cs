using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cNetworkReceiveEventArgs : EventArgs
    {
        public readonly cBytesLines Lines;

        public cNetworkReceiveEventArgs(cBytesLines pLines) { Lines = pLines; }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cNetworkReceiveEventArgs));
            foreach (var lLine in Lines) lBuilder.Append(lLine.ToString(80));
            return lBuilder.ToString();
        }
    }
}
