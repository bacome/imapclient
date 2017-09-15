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
        /*
        private cFetchAttributes ZFetchAttributesRequired(cMessageProperties pProperties)
        {
            fFetchAttributes lAttributesRequired = 0;
            List<string> lNamesRequired = new List<string>();

            if ((pProperties.Properties & (fMessageProperties.envelope | fMessageProperties.sent | fMessageProperties.subject | fMessageProperties.basesubject | fMessageProperties.from | fMessageProperties.sender | fMessageProperties.replyto | fMessageProperties.to | fMessageProperties.cc | fMessageProperties.bcc | fMessageProperties.inreplyto | fMessageProperties.messageid)) != 0) lAttributesRequired |= fFetchAttributes.envelope;
            if ((pProperties.Properties & (fMessageProperties.flags | fMessageProperties.isanswered | fMessageProperties.isflagged | fMessageProperties.isdeleted | fMessageProperties.isseen | fMessageProperties.isdraft | fMessageProperties.isrecent | fMessageProperties.ismdnsent | fMessageProperties.isforwarded | fMessageProperties.issubmitpending | fMessageProperties.issubmitted)) != 0) lAttributesRequired |= fFetchAttributes.flags;
            if ((pProperties.Properties & fMessageProperties.received) != 0) lAttributesRequired |= fFetchAttributes.received;
            if ((pProperties.Properties & fMessageProperties.size) != 0) lAttributesRequired |= fFetchAttributes.size;
            if ((pProperties.Properties & fMessageProperties.uid) != 0) lAttributesRequired |= fFetchAttributes.uid;
            if ((pProperties.Properties & fMessageProperties.modseq) != 0) lAttributesRequired |= fFetchAttributes.modseq;
            if ((pProperties.Properties & (fMessageProperties.bodystructure | fMessageProperties.attachments | fMessageProperties.plaintextsizeinbytes)) != 0) lAttributesRequired |= fFetchAttributes.bodystructure;
            if ((pProperties.Properties & fMessageProperties.references) != 0) lNamesRequired.Add(cHeaderFieldNames.References);
            if ((pProperties.Properties & fMessageProperties.importance) != 0) lNamesRequired.Add(cHeaderFieldNames.Importance);

            return new cFetchAttributes(lAttributesRequired, pProperties.Names.Union(new cHeaderFieldNames(lNamesRequired)));
        } */

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