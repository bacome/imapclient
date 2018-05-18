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
        internal Task FetchSectionAsync(iMessageHandle pMessageHandle, cSection pSection, eDecodingRequired pDecoding, iFetchSectionTarget pTarget, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(FetchSectionAsync), pMessageHandle, pSection, pDecoding);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            if (pTarget == null) throw new ArgumentNullException(nameof(pTarget));

            return lSession.FetchSectionAsync(pMessageHandle, pSection, pDecoding, pTarget, pCancellationToken, lContext);
        }
    }
}