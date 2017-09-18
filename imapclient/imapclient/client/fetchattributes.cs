using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private async Task ZFetchAttributesAsync(cMessageHandleList pHandles, cFetchAttributes pAttributes, cPropertyFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchAttributesAsync), pHandles, pAttributes);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pAttributes == null) throw new ArgumentNullException(nameof(pAttributes));

            if (pHandles.Count == 0) return;
            if (pAttributes.IsNone) return;

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lProgress = new cProgress();                    
                    await lSession.FetchAttributesAsync(lMC, pHandles, pAttributes, lProgress, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lProgress = new cProgress(mSynchroniser, pConfiguration.Increment);
                await lSession.FetchAttributesAsync(lMC, pHandles, pAttributes, lProgress, lContext).ConfigureAwait(false);
            }
        }

        private async Task<List<cMessage>> ZUIDFetchAttributesAsync(iMailboxHandle pHandle, cUIDList pUIDs, cFetchAttributes pAttributes, cPropertyFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchAttributesAsync), pHandle, pUIDs, pAttributes);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
            if (pAttributes == null) throw new ArgumentNullException(nameof(pAttributes));

            if (pUIDs.Count == 0) return new List<cMessage>();
            if (pAttributes.IsNone) throw new ArgumentOutOfRangeException(nameof(pAttributes));

            cMessageHandleList lHandles;

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lProgress = new cProgress();
                    lHandles = await lSession.UIDFetchAttributesAsync(lMC, pHandle, pUIDs, pAttributes, lProgress, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lProgress = new cProgress(mSynchroniser, pConfiguration.Increment);
                lHandles = await lSession.UIDFetchAttributesAsync(lMC, pHandle, pUIDs, pAttributes, lProgress, lContext).ConfigureAwait(false);
            }

            List<cMessage> lMessages = new List<cMessage>(lHandles.Count);
            foreach (var lHandle in lHandles) lMessages.Add(new cMessage(this, lHandle));
            return lMessages;
        }
    }
}