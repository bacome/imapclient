﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task<cHandleList> FetchPropertiesAsync(cMethodControl pMC, cMailboxId pMailboxId, cUIDList pUIDs, fMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchPropertiesAsync), pMC, pMailboxId, pUIDs, pProperties);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);


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
                        if (lHandle == null) lUIDs.Add(lUID);
                        else lHandles.Add(lHandle);
                    }
                }


                // for the messages I have handles for, fetch the missing properties
                ////////////////////////////////////////////////////////////////////

                if (lHandles.Count > 0)
                {
                    // split the handles into groups based on what properties need to be retrieved, for each group do the retrieval
                    foreach (var lGroup in ZFetchPropertiesFetchGroups(lHandles, pProperties)) await ZFetchPropertiesAsync(pMC, pMailboxId, lGroup, lContext).ConfigureAwait(false);
                }

                // for the messages only identified by UID I have to get all the properties
                ///////////////////////////////////////////////////////////////////////////

                if (lUIDs.Count > 0)
                {
                    await ZFetchPropertiesAsync(pMC, pMailboxId, lUIDs, pProperties, lContext).ConfigureAwait(false);

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

            public async Task ZFetchPropertiesAsync(cMethodControl pMC, cMailboxId pMailboxId, cUIDList pUIDs, fMessageProperties pProperties, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchPropertiesAsync), pMC, pMailboxId, pUIDs, pProperties);

                // get the UIDValidity
                uint lUIDValidity = pUIDs[0].UIDValidity;

                // sort the uids so we might get good sequence sets
                pUIDs.Sort();

                int lIndex = 0;

                while (lIndex < pUIDs.Count)
                {
                    // the number of messages to fetch this time
                    int lFetchCount = mFetchPropertiesConfiguration.Current;

                    // get the UIDs to fetch this time
                    cUIntList lUIDs = new cUIntList();
                    while (lIndex < pUIDs.Count && lUIDs.Count < lFetchCount) lUIDs.Add(pUIDs[lIndex++].UID);

                    // fetch
                    Stopwatch lStopwatch = Stopwatch.StartNew();
                    await ZUIDFetchPropertiesAsync(pMC, pMailboxId, lUIDValidity, lUIDs, pProperties, lContext).ConfigureAwait(false);
                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchPropertiesConfiguration.AddSample(lUIDs.Count, lStopwatch.ElapsedMilliseconds);
                }
            }
        }
    }
}