﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cMessage Message(iMailboxHandle pHandle, cUID pUID, cMessageCacheItems pItems)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Message));
            var lTask = ZUIDFetchCacheItemsAsync(pHandle, cUIDList.FromUID(pUID), pItems, null, lContext);
            mSynchroniser.Wait(lTask, lContext);
            var lResult = lTask.Result;
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        internal async Task<cMessage> MessageAsync(iMailboxHandle pHandle, cUID pUID, cMessageCacheItems pItems)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessageAsync));
            var lResult = await ZUIDFetchCacheItemsAsync(pHandle, cUIDList.FromUID(pUID), pItems, null, lContext).ConfigureAwait(false);
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        internal List<cMessage> Messages(iMailboxHandle pHandle, IEnumerable<cUID> pUIDs, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZUIDFetchCacheItemsAsync(pHandle, cUIDList.FromUIDs(pUIDs), pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<List<cMessage>> MessagesAsync(iMailboxHandle pHandle, IEnumerable<cUID> pUIDs, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZUIDFetchCacheItemsAsync(pHandle, cUIDList.FromUIDs(pUIDs), pItems, pConfiguration, lContext);
        }

        internal List<cMessage> Messages(iMailboxHandle pHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZMessagesAsync(pHandle, pFilter, pSort, pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<List<cMessage>> MessagesAsync(iMailboxHandle pHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZMessagesAsync(pHandle, pFilter, pSort, pItems, pConfiguration, lContext);
        }

        private async Task<List<cMessage>> ZMessagesAsync(iMailboxHandle pHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesAsync), pHandle, pFilter, pSort, pItems);

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    return await ZZMessagesAsync(lMC, pHandle, pFilter, pSort, pItems, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await ZZMessagesAsync(lMC, pHandle, pFilter, pSort, pItems, pConfiguration, lContext).ConfigureAwait(false);
            }
        }

        private async Task<List<cMessage>> ZZMessagesAsync(cMethodControl pMC, iMailboxHandle pHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMessagesAsync), pMC, pHandle, pFilter, pSort, pItems);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pFilter == null) throw new ArgumentNullException(nameof(pFilter));
            if (pSort == null) throw new ArgumentNullException(nameof(pSort));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            cMessageCacheItems lItems;

            if (ReferenceEquals(pSort, cSort.None)) lItems = pItems;
            /* de-implemented pending a requirement to complete it
            else if (ReferenceEquals(pSort, cSort.ThreadOrderedSubject))
            {
                if (lSession.Capabilities.ThreadOrderedSubject) return await ZMessagesThreadAsync(pMC, lSession, pHandle, eMessageThreadAlgorithm.orderedsubject, pFilter, pItems, pConfiguration, lContext).ConfigureAwait(false);
                lItems = new cCacheItems(pItems.Attributes | fCacheAttributes.envelope | fCacheAttributes.received, pItems.Names);
            }
            else if (ReferenceEquals(pSort, cSort.ThreadReferences))
            {
                if (lSession.Capabilities.ThreadReferences) return await ZMessagesThreadAsync(pMC, lSession, pHandle, eMessageThreadAlgorithm.references, pFilter, pItems, pConfiguration, lContext).ConfigureAwait(false);
                lItems = new cCacheItems(pItems.Attributes | fCacheAttributes.envelope | fCacheAttributes.received, pItems.Names.Union(cHeaderFieldNames.References));
            } */
            else
            {
                var lSortAttributes = pSort.Attributes(out var lSortDisplay);
                if (!lSortDisplay && lSession.Capabilities.Sort || lSortDisplay && lSession.Capabilities.SortDisplay) return await ZMessagesSortAsync(pMC, lSession, pHandle, pSort, pFilter, pItems, pConfiguration, lContext).ConfigureAwait(false);
                lItems = new cMessageCacheItems(pItems.Attributes | lSortAttributes, pItems.Names);
            }

            cMessageHandleList lHandles;
            if (lSession.Capabilities.ESearch) lHandles = await lSession.SearchExtendedAsync(pMC, pHandle, pFilter, lContext).ConfigureAwait(false);
            else lHandles = await lSession.SearchAsync(pMC, pHandle, pFilter, lContext).ConfigureAwait(false);

            // get the properties
            await ZMessagesFetchAsync(pMC, lSession, lHandles, lItems, pConfiguration, lContext).ConfigureAwait(false);

            if (ReferenceEquals(pSort, cSort.None)) return ZMessagesFlatMessageList(lHandles, lContext);

            // client side sorting

            // de-implemented pending a requirement to complete it
            // if (ReferenceEquals(pSort, cSort.ThreadOrderedSubject)) return ZMessagesThreadOrderedSubject(lHandles, lContext);
            // if (ReferenceEquals(pSort, cSort.ThreadReferences)) return ZMessagesThreadReferences(lHandles, lContext);

            lHandles.Sort(pSort);
            return ZMessagesFlatMessageList(lHandles, lContext);
        }

        private async Task<List<cMessage>> ZMessagesSortAsync(cMethodControl pMC, cSession pSession, iMailboxHandle pHandle, cSort pSort, cFilter pFilter, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesSortAsync), pMC, pHandle, pSort, pFilter, pItems);

            cMessageHandleList lHandles;
            if (pSession.Capabilities.ESort) lHandles = await pSession.SortExtendedAsync(pMC, pHandle, pFilter, pSort, lContext).ConfigureAwait(false);
            else lHandles = await pSession.SortAsync(pMC, pHandle, pFilter, pSort, lContext).ConfigureAwait(false);

            await ZMessagesFetchAsync(pMC, pSession, lHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);

            return ZMessagesFlatMessageList(lHandles, lContext);
        }

        private async Task ZMessagesFetchAsync(cMethodControl pMC, cSession pSession, cMessageHandleList pHandles, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesFetchAsync), pMC, pHandles, pItems);

            if (pHandles.Count == 0) return;
            if (pItems.IsNone) return;

            if (pHandles.TrueForAll(h => h.Contains(pItems))) return;

            cProgress lProgress;

            if (pConfiguration == null) lProgress = new cProgress();
            else
            {
                mSynchroniser.InvokeActionInt(pConfiguration.SetCount, pHandles.Count, lContext);
                lProgress = new cProgress(mSynchroniser, pConfiguration.Increment);
            }

            await pSession.FetchCacheItemsAsync(pMC, pHandles, pItems, lProgress, lContext).ConfigureAwait(false);
        }

        private List<cMessage> ZMessagesFlatMessageList(cMessageHandleList pHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesFlatMessageList), pHandles);
            var lResult = new List<cMessage>(pHandles.Count);
            foreach (var lHandle in pHandles) lResult.Add(new cMessage(this, lHandle));
            return lResult;
        }

        /* de-implemented pending a requirement to complete it 
         
        private enum eMessageThreadAlgorithm { orderedsubject, references }

        private async Task<List<cMessage>> ZMessagesThreadAsync(cMethodControl pMC, cSession pSession, iMailboxHandle pHandle, eMessageThreadAlgorithm pAlgorithm, cFilter pFilter, cCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            // this routine uses the thread command and then gets the properties of the returned messages (if required)
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadAsync), pMC, pHandle, pAlgorithm, pFilter, pItems);
            throw new NotImplementedException();
            // TODO
            //  (this will call fetch if required)
            //  (note the UIDValidity check is required)
        } 

        private static readonly cSort kMessagesThreadOrderedSubjectSort = new cSort(cSortItem.Subject, cSortItem.Sent);

        private List<cMessage> ZMessagesThreadOrderedSubject(cMessageHandleList pHandles, cTrace.cContext pParentContext)
        {
            // this routine is the client side implementation of the threading algorithm

            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadOrderedSubject), pHandles);

            bool lFirst;

            // sort
            pHandles.Sort(kMessagesThreadOrderedSubjectSort);

            // split into threads

            List<cMessageHandleList> lThreads = new List<cMessageHandleList>();
            cMessageHandleList lCurrentThread = null;
            string lCurrentThreadBaseSubject = null;
            
            lFirst = true;

            foreach (var lHandle in pHandles)
            {
                string lBaseSubject = lHandle.Envelope.BaseSubject;

                if (lFirst || lBaseSubject != lCurrentThreadBaseSubject)
                {
                    lFirst = false;
                    lCurrentThread = new cMessageHandleList();
                    lThreads.Add(lCurrentThread);
                    lCurrentThreadBaseSubject = lBaseSubject;
                }

                lCurrentThread.Add(lHandle);
            }

            // sort threads
            lThreads.Sort(ZMessagesThreadOrderedSubjectCompare);

            // generate result

            var lResult = new List<cMessage>(pHandles.Count);

            foreach (var lThread in lThreads)
            {
                lFirst = true;

                foreach (var lHandle in lThread)
                {
                    if (lFirst)
                    {
                        lFirst = false;
                        lResult.Add(new cMessage(this, lHandle, 0));
                    }
                    else lResult.Add(new cMessage(this, lHandle, 1));
                }
            }

            // done

            return lResult;
        }

        private static int ZMessagesThreadOrderedSubjectCompare(cMessageHandleList pX, cMessageHandleList pY)
        {
            var lHandleX = pX[0];
            var lHandleY = pY[0];

            var lSentX = lHandleX.Envelope?.Sent ?? lHandleX.Received;
            var lSentY = lHandleY.Envelope?.Sent ?? lHandleY.Received;

            if (lSentX == null)
            {
                if (lSentY == null) return lHandleX.CacheSequence.CompareTo(lHandleY.CacheSequence);
                return -1;
            }

            if (lSentY == null) return 1;

            int lCompareTo = lSentX.Value.CompareTo(lSentY.Value);
            if (lCompareTo == 0) return lHandleX.CacheSequence.CompareTo(lHandleY.CacheSequence);
            return lCompareTo;
        }

        private List<cMessage> ZMessagesThreadReferences(cMessageHandleList pHandles, cTrace.cContext pParentContext)
        {
            // this routine is the client side implementation of the threading algorithm
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadReferences), pHandles);
            throw new NotImplementedException();
            // TODO
        } */
    }
}