using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task FetchCacheItemsAsync(cMethodControl pMC, cMessageHandleList pMessageHandles, cMessageCacheItems pItems, Action<int> pIncrement, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchCacheItemsAsync), pMC, pMessageHandles, pItems);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

                if (pMessageHandles == null) throw new ArgumentNullException(nameof(pMessageHandles));
                if (pItems == null) throw new ArgumentNullException(nameof(pItems));

                mMailboxCache.CheckInSelectedMailbox(pMessageHandles); // to be repeated inside the select lock

                // split the handles into groups based on what attributes need to be retrieved, for each group do the retrieval
                foreach (var lGroup in ZFetchCacheItemsGroups(pMessageHandles, pItems)) await ZFetchCacheItemsAsync(pMC, lGroup, pIncrement, lContext).ConfigureAwait(false);
            }

            private async Task ZFetchCacheItemsAsync(cMethodControl pMC, cFetchCacheItemsGroup pGroup, Action<int> pIncrement, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchCacheItemsAsync), pMC, pGroup);

                if (pGroup.Items.IsEmpty)
                {
                    // the group where we already have everything that we need
                    mSynchroniser.InvokeActionInt(pIncrement, pGroup.MessageHandles.Count, lContext);
                    return; 
                }

                int lExpungedCount;
                int lIndex = 0;
                cUIDList lUIDs = new cUIDList();

                if (pGroup.MSNHandleCount > 0)
                {
                    // this is where we use straight fetch (not UID fetch)
                    //////////////////////////////////////////////////////

                    // sort the handles so we might get good sequence sets
                    pGroup.MessageHandles.SortByCacheSequence();

                    int lMSNHandleCount = pGroup.MSNHandleCount;
                    Stopwatch lStopwatch = new Stopwatch();

                    while (lIndex < pGroup.MessageHandles.Count && lMSNHandleCount != 0)
                    {
                        // the number of messages to fetch this time
                        int lFetchCount = mFetchCacheItemsSizer.Current;

                        // the number of UID handles we need to fetch to top up the number of handles to the limit
                        //
                        int lUIDHandleCount;
                        if (lFetchCount > lMSNHandleCount) lUIDHandleCount = lFetchCount - lMSNHandleCount;
                        else lUIDHandleCount = 0;

                        // get the handles to fetch this time

                        lExpungedCount = 0;
                        cMessageHandleList lMessageHandles = new cMessageHandleList();

                        while (lIndex < pGroup.MessageHandles.Count && lMessageHandles.Count < lFetchCount)
                        {
                            var lMessageHandle = pGroup.MessageHandles[lIndex++];

                            if (lMessageHandle.Expunged) lExpungedCount++;
                            else
                            {
                                if (lMessageHandle.UID == null)
                                {
                                    lMessageHandles.Add(lMessageHandle);
                                    lMSNHandleCount--;
                                }
                                else if (lUIDHandleCount > 0)
                                {
                                    lMessageHandles.Add(lMessageHandle);
                                    lUIDHandleCount--;
                                }
                                else lUIDs.Add(lMessageHandle.UID);
                            }
                        }

                        // if other fetching is occurring at the same time (retrieving UIDs) then there mightn't be any
                        //
                        if (lMessageHandles.Count > 0)
                        {
                            // fetch
                            lStopwatch.Restart();
                            await ZFetchCacheItemsAsync(pMC, lMessageHandles, pGroup.Items, lContext).ConfigureAwait(false);
                            lStopwatch.Stop();

                            // store the time taken so the next fetch is a better size
                            mFetchCacheItemsSizer.AddSample(lMessageHandles.Count, lStopwatch.ElapsedMilliseconds);
                        }

                        // update progress
                        if (lExpungedCount > 0 || lMessageHandles.Count > 0) mSynchroniser.InvokeActionInt(pIncrement, lExpungedCount + lMessageHandles.Count, lContext);
                    }
                }

                lExpungedCount = 0;

                while (lIndex < pGroup.MessageHandles.Count)
                {
                    var lMessageHandle = pGroup.MessageHandles[lIndex++];
                    if (lMessageHandle.Expunged) lExpungedCount++;
                    else lUIDs.Add(lMessageHandle.UID);
                }

                if (lExpungedCount > 0) mSynchroniser.InvokeActionInt(pIncrement, lExpungedCount, lContext);

                if (lUIDs.Count == 0) return;

                // uid fetch the remainder
                var lMailboxHandle = pGroup.MessageHandles[0].MessageCache.MailboxHandle;
                await ZUIDFetchCacheItemsAsync(pMC, lMailboxHandle, lUIDs, pGroup.Items, pIncrement, lContext).ConfigureAwait(false);
            }

            private IEnumerable<cFetchCacheItemsGroup> ZFetchCacheItemsGroups(cMessageHandleList pMessageHandles, cMessageCacheItems pItems)
            {
                Dictionary<cMessageCacheItems, cFetchCacheItemsGroup> lGroups = new Dictionary<cMessageCacheItems, cFetchCacheItemsGroup>();

                foreach (var lMessageHandle in pMessageHandles)
                {
                    cMessageCacheItems lItems;

                    if (lMessageHandle.Expunged) lItems = cMessageCacheItems.Empty; // none to get
                    else lItems = lMessageHandle.Missing(pItems); // might also be none to get

                    cFetchCacheItemsGroup lGroup;

                    if (!lGroups.TryGetValue(lItems, out lGroup))
                    {
                        lGroup = new cFetchCacheItemsGroup(lItems);
                        lGroups.Add(lItems, lGroup);
                    }

                    if (lMessageHandle.UID == null) lGroup.MSNHandleCount++;

                    lGroup.MessageHandles.Add(lMessageHandle);
                }

                return lGroups.Values;
            }

            private class cFetchCacheItemsGroup
            {
                public readonly cMessageCacheItems Items;
                public int MSNHandleCount = 0;
                public readonly cMessageHandleList MessageHandles = new cMessageHandleList();

                public cFetchCacheItemsGroup(cMessageCacheItems pItems) { Items = pItems ?? throw new ArgumentNullException(nameof(pItems)); }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cFetchCacheItemsGroup));
                    lBuilder.Append(Items);
                    lBuilder.Append(MSNHandleCount);
                    lBuilder.Append(MessageHandles);
                    return lBuilder.ToString();
                }
            }
        }
    }
}