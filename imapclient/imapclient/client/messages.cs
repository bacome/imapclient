using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cMessage Message(iMailboxHandle pMailboxHandle, cUID pUID, cMessageCacheItems pItems)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Message));
            var lTask = ZUIDFetchCacheItemsAsync(pMailboxHandle, cUIDList.FromUID(pUID), pItems, null, lContext);
            mSynchroniser.Wait(lTask, lContext);
            var lResult = lTask.Result;
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        internal async Task<cMessage> MessageAsync(iMailboxHandle pMailboxHandle, cUID pUID, cMessageCacheItems pItems)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessageAsync));
            var lResult = await ZUIDFetchCacheItemsAsync(pMailboxHandle, cUIDList.FromUID(pUID), pItems, null, lContext).ConfigureAwait(false);
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        internal List<cMessage> Messages(iMailboxHandle pMailboxHandle, IEnumerable<cUID> pUIDs, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZUIDFetchCacheItemsAsync(pMailboxHandle, cUIDList.FromUIDs(pUIDs), pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<List<cMessage>> MessagesAsync(iMailboxHandle pMailboxHandle, IEnumerable<cUID> pUIDs, cMessageCacheItems pItems, cCacheItemFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZUIDFetchCacheItemsAsync(pMailboxHandle, cUIDList.FromUIDs(pUIDs), pItems, pConfiguration, lContext);
        }

        internal List<cMessage> Messages(iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZMessagesAsync(pMailboxHandle, pFilter, pSort, pItems, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<List<cMessage>> MessagesAsync(iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZMessagesAsync(pMailboxHandle, pFilter, pSort, pItems, pConfiguration, lContext);
        }

        private async Task<List<cMessage>> ZMessagesAsync(iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesAsync), pMailboxHandle, pFilter, pSort, pItems);

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    return await ZZMessagesAsync(lMC, pMailboxHandle, pFilter, pSort, pItems, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await ZZMessagesAsync(lMC, pMailboxHandle, pFilter, pSort, pItems, pConfiguration, lContext).ConfigureAwait(false);
            }
        }

        private async Task<List<cMessage>> ZZMessagesAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cFilter pFilter, cSort pSort, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZMessagesAsync), pMC, pMailboxHandle, pFilter, pSort, pItems);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotSelected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
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
                if (!lSortDisplay && lSession.Capabilities.Sort || lSortDisplay && lSession.Capabilities.SortDisplay) return await ZMessagesSortAsync(pMC, lSession, pMailboxHandle, pSort, pFilter, pItems, pConfiguration, lContext).ConfigureAwait(false);
                lItems = new cMessageCacheItems(pItems.Attributes | lSortAttributes, pItems.Names);
            }

            cMessageHandleList lMessageHandles;
            if (lSession.Capabilities.ESearch) lMessageHandles = await lSession.SearchExtendedAsync(pMC, pMailboxHandle, pFilter, lContext).ConfigureAwait(false);
            else lMessageHandles = await lSession.SearchAsync(pMC, pMailboxHandle, pFilter, lContext).ConfigureAwait(false);

            // get the properties
            await ZMessagesFetchAsync(pMC, lSession, lMessageHandles, lItems, pConfiguration, lContext).ConfigureAwait(false);

            if (ReferenceEquals(pSort, cSort.None)) return ZMessagesFlatMessageList(lMessageHandles, lContext);

            // client side sorting

            // de-implemented pending a requirement to complete it
            // if (ReferenceEquals(pSort, cSort.ThreadOrderedSubject)) return ZMessagesThreadOrderedSubject(lHandles, lContext);
            // if (ReferenceEquals(pSort, cSort.ThreadReferences)) return ZMessagesThreadReferences(lHandles, lContext);

            lMessageHandles.Sort(pSort);
            return ZMessagesFlatMessageList(lMessageHandles, lContext);
        }

        private async Task<List<cMessage>> ZMessagesSortAsync(cMethodControl pMC, cSession pSession, iMailboxHandle pMailboxHandle, cSort pSort, cFilter pFilter, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesSortAsync), pMC, pMailboxHandle, pSort, pFilter, pItems);

            cMessageHandleList lMessageHandles;
            if (pSession.Capabilities.ESort) lMessageHandles = await pSession.SortExtendedAsync(pMC, pMailboxHandle, pFilter, pSort, lContext).ConfigureAwait(false);
            else lMessageHandles = await pSession.SortAsync(pMC, pMailboxHandle, pFilter, pSort, lContext).ConfigureAwait(false);

            await ZMessagesFetchAsync(pMC, pSession, lMessageHandles, pItems, pConfiguration, lContext).ConfigureAwait(false);

            return ZMessagesFlatMessageList(lMessageHandles, lContext);
        }

        private async Task ZMessagesFetchAsync(cMethodControl pMC, cSession pSession, cMessageHandleList pMessageHandles, cMessageCacheItems pItems, cMessageFetchConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesFetchAsync), pMC, pMessageHandles, pItems);

            if (pMessageHandles.Count == 0) return;
            if (pItems.IsEmpty) return;

            if (pMessageHandles.TrueForAll(h => h.Contains(pItems))) return;

            Action<int> lIncrement;

            if (pConfiguration == null) lIncrement = null;
            else
            {
                mSynchroniser.InvokeActionInt(pConfiguration.SetMaximum, pMessageHandles.Count, lContext);
                lIncrement = pConfiguration.Increment;
            }

            await pSession.FetchCacheItemsAsync(pMC, pMessageHandles, pItems, lIncrement, lContext).ConfigureAwait(false);
        }

        private List<cMessage> ZMessagesFlatMessageList(cMessageHandleList pMessageHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesFlatMessageList), pMessageHandles);
            var lResult = new List<cMessage>(pMessageHandles.Count);
            foreach (var lMessageHandle in pMessageHandles) lResult.Add(new cMessage(this, lMessageHandle));
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