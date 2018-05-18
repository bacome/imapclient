using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal interface iFetchSectionTarget
    {
        Task WriteAsync(byte[] pBuffer, int pCount, int pFetchedBytesInBuffer, CancellationToken pCancellationToken, cTrace.cContext pParentContext);
    }
}