using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries a response received from a server.
    /// </summary>
    /// <seealso cref="cIMAPClient.NetworkReceive"/>
    public class cNetworkReceiveEventArgs : EventArgs
    {
        /// <summary>
        /// The response that was received.
        /// </summary>
        public readonly cResponse Response;

        internal cNetworkReceiveEventArgs(cResponse pResponse) { Response = pResponse; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cNetworkReceiveEventArgs));
            foreach (var lLine in Response) lBuilder.Append(lLine.ToString(80));
            return lBuilder.ToString();
        }
    }
}
