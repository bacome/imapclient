using System;
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
            private async Task ZUIDFetchCacheItemsAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, uint pUIDValidity, cUIntList pUIDs, cMessageCacheItems pItems, ulong pChangedSince, bool pVanished, cTrace.cContext pParentContext)
            {
                // note that this will fail if the UIDValidity has changed (this is different to the behaviour of standard fetch)
                // note that the caller should have checked that pAttributes contains some attributes to fetch

                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchCacheItemsAsync), pMC, pMailboxHandle, pUIDValidity, pUIDs, pItems, pChangedSince, pVanished);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
                if (pItems == null) throw new ArgumentNullException(nameof(pItems));

                if (pUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pUIDs));
                if (pItems.IsEmpty) throw new ArgumentOutOfRangeException(nameof(pItems));

                if (pChangedSince != 0 && !_Capabilities.CondStore) throw new ArgumentOutOfRangeException(nameof(pChangedSince));
                if (pVanished) if ((EnabledExtensions & fEnableableExtensions.qresync) == 0 || pChangedSince == 0) throw new ArgumentOutOfRangeException(nameof(pChangedSince));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, pUIDValidity);

                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.AddUIDValidity(pUIDValidity); // the command is sensitive to UIDValidity changes

                    lBuilder.Add(kFetchCommandPartUIDFetchSpace, new cTextCommandPart(cSequenceSet.FromUInts(pUIDs)), cCommandPart.Space);
                    lBuilder.Add(pItems, lSelectedMailbox.MessageCache.NoModSeq);

                    if (pChangedSince > 0)
                    {
                        lBuilder.Add(kFetchCommandPartChangedSince, new cTextCommandPart(pChangedSince));
                        if (pVanished) lBuilder.Add(kFetchCommandPartVanished);
                        lBuilder.Add(cCommandPart.RParen);
                    }

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("uid fetch success");
                        return;
                    }

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, 0, lContext);
                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}