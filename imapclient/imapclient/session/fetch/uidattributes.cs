using System;
using System.Diagnostics;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task<cHandleList> UIDFetchAttributesAsync(cFetchAttributesMethodControl pMC, cMailboxId pMailboxId, cUIDList pUIDs, fFetchAttributes pAttributes, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchAttributesAsync), pMC, pMailboxId, pUIDs, pAttributes);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                var lAttributes = ZFetchAttributes(pMailboxId.MailboxName, pAttributes, lContext);


                // split the list into those messages I have handles for and those I dont
                /////////////////////////////////////////////////////////////////////////

                cHandleList lHandles = new cHandleList();
                cUIDList lUIDs = new cUIDList();

                // check the selected mailbox and resolve uids -> handles whilst blocking select exclusive access
                //
                using (var lBlock = await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false))
                {
                    if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);

                    foreach (var lUID in pUIDs)
                    {
                        var lHandle = _SelectedMailbox.GetHandle(lUID);
                        if (lHandle == null) lUIDs.Add(lUID); // don't have a handle
                        else if((~lHandle.Attributes & lAttributes) == lAttributes) lUIDs.Add(lUID); // have to get all the attributes
                        else lHandles.Add(lHandle);
                    }
                }

                // for the messages I have handles for, fetch the missing attributes
                ////////////////////////////////////////////////////////////////////

                if (lHandles.Count > 0)
                {
                    // split the handles into groups based on what attributes need to be retrieved, for each group do the retrieval
                    foreach (var lGroup in ZFetchAttributesGroups(lHandles, lAttributes)) await ZFetchAttributesAsync(pMC, pMailboxId, lGroup, lContext).ConfigureAwait(false);
                }

                // for the messages only identified by UID or where I have to get all the attributes
                ////////////////////////////////////////////////////////////////////////////////////

                if (lUIDs.Count > 0)
                {
                    await ZUIDFetchAttributesAsync(pMC, pMailboxId, lUIDs, lAttributes, lContext).ConfigureAwait(false);

                    // resolve uids -> handles whilst blocking select exclusive access
                    //
                    using (var lBlock = await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false))
                    {
                        if (_SelectedMailbox == null || _SelectedMailbox.MailboxId != pMailboxId) throw new cMailboxNotSelectedException(lContext);

                        foreach (var lUID in lUIDs)
                        {
                            var lHandle = _SelectedMailbox.GetHandle(lUID);
                            if (lHandle != null) lHandles.Add(lHandle);
                        }
                    }
                }

                return lHandles;
            }

            public async Task ZUIDFetchAttributesAsync(cFetchAttributesMethodControl pMC, cMailboxId pMailboxId, cUIDList pUIDs, fFetchAttributes pAttributes, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchAttributesAsync), pMC, pMailboxId, pUIDs, pAttributes);

                // get the UIDValidity
                uint lUIDValidity = pUIDs[0].UIDValidity;

                // sort the uids so we might get good sequence sets
                pUIDs.Sort();

                int lIndex = 0;

                while (lIndex < pUIDs.Count)
                {
                    // the number of messages to fetch this time
                    int lFetchCount = mFetchAttributesSizer.Current;

                    // get the UIDs to fetch this time
                    cUIntList lUIDs = new cUIntList();
                    while (lIndex < pUIDs.Count && lUIDs.Count < lFetchCount) lUIDs.Add(pUIDs[lIndex++].UID);

                    // fetch
                    Stopwatch lStopwatch = Stopwatch.StartNew();
                    await ZUIDFetchAttributesAsync(pMC, pMailboxId, lUIDValidity, lUIDs, pAttributes, lContext).ConfigureAwait(false);
                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchAttributesSizer.AddSample(lUIDs.Count, lStopwatch.ElapsedMilliseconds, lContext);
                    pMC.IncrementProgress(lUIDs.Count);
                }
            }
        }
    }
}