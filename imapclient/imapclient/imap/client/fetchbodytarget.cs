using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal interface iFetchBodyTarget
    {
        Task WriteAsync(byte[] pBuffer, int pCount, CancellationToken pCancellationToken, cTrace.cContext pParentContext);
    }
}