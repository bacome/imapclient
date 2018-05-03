using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal Task UIDFetchAsync(iMailboxHandle pMailboxHandle, cSectionCachePersistentKey pKey, cSectionCache.cItem.cReaderWriter pReaderWriter, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetchAsync), pMailboxHandle, pKey);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pKey == null) throw new ArgumentNullException(nameof(pKey));
            if (pReaderWriter == null) throw new ArgumentNullException(nameof(pReaderWriter));

            return lSession.UIDFetchBodyAsync(pMailboxHandle, pKey, pReaderWriter, pCancellationToken, lContext);
        }
    }
}