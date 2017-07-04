﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cMessage UIDFetch(cMailboxId pMailboxId, cUID pUID, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetch));
            var lTask = ZUIDFetchAsync(pMailboxId, ZUIDFetchUIDs(pUID), pProperties, null, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            var lResult = lTask.Result;
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        public List<cMessage> UIDFetch(cMailboxId pMailboxId, IList<cUID> pUIDs, fMessageProperties pProperties, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetch));
            var lTask = ZUIDFetchAsync(pMailboxId, ZUIDFetchUIDs(pUIDs), pProperties, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public async Task<cMessage> UIDFetchAsync(cMailboxId pMailboxId, cUID pUID, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetchAsync));
            var lResult = await ZUIDFetchAsync(pMailboxId, ZUIDFetchUIDs(pUID), pProperties, null, lContext).ConfigureAwait(false);
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        public Task<List<cMessage>> UIDFetchAsync(cMailboxId pMailboxId, IList<cUID> pUIDs, fMessageProperties pProperties, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(UIDFetchAsync));
            return ZUIDFetchAsync(pMailboxId, ZUIDFetchUIDs(pUIDs), pProperties, pFC, lContext);
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

        private async Task<List<cMessage>>ZUIDFetchAsync(cMailboxId pMailboxId, cUIDList pUIDs, fMessageProperties pProperties, cFetchControl pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchAsync), pMailboxId, pUIDs, pProperties, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            if (pUIDs.Count == 0) return new List<cMessage>();

            // must have specified some properties to get, there is no default for fetch
            if ((pProperties & fMessageProperties.allmask) == 0 || (pProperties & fMessageProperties.clientdefault) != 0) throw new ArgumentOutOfRangeException(nameof(pProperties));

            cHandleList lHandles;

            mAsyncCounter.Increment(lContext);

            try
            {
                cFetchPropertiesMethodControl lMC;
                if (pFC == null) lMC = new cFetchPropertiesMethodControl(mTimeout, CancellationToken, null);
                else lMC = new cFetchPropertiesMethodControl(pFC.Timeout, pFC.CancellationToken, pFC.IncrementProgress);
                lHandles = await lSession.UIDFetchAsync(lMC, pMailboxId, pUIDs, pProperties, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }

            List<cMessage> lMessages = new List<cMessage>(lHandles.Count);
            foreach (var lHandle in lHandles) lMessages.Add(new cMessage(this, pMailboxId, lHandle));
            return lMessages;
        }
    }
}