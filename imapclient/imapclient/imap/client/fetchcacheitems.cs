using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Ensures that the specified items are cached for the specified messages.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <param name="pItems"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns>A list of messages where something went wrong and the cache was not populated.</returns>
        /// <remarks>
        /// <note type="note"><see cref="cMessageCacheItems"/> has implicit conversions from other types including <see cref="fIMAPMessageProperties"/>. This means that you can use values of those types as arguments to this method.</note>
        /// </remarks>
        public List<cIMAPMessage> FetchCacheItems(IEnumerable<cIMAPMessage> pMessages, cMessageCacheItems pItems, cIncrementConfiguration pConfiguration)
        {
            var lContext = RootContext.NewMethod(true, nameof(cIMAPClient), nameof(FetchCacheItems), pItems, pConfiguration);

            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lMessageHandles = cMessageHandleList.FromMessages(pMessages);

            var lTask = FetchCacheItemsAsync(lMessageHandles, pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return new List<cIMAPMessage>(from m in pMessages where !m.MessageHandle.Contains(pItems) select m);
        }

        /// <summary>
        /// Asynchronously ensures that the specified items are cached for the specified messages.
        /// </summary>
        /// <param name="pMessages"></param>
        /// <param name="pItems"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <inheritdoc cref="Fetch(IEnumerable{cIMAPMessage}, cMessageCacheItems, cFetchCacheItemConfiguration)" select="returns|remarks"/>
        public async Task<List<cIMAPMessage>> FetchCacheItemsAsync(IEnumerable<cIMAPMessage> pMessages, cMessageCacheItems pItems, cIncrementConfiguration pConfiguration)
        {
            var lContext = RootContext.NewMethod(true, nameof(cIMAPClient), nameof(FetchCacheItemsAsync), pItems, pConfiguration);

            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            var lMessageHandles = cMessageHandleList.FromMessages(pMessages);

            await FetchCacheItemsAsync(lMessageHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);

            return new List<cIMAPMessage>(from m in pMessages where !m.MessageHandle.Contains(pItems) select m);
        }

        internal async Task FetchCacheItemsAsync(cMessageHandleList pMessageHandles, cMessageCacheItems pItems, cIncrementConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(FetchCacheItemsAsync), pMessageHandles, pItems, pConfiguration);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pMessageHandles.Count == 0) return;
            if (pItems.IsEmpty) return;

            if (pMessageHandles.All(h => h.Contains(pItems))) return;

            if (pConfiguration == null)
            {
                using (var lToken = CancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    await lSession.FetchCacheItemsAsync(lMC, pMessageHandles, pItems, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                await lSession.FetchCacheItemsAsync(lMC, pMessageHandles, pItems, pConfiguration.Increment, lContext).ConfigureAwait(false);
            }
        }
    }
}
