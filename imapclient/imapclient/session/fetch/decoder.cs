using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cDecoder
            {
                private Stream mStream;

                public cDecoder(Stream pStream)
                {
                    mStream = pStream;
                }

                protected virtual Task YWriteAsync(cMethodControl pMC, byte[] lBuffer)
                {
                    mStream.WriteTimeout = pMC.Timeout;
                    return mStream.WriteAsync(lBuffer, 0, lBuffer.Length, pMC.CancellationToken);
                }

                public abstract Task WriteAsync(cMethodControl pMC, IList<byte> pBytes, int pOffset);
                public abstract bool HasUndecodedBytes();
            }

            private class cIdentityDecoder : cDecoder
            {
                public cIdentityDecoder(Stream pStream) : base(pStream) { }

                public override Task WriteAsync(cMethodControl pMC, IList<byte> pBytes, int pOffset)
                {
                    int lCount = pBytes.Count - pOffset;
                    byte[] lBuffer = new byte[lCount];
                    for (int i = 0, j = pOffset; i < lCount; i++, j++) lBuffer[i] = pBytes[j];
                    return YWriteAsync(pMC, lBuffer);
                }

                public override bool HasUndecodedBytes() => false;
            }

            private class cBase64Decoder : cDecoder
            {
                public cBase64Decoder(Stream pStream) : base(pStream) { }

                public override Task WriteAsync(cMethodControl pMC, IList<byte> pBytes, int pOffset)
                {
                    // TODO: rfc 2045 section 6.8
                    throw new NotImplementedException();
                }

                public override bool HasUndecodedBytes() => false;
            }

            private class cQuotedPrintableDecoder : cDecoder
            {
                public cQuotedPrintableDecoder(Stream pStream) : base(pStream) { }

                public override Task WriteAsync(cMethodControl pMC, IList<byte> pBytes, int pOffset)
                {
                    // TODO: rfc 2045 section 6.7
                    throw new NotImplementedException();
                }

                public override bool HasUndecodedBytes() => false;
            }
        }
    }
}