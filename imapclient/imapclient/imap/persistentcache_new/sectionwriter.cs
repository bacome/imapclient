using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal interface iSectionWriter
    {
        void InstallDecoder(bool pBinary, eDecodingRequired pDecoding, cTrace.cContext pParentContext);
        Task WriteAsync(IList<byte> pBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext);
    }
}