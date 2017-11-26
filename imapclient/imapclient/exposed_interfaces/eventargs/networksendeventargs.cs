using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries data sent to a server.
    /// </summary>
    /// <seealso cref="cIMAPClient.NetworkSend"/>
    public class cNetworkSendEventArgs : EventArgs
    {
        /// <summary>
        /// The number of bytes sent (<see langword="null"/> if this can't be disclosed).
        /// </summary>
        public readonly int? Bytes;

        /// <summary>
        /// The bytes sent (sensitive data redacted).
        /// </summary>
        public readonly ReadOnlyCollection<cBytes> Buffers;

        internal cNetworkSendEventArgs(cBytes pBuffer)
        {
            Bytes = pBuffer.Count;
            List<cBytes> lBuffers = new List<cBytes>(1);
            lBuffers.Add(pBuffer);
            Buffers = lBuffers.AsReadOnly();
        }

        internal cNetworkSendEventArgs(IEnumerable<byte> pBuffer)
        {
            cBytes lBuffer = new cBytes(new List<byte>(pBuffer));
            Bytes = lBuffer.Count;
            List<cBytes> lBuffers = new List<cBytes>(1);
            lBuffers.Add(lBuffer);
            Buffers = lBuffers.AsReadOnly();
        }

        internal cNetworkSendEventArgs(int? pBytes, IEnumerable<cBytes> pBuffers)
        {
            Bytes = pBytes;
            Buffers = new List<cBytes>(pBuffers).AsReadOnly();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cNetworkSendEventArgs));
            if (Bytes != null) lBuilder.Append(Bytes);
            foreach (var lBuffer in Buffers) lBuilder.Append(lBuffer.ToString(80));
            return lBuilder.ToString();
        }
    }
}
