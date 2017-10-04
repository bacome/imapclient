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
        public enum eStoreOperation { add, remove, replace }

        public bool Store(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Store));
            var lTask = ZStoreAsync(cMessageHandleList.FromHandle(pHandle), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result.Count == 0;
        }

        public cMessageHandleList Store(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Store));
            var lTask = ZStoreAsync(cMessageHandleList.FromHandles(pHandles), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public async Task<bool> StoreAsync(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Store));
            var lHandles = await ZStoreAsync(cMessageHandleList.FromHandle(pHandle), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            return lHandles.Count == 0;
        }

        public Task<cMessageHandleList> StoreAsync(IEnumerable<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Store));
            return ZStoreAsync(cMessageHandleList.FromHandles(pHandles), pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
        }

        private async Task<cMessageHandleList> ZStoreAsync(cMessageHandleList pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZStoreAsync), pHandles, pOperation, pFlags, pIfUnchangedSinceModSeq);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

            if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
            if (pIfUnchangedSinceModSeq != null && !lSession.Capabilities.CondStore) throw new InvalidOperationException();

            if (pHandles.Count == 0) return pHandles;
            // it is valid to add or remove zero flags according to the ABNF (!)

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                return await lSession.StoreAsync(lMC, pHandles, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext).ConfigureAwait(false);
            }
        }
    }
}