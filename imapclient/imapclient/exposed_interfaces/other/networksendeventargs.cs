using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cNetworkSendEventArgs : EventArgs
    {
        public readonly int? Bytes;
        public readonly ReadOnlyCollection<cBytes> Buffers;

        public cNetworkSendEventArgs(cBytes pBuffer)
        {
            Bytes = pBuffer.Count;
            List<cBytes> lBuffers = new List<cBytes>(1);
            lBuffers.Add(pBuffer);
            Buffers = lBuffers.AsReadOnly();
        }

        public cNetworkSendEventArgs(IEnumerable<byte> pBuffer)
        {
            cBytes lBuffer = new cBytes(new List<byte>(pBuffer));
            Bytes = lBuffer.Count;
            List<cBytes> lBuffers = new List<cBytes>(1);
            lBuffers.Add(lBuffer);
            Buffers = lBuffers.AsReadOnly();
        }

        public cNetworkSendEventArgs(int? pBytes, IEnumerable<cBytes> pBuffers)
        {
            Bytes = pBytes;
            Buffers = new List<cBytes>(pBuffers).AsReadOnly();
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cNetworkSendEventArgs));
            if (Bytes != null) lBuilder.Append(Bytes);
            foreach (var lBuffer in Buffers) lBuilder.Append(lBuffer.ToString(80));
            return lBuilder.ToString();
        }
    }
}
