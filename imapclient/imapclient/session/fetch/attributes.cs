using System;
using System.Collections.Generic;
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
            public async Task FetchAttributesAsync(cFetchAttributesMethodControl pMC, cMessageHandleList pHandles, fFetchAttributes pAttributes, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(FetchAttributesAsync), pMC, pHandles, pAttributes);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                mMailboxCache.CheckInSelectedMailbox(pHandles); // to be repeated inside the select lock

                // always get the flags and modseq together
                var lAttributes = ZFetchAttributes(pAttributes, lContext);

                // split the handles into groups based on what attributes need to be retrieved, for each group do the retrieval
                foreach (var lGroup in ZFetchAttributesGroups(pHandles, lAttributes)) await ZFetchAttributesAsync(pMC, lGroup, lContext).ConfigureAwait(false);
            }

            private fFetchAttributes ZFetchAttributes(fFetchAttributes pAttributes, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchAttributes), pAttributes);

                if ((pAttributes & (fFetchAttributes.flags | fFetchAttributes.modseq)) == 0) return pAttributes;

                bool lNoModSeq = mMailboxCache.SelectedMailbox?.NoModSeq ?? true;

                if ((pAttributes & fFetchAttributes.modseq) != 0)
                {
                    if (lNoModSeq) throw new cUnsupportedByMailboxException(fCapabilities.CondStore, lContext);
                    return pAttributes | fFetchAttributes.flags;
                }

                if (!lNoModSeq) return pAttributes | fFetchAttributes.modseq;

                return pAttributes;
            }

            private async Task ZFetchAttributesAsync(cFetchAttributesMethodControl pMC, cFetchAttributesGroup pGroup, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZFetchAttributesAsync), pMC, pGroup);

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

                        cMessageHandleList lHandles = new cMessageHandleList();

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
                            await ZFetchAttributesAsync(pMC, lHandles, pGroup.Attributes, lContext).ConfigureAwait(false);
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
                var lMailboxHandle = pGroup.Handles[0].Cache.MailboxHandle;
                await ZUIDFetchAttributesAsync(pMC, lMailboxHandle, lUIDs, pGroup.Attributes, lContext).ConfigureAwait(false);
            }

            private IEnumerable<cFetchAttributesGroup> ZFetchAttributesGroups(cMessageHandleList pHandles, fFetchAttributes pAttributes)
            {
                Dictionary<fFetchAttributes, cFetchAttributesGroup> lGroups = new Dictionary<fFetchAttributes, cFetchAttributesGroup>();

                foreach (var lHandle in pHandles)
                {
                    fFetchAttributes lAttributes = ~lHandle.Attributes & pAttributes;

                    cFetchAttributesGroup lGroup;

                    if (!lGroups.TryGetValue(lAttributes, out lGroup))
                    {
                        lGroup = new cFetchAttributesGroup(lAttributes);
                        lGroups.Add(lAttributes, lGroup);
                    }

                    if (lHandle.UID == null) lGroup.MSNHandleCount++;

                    lGroup.Handles.Add(lHandle);
                }

                return lGroups.Values;
            }

            private class cFetchAttributesGroup
            {
                public readonly fFetchAttributes Attributes;
                public int MSNHandleCount = 0;
                public readonly cMessageHandleList Handles = new cMessageHandleList();

                public cFetchAttributesGroup(fFetchAttributes pAttributes) { Attributes = pAttributes; }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cFetchAttributesGroup));
                    lBuilder.Append(Attributes);
                    lBuilder.Append(MSNHandleCount);
                    lBuilder.Append(Handles);
                    return lBuilder.ToString();
                }
            }
        }
    }
}