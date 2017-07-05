using System;
using System.Collections.Generic;
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
            public async Task FetchAsync(cFetchAttributesMethodControl pMC, cMailboxId pMailboxId, cHandleList pHandles, fFetchAttributes pAttributes, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchAsync), pMC, pMailboxId, pHandles, pAttributes);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                // split the handles into groups based on what attributes need to be retrieved, for each group do the retrieval
                foreach (var lGroup in ZFetchGroups(pHandles, pAttributes)) await ZFetchAsync(pMC, pMailboxId, lGroup, lContext).ConfigureAwait(false);
            }

            private async Task ZFetchAsync(cFetchAttributesMethodControl pMC, cMailboxId pMailboxId, cFetchGroup pGroup, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchAsync), pMC, pMailboxId, pGroup);

                if (pGroup.Attributes == 0) return; // the group where we already have everything that we need

                cUIDList lUIDs = new cUIDList();

                if (pGroup.MSNHandleCount == 0) foreach (var lHandle in pGroup.Handles) lUIDs.Add(lHandle.UID);
                else
                {
                    // this is where we use straight fetch (not UID fetch)
                    //////////////////////////////////////////////////////

                    // sort the handles so we might get good sequence sets
                    pGroup.Handles.SortByCacheSequence();

                    int lMSNHandleCount = pGroup.MSNHandleCount;
                    int lIndex = 0;

                    while (lIndex < pGroup.Handles.Count && lMSNHandleCount != 0)
                    {
                        // the number of messages to fetch this time
                        int lFetchCount = mFetchAttributesSizer.Current;

                        // the number of UID handles we need to fetch to top up the number of handles to the limit
                        //
                        int lUIDHandleCount;
                        if (lFetchCount > lMSNHandleCount) lUIDHandleCount = lFetchCount - lMSNHandleCount;
                        else lUIDHandleCount = 0;

                        // get the handles to fetch this time

                        cHandleList lHandles = new cHandleList();

                        while (lIndex < pGroup.Handles.Count && lHandles.Count < lFetchCount)
                        {
                            var lHandle = pGroup.Handles[lIndex++];

                            if (lHandle.UID == null)
                            {
                                lHandles.Add(lHandle);
                                lMSNHandleCount--;
                            }
                            else if (lUIDHandleCount > 0)
                            {
                                lHandles.Add(lHandle);
                                lUIDHandleCount--;
                            }
                            else lUIDs.Add(lHandle.UID);
                        }

                        // if other fetching is occurring at the same time (retrieving UIDs) then there mightn't be any
                        //
                        if (lHandles.Count > 0)
                        {
                            // fetch
                            Stopwatch lStopwatch = Stopwatch.StartNew();
                            await ZFetchAsync(pMC, pMailboxId, lHandles, pGroup.Attributes, lContext).ConfigureAwait(false);
                            lStopwatch.Stop();

                            // store the time taken so the next fetch is a better size
                            mFetchAttributesSizer.AddSample(lHandles.Count, lStopwatch.ElapsedMilliseconds, lContext);
                            pMC.IncrementProgress(lHandles.Count);
                        }
                    }

                    while (lIndex < pGroup.Handles.Count) lUIDs.Add(pGroup.Handles[lIndex++].UID);
                    if (lUIDs.Count == 0) return;
                }

                // uid fetch the remainder
                await ZUIDFetchAsync(pMC, pMailboxId, lUIDs, pGroup.Attributes, lContext).ConfigureAwait(false);
            }

            private IEnumerable<cFetchGroup> ZFetchGroups(cHandleList pHandles, fFetchAttributes pAttributes)
            {
                Dictionary<fFetchAttributes, cFetchGroup> lGroups = new Dictionary<fFetchAttributes, cFetchGroup>();

                foreach (var lHandle in pHandles)
                {
                    fFetchAttributes lAttributes = ~lHandle.Attributes & pAttributes;

                    cFetchGroup lGroup;

                    if (!lGroups.TryGetValue(lAttributes, out lGroup))
                    {
                        lGroup = new cFetchGroup(lAttributes);
                        lGroups.Add(lAttributes, lGroup);
                    }

                    if (lHandle.UID == null) lGroup.MSNHandleCount++;

                    lGroup.Handles.Add(lHandle);
                }

                return lGroups.Values;
            }

            private class cFetchGroup
            {
                public readonly fFetchAttributes Attributes;
                public int MSNHandleCount = 0;
                public readonly cHandleList Handles = new cHandleList();

                public cFetchGroup(fFetchAttributes pAttributes) { Attributes = pAttributes; }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cFetchGroup));
                    lBuilder.Append(Attributes);
                    lBuilder.Append(MSNHandleCount);
                    lBuilder.Append(Handles);
                    return lBuilder.ToString();
                }
            }
        }
    }
}