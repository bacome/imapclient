using System;
using System.Diagnostics;
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
            public async Task<cMessageHandleList> UIDFetchCacheItemsAsync(cMethodControl pMC, cBatchSizer pBatchSizer, iMailboxHandle pMailboxHandle, cUIDList pUIDs, cMessageCacheItems pItems, ulong pChangedSince, bool pVanished, Action<int> pIncrement, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchCacheItemsAsync), pMC, pBatchSizer, pMailboxHandle, pUIDs, pItems, pChangedSince, pVanished);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pBatchSizer == null) throw new ArgumentNullException(nameof(pBatchSizer));
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
                if (pItems == null) throw new ArgumentNullException(nameof(pItems));

                if (pUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pUIDs));
                if (pItems.IsEmpty) throw new ArgumentOutOfRangeException(nameof(pItems));

                if (pChangedSince != 0 && (EnabledExtensions & fEnableableExtensions.qresync) == 0) throw new ArgumentOutOfRangeException(nameof(pChangedSince));
                if (pVanished && pChangedSince == 0) throw new ArgumentOutOfRangeException(nameof(pVanished));

                uint lUIDValidity = pUIDs[0].UIDValidity;

                mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, lUIDValidity); // to be repeated inside the select lock

                // split the list into those messages I have handles for and those I dont
                /////////////////////////////////////////////////////////////////////////

                cMessageHandleList lMessageHandles = new cMessageHandleList();
                cUIDList lUIDs = new cUIDList();

                // check the selected mailbox and resolve uids -> handles whilst blocking select exclusive access
                //
                using (var lBlock = await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false))
                {
                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, lUIDValidity);

                    foreach (var lUID in pUIDs)
                    {
                        var lMessageHandle = lSelectedMailbox.GetHandle(lUID);
                        if (lMessageHandle == null) lUIDs.Add(lUID); // don't have a handle
                        else if (lMessageHandle.ContainsNone(pItems)) lUIDs.Add(lUID); // have to get all the attributes, may as well fetch them with the ones where I might need all the attributes
                        else lMessageHandles.Add(lMessageHandle);
                    }
                }

                using (var lIncrementer = new cIncrementer(mSynchroniser, pIncrement, mIncrementInvokeMillisecondsDelay, lContext))
                {
                    // for the messages I have handles for, fetch the missing attributes
                    ////////////////////////////////////////////////////////////////////

                    if (lMessageHandles.Count > 0)
                    {
                        // split the handles into groups based on what attributes need to be retrieved, for each group do the retrieval
                        foreach (var lGroup in ZFetchCacheItemsGroups(lMessageHandles, pItems)) await ZFetchCacheItemsAsync(pMC, pBatchSizer, lGroup, pChangedSince, pVanished, lIncrementer, lContext).ConfigureAwait(false);
                    }

                    // for the messages only identified by UID or where I have to get all the items
                    ///////////////////////////////////////////////////////////////////////////////

                    if (lUIDs.Count > 0)
                    {
                        await ZUIDFetchCacheItemsAsync(pMC, pBatchSizer, pMailboxHandle, lUIDs, pItems, pChangedSince, pVanished, lIncrementer, lContext).ConfigureAwait(false);

                        // resolve uids -> handles whilst blocking select exclusive access
                        //
                        using (var lBlock = await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false))
                        {
                            cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pMailboxHandle, lUIDValidity);

                            foreach (var lUID in lUIDs)
                            {
                                var lMessageHandle = lSelectedMailbox.GetHandle(lUID);
                                if (lMessageHandle != null) lMessageHandles.Add(lMessageHandle);
                            }
                        }
                    }
                }

                return lMessageHandles;
            }

            private async Task ZUIDFetchCacheItemsAsync(cMethodControl pMC, cBatchSizer pBatchSizer, iMailboxHandle pMailboxHandle, cUIDList pUIDs, cMessageCacheItems pItems, ulong pChangedSince, bool pVanished, cIncrementer pIncrementer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchCacheItemsAsync), pMC, pMailboxHandle, pUIDs, pItems, pChangedSince, pVanished);

                // get the UIDValidity
                uint lUIDValidity = pUIDs[0].UIDValidity;

                // sort the uids so we might get good sequence sets
                pUIDs.Sort();

                int lIndex = 0;
                Stopwatch lStopwatch = new Stopwatch();

                while (lIndex < pUIDs.Count)
                {
                    // the number of messages to fetch this time
                    int lFetchCount = pBatchSizer.Current;

                    // get the UIDs to fetch this time
                    cUIntList lUIDs = new cUIntList();
                    while (lIndex < pUIDs.Count && lUIDs.Count < lFetchCount) lUIDs.Add(pUIDs[lIndex++].UID);

                    // fetch
                    lStopwatch.Restart();
                    await ZUIDFetchCacheItemsAsync(pMC, pMailboxHandle, lUIDValidity, lUIDs, pItems, pChangedSince, pVanished, lContext).ConfigureAwait(false);
                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    pBatchSizer.AddSample(lUIDs.Count, lStopwatch.ElapsedMilliseconds);

                    // update progress
                    pIncrementer.Increment(lUIDs.Count);
                }
            }
        }
    }
}