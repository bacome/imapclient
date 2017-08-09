using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private fFetchAttributes ZFetchAttributesRequired(fMessageProperties pProperties)
        {
            fFetchAttributes lRequired = 0;

            var lProperties = ZDefaultMessagePropertiesAdd(pProperties);

            if ((lProperties & (fMessageProperties.sent | fMessageProperties.subject | fMessageProperties.basesubject | fMessageProperties.from | fMessageProperties.sender | fMessageProperties.replyto | fMessageProperties.to | fMessageProperties.cc | fMessageProperties.bcc | fMessageProperties.inreplyto | fMessageProperties.messageid)) != 0) lRequired |= fFetchAttributes.envelope;
            if ((lProperties & (fMessageProperties.flags | fMessageProperties.isanswered | fMessageProperties.isflagged | fMessageProperties.isdeleted | fMessageProperties.isseen | fMessageProperties.isdraft | fMessageProperties.isrecent | fMessageProperties.ismdnsent | fMessageProperties.isforwarded | fMessageProperties.issubmitpending | fMessageProperties.issubmitted)) != 0) lRequired |= fFetchAttributes.flags;
            if ((lProperties & fMessageProperties.received) != 0) lRequired |= fFetchAttributes.received;
            if ((lProperties & fMessageProperties.size) != 0) lRequired |= fFetchAttributes.size;
            if ((lProperties & fMessageProperties.uid) != 0) lRequired |= fFetchAttributes.uid;
            if ((lProperties & fMessageProperties.references) != 0) lRequired |= fFetchAttributes.references;
            if ((lProperties & fMessageProperties.modseq) != 0) lRequired |= fFetchAttributes.modseq;
            if ((lProperties & (fMessageProperties.bodystructure | fMessageProperties.attachments)) != 0) lRequired |= fFetchAttributes.bodystructure;

            return lRequired;
        }

        private fFetchAttributes ZFetchAttributesToFetch(cMessageHandleList pHandles, fFetchAttributes pRequired)
        {
            fFetchAttributes lToFetch = 0;
            foreach (var lHandle in pHandles) lToFetch |= pRequired & ~lHandle.Attributes;
            return lToFetch;
        }

        private async Task ZFetchAttributesAsync(cMessageHandleList pHandles, fFetchAttributes pAttributes, cFetchControl pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZFetchAttributesAsync), pHandles, pAttributes, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new InvalidOperationException();

            if (pHandles == null) throw new ArgumentNullException(nameof(pHandles));
            if (pHandles.Count == 0) return;
            if (pAttributes == 0) throw new ArgumentOutOfRangeException(nameof(pAttributes));

            mAsyncCounter.Increment(lContext);

            try
            {
                cFetchAttributesMethodControl lMC;
                if (pFC == null) lMC = new cFetchAttributesMethodControl(mTimeout, CancellationToken, null);
                else lMC = new cFetchAttributesMethodControl(pFC.Timeout, pFC.CancellationToken, pFC.IncrementProgress);
                await lSession.FetchAttributesAsync(lMC, pHandles, pAttributes, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }
        }

        private async Task<List<cMessage>> ZUIDFetchAttributesAsync(iMailboxHandle pHandle, cUIDList pUIDs, fFetchAttributes pAttributes, cFetchControl pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZUIDFetchAttributesAsync), pHandle, pUIDs, pAttributes, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
            if (pUIDs.Count == 0) return new List<cMessage>();
            if (pAttributes == 0) throw new ArgumentOutOfRangeException(nameof(pAttributes));

            cMessageHandleList lHandles;

            mAsyncCounter.Increment(lContext);

            try
            {
                cFetchAttributesMethodControl lMC;
                if (pFC == null) lMC = new cFetchAttributesMethodControl(mTimeout, CancellationToken, null);
                else lMC = new cFetchAttributesMethodControl(pFC.Timeout, pFC.CancellationToken, pFC.IncrementProgress);
                lHandles = await lSession.UIDFetchAttributesAsync(lMC, pHandle, pUIDs, pAttributes, lContext).ConfigureAwait(false);
            }
            finally { mAsyncCounter.Decrement(lContext); }

            List<cMessage> lMessages = new List<cMessage>(lHandles.Count);
            foreach (var lHandle in lHandles) lMessages.Add(new cMessage(this, lHandle));
            return lMessages;
        }

        private class cFetchAttributesMethodControl : cMethodControl
        {
            private readonly Action<int> mIncrementProgress;

            public cFetchAttributesMethodControl(int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrementProgress) : base(pTimeout, pCancellationToken)
            {
                mIncrementProgress = pIncrementProgress;
            }

            public void IncrementProgress(int pValue) => mIncrementProgress?.Invoke(pValue);
        }
    }
}