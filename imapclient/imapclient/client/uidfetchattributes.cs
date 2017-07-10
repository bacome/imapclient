using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cMessage UIDFetch(cMailboxId pMailboxId, cUID pUID, fFetchAttributes pAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetch));
            var lTask = ZUIDFetchAsync(pMailboxId, ZUIDFetchUIDs(pUID), pAttributes, null, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            var lResult = lTask.Result;
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        public List<cMessage> UIDFetch(cMailboxId pMailboxId, IList<cUID> pUIDs, fFetchAttributes pAttributes, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetch));
            var lTask = ZUIDFetchAsync(pMailboxId, ZUIDFetchUIDs(pUIDs), pAttributes, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public async Task<cMessage> UIDFetchAsync(cMailboxId pMailboxId, cUID pUID, fFetchAttributes pAttributes)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetchAsync));
            var lResult = await ZUIDFetchAsync(pMailboxId, ZUIDFetchUIDs(pUID), pAttributes, null, lContext).ConfigureAwait(false);
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        public Task<List<cMessage>> UIDFetchAsync(cMailboxId pMailboxId, IList<cUID> pUIDs, fFetchAttributes pAttributes, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetchAsync));
            return ZUIDFetchAsync(pMailboxId, ZUIDFetchUIDs(pUIDs), pAttributes, pFC, lContext);
        }

        private cUIDList ZUIDFetchUIDs(cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cUIDList(pUID);
        }

        private cUIDList ZUIDFetchUIDs(IList<cUID> pUIDs)
        {
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            uint? lUIDValidity = null;

            foreach (var lUID in pUIDs)
            {
                if (lUID == null) throw new ArgumentOutOfRangeException(nameof(pUIDs), "contains nulls");
                if (lUIDValidity == null) lUIDValidity = lUID.UIDValidity;
                else if (lUID.UIDValidity != lUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDs), "contains mixed uidvalidities");
            }

            return new cUIDList(pUIDs);
        }

        private async Task<List<cMessage>>ZUIDFetchAsync(cMailboxId pMailboxId, cUIDList pUIDs, fFetchAttributes pAttributes, cFetchControl pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchAsync), pMailboxId, pUIDs, pAttributes, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            if (pUIDs.Count == 0) return new List<cMessage>();

            // must have specified some attributes to get, there is no default for fetch
            if ((pAttributes & fFetchAttributes.allmask) == 0 || (pAttributes & fFetchAttributes.clientdefault) != 0) throw new ArgumentOutOfRangeException(nameof(pAttributes));

            cHandleList lHandles;

            mAsyncCounter.Increment(lContext);

            try
            {
                cFetchAttributesMethodControl lMC;
                if (pFC == null) lMC = new cFetchAttributesMethodControl(mTimeout, CancellationToken, null);
                else lMC = new cFetchAttributesMethodControl(pFC.Timeout, pFC.CancellationToken, pFC.IncrementProgress);
                lHandles = await lSession.UIDFetchAsync(lMC, pMailboxId, pUIDs, pAttributes, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }

            List<cMessage> lMessages = new List<cMessage>(lHandles.Count);
            foreach (var lHandle in lHandles) lMessages.Add(new cMessage(this, pMailboxId, lHandle));
            return lMessages;
        }
    }
}
