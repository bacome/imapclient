using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents data received from a server.
    /// </summary>
    /// <seealso cref="cIMAPClient.NetworkReceive"/>
    public class cNetworkReceiveEventArgs : EventArgs
    {
        /// <summary>
        /// The data received.
        /// </summary>
        public readonly cBytesLines Lines;

        internal cNetworkReceiveEventArgs(cBytesLines pLines) { Lines = pLines; }

        /**<summary>Returns a string that represents the data received.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cNetworkReceiveEventArgs));
            foreach (var lLine in Lines) lBuilder.Append(lLine.ToString(80));
            return lBuilder.ToString();
        }
    }
}
