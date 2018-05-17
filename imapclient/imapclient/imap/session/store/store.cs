﻿using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public Task StoreAsync(cMethodControl pMC, cStoreFeedback pFeedback, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StoreAsync), pMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pFeedback.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFeedback));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !_Capabilities.CondStore) throw new InvalidOperationException(kInvalidOperationExceptionMessage.CondStoreNotInUse);

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckInSelectedMailbox(pFeedback); // to be repeated inside the select lock
                if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate); // to be repeated inside the select lock

                if (pFeedback.AllHaveUID)
                {
                    cStoreFeedbackCollector lFeedbackCollector = new cStoreFeedbackCollector(pFeedback);
                    return ZUIDStoreAsync(pMC, lSelectedMailbox.MailboxHandle, pFeedback[0].MessageHandle.UID.UIDValidity, lFeedbackCollector, pOperation, pFlags, pIfUnchangedSinceModSeq, null, lContext);
                }
                else return ZStoreAsync(pMC, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq, lContext);
            }

            public Task UIDStoreAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cUIDStoreFeedback pFeedback, eStoreOperation pOperation, cStorableFlags pFlags, ulong? pIfUnchangedSinceModSeq, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDStoreAsync), pMC, pMailboxHandle, pFeedback, pOperation, pFlags, pIfUnchangedSinceModSeq);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pFeedback == null) throw new ArgumentNullException(nameof(pFeedback));
                if (pFlags == null) throw new ArgumentNullException(nameof(pFlags));

                if (pFeedback.Count == 0) throw new ArgumentOutOfRangeException(nameof(pFeedback));

                if (pIfUnchangedSinceModSeq == 0) throw new ArgumentOutOfRangeException(nameof(pIfUnchangedSinceModSeq));
                if (pIfUnchangedSinceModSeq != null && !_Capabilities.CondStore) throw new InvalidOperationException(kInvalidOperationExceptionMessage.CondStoreNotInUse);

                uint lUIDValidity = pFeedback[0].UID.UIDValidity;

                cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, lUIDValidity); // to be repeated inside the select lock
                if (!lSelectedMailbox.SelectedForUpdate) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelectedForUpdate); // to be repeated inside the select lock

                cStoreFeedbackCollector lFeedbackCollector = new cStoreFeedbackCollector(pFeedback);
                return ZUIDStoreAsync(pMC, pMailboxHandle, lUIDValidity, lFeedbackCollector, pOperation, pFlags, pIfUnchangedSinceModSeq, pFeedback, lContext);
            }
        }
    }
}