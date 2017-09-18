using System;
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
            public async Task<cMessageHandleList> UIDFetchAttributesAsync(cMethodControl pMC, iMailboxHandle pHandle, cUIDList pUIDs, cFetchAttributes pAttributes, cProgress pProgress, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(UIDFetchAttributesAsync), pMC, pHandle, pUIDs, pAttributes);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));
                if (pAttributes == null) throw new ArgumentNullException(nameof(pAttributes));
                if (pProgress == null) throw new ArgumentNullException(nameof(pProgress));

                if (pUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pUIDs));
                if (pAttributes.IsNone) throw new ArgumentOutOfRangeException(nameof(pAttributes));

                uint lUIDValidity = pUIDs[0].UIDValidity;

                mMailboxCache.CheckIsSelectedMailbox(pHandle, lUIDValidity); // to be repeated inside the select lock

                // split the list into those messages I have handles for and those I dont
                /////////////////////////////////////////////////////////////////////////

                cMessageHandleList lHandles = new cMessageHandleList();
                cUIDList lUIDs = new cUIDList();

                // check the selected mailbox and resolve uids -> handles whilst blocking select exclusive access
                //
                using (var lBlock = await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false))
                {
                    cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, lUIDValidity);

                    foreach (var lUID in pUIDs)
                    {
                        var lHandle = lSelectedMailbox.GetHandle(lUID);
                        if (lHandle == null) lUIDs.Add(lUID); // don't have a handle
                        if (lHandle.ContainsNone(pAttributes)) lUIDs.Add(lUID); // have to get all the attributes, may as well fetch them with the ones where I might need all the attributes
                        else lHandles.Add(lHandle);
                    }
                }

                // for the messages I have handles for, fetch the missing attributes
                ////////////////////////////////////////////////////////////////////

                if (lHandles.Count > 0)
                {
                    // split the handles into groups based on what attributes need to be retrieved, for each group do the retrieval
                    foreach (var lGroup in ZFetchAttributesGroups(lHandles, pAttributes)) await ZFetchAttributesAsync(pMC, lGroup, pProgress, lContext).ConfigureAwait(false);
                }

                // for the messages only identified by UID or where I have to get all the attributes
                ////////////////////////////////////////////////////////////////////////////////////

                if (lUIDs.Count > 0)
                {
                    await ZUIDFetchAttributesAsync(pMC, pHandle, lUIDs, pAttributes, pProgress, lContext).ConfigureAwait(false);

                    // resolve uids -> handles whilst blocking select exclusive access
                    //
                    using (var lBlock = await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false))
                    {
                        cSelectedMailbox lSelectedMailbox = mMailboxCache.CheckIsSelectedMailbox(pHandle, lUIDValidity);

                        foreach (var lUID in lUIDs)
                        {
                            var lHandle = lSelectedMailbox.GetHandle(lUID);
                            if (lHandle != null) lHandles.Add(lHandle);
                        }
                    }
                }

                return lHandles;
            }

            private async Task ZUIDFetchAttributesAsync(cMethodControl pMC, iMailboxHandle pHandle, cUIDList pUIDs, cFetchAttributes pAttributes, cProgress pProgress, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZUIDFetchAttributesAsync), pMC, pHandle, pUIDs, pAttributes);

                // get the UIDValidity
                uint lUIDValidity = pUIDs[0].UIDValidity;

                // sort the uids so we might get good sequence sets
                pUIDs.Sort();

                int lIndex = 0;

                while (lIndex < pUIDs.Count)
                {
                    // the number of messages to fetch this time
                    int lFetchCount = mFetchAttributesReadSizer.Current;

                    // get the UIDs to fetch this time
                    cUIntList lUIDs = new cUIntList();
                    while (lIndex < pUIDs.Count && lUIDs.Count < lFetchCount) lUIDs.Add(pUIDs[lIndex++].UID);

                    // fetch
                    Stopwatch lStopwatch = Stopwatch.StartNew();
                    await ZUIDFetchAttributesAsync(pMC, pHandle, lUIDValidity, lUIDs, pAttributes, lContext).ConfigureAwait(false);
                    lStopwatch.Stop();

                    // store the time taken so the next fetch is a better size
                    mFetchAttributesReadSizer.AddSample(lUIDs.Count, lStopwatch.ElapsedMilliseconds, lContext);

                    // update progress
                    pProgress.Increment(lUIDs.Count, lContext);
                }
            }
        }
    }
}