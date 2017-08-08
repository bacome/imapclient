using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public cMessage Message(iMailboxHandle pHandle, cUID pUID, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Message));
            var lTask = ZUIDFetchAttributesAsync(pHandle, ZMessageUIDs(pUID), ZFetchAttributesRequired(pProperties | fMessageProperties.uid), null, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            var lResult = lTask.Result;
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        public async Task<cMessage> MessageAsync(iMailboxHandle pHandle, cUID pUID, fMessageProperties pProperties)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessageAsync));
            var lResult = await ZUIDFetchAttributesAsync(pHandle, ZMessageUIDs(pUID), ZFetchAttributesRequired(pProperties | fMessageProperties.uid), null, lContext).ConfigureAwait(false);
            if (lResult.Count == 0) return null;
            if (lResult.Count == 1) return lResult[0];
            throw new cInternalErrorException(lContext);
        }

        public List<cMessage> Messages(iMailboxHandle pHandle, IList<cUID> pUIDs, fMessageProperties pProperties, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZUIDFetchAttributesAsync(pHandle, ZMessageUIDs(pUIDs), ZFetchAttributesRequired(pProperties | fMessageProperties.uid), pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMessage>> MessagesAsync(iMailboxHandle pHandle, IList<cUID> pUIDs, fMessageProperties pProperties, cFetchControl pFC)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZUIDFetchAttributesAsync(pHandle, ZMessageUIDs(pUIDs), ZFetchAttributesRequired(pProperties | fMessageProperties.uid), pFC, lContext);
        }

        private cUIDList ZMessageUIDs(cUID pUID)
        {
            if (pUID == null) throw new ArgumentNullException(nameof(pUID));
            return new cUIDList(pUID);
        }

        private cUIDList ZMessageUIDs(IList<cUID> pUIDs)
        {
            if (pUIDs == null) throw new ArgumentNullException(nameof(pUIDs));

            uint? lUIDValidity = null;

            foreach (var lUID in pUIDs)
            {
                if (lUID == null) throw new ArgumentOutOfRangeException(nameof(pUIDs), "contains nulls");
                if (lUIDValidity == null) lUIDValidity = lUID.UIDValidity;
                else if (lUID.UIDValidity != lUIDValidity) throw new ArgumentOutOfRangeException(nameof(pUIDs), "contains mixed uidvalidities");
            }

            return new cUIDList(pUIDs);
        }

        public cSort DefaultMessageSort { get; set; } = null;

        private enum eMessageThreadAlgorithm { orderedsubject, references, refs }

        public List<cMessage> Messages(iMailboxHandle pHandle, cFilter pFilter, cSort pSort, fMessageProperties pProperties, cFetchControl pFC = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZMessagesAsync(pHandle, pFilter, pSort, pProperties, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMessage>> MessagesAsync(iMailboxHandle pHandle, cFilter pFilter, cSort pSort, fMessageProperties pProperties, cFetchControl pFC = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZMessagesAsync(pHandle, pFilter, pSort, pProperties, pFC, lContext);
        }

        private async Task<List<cMessage>> ZMessagesAsync(iMailboxHandle pHandle, cFilter pFilter, cSort pSort, fMessageProperties pProperties, cFetchControl pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesAsync), pHandle, pFilter, pSort, pProperties, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new InvalidOperationException();

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));

            cSort lSort;
            if (pSort == null) lSort = DefaultMessageSort;
            else lSort = pSort;

            var lCapability = lSession.Capability;

            cMessageHandleList lHandles;

            mAsyncCounter.Increment(lContext);

            try
            {
                cFetchAttributesMethodControl lMC;
                if (pFC == null) lMC = new cFetchAttributesMethodControl(mTimeout, CancellationToken, null);
                else lMC = new cFetchAttributesMethodControl(pFC.Timeout, pFC.CancellationToken, pFC.IncrementProgress);

                fMessageProperties lProperties;

                if (lSort == null) lProperties = pProperties;
                else
                {
                    // may have to add to the attributes to get the data we need to do the sort if we are doing the sort
                    if (ReferenceEquals(lSort, cSort.OrderedSubject))
                    {
                        if (lCapability.ThreadOrderedSubject) return await ZMessagesThreadAsync(lMC, lSession, pHandle, eMessageThreadAlgorithm.orderedsubject, pFilter, pProperties, lContext).ConfigureAwait(false);
                        lProperties = pProperties | fMessageProperties.basesubject | fMessageProperties.received;
                    }
                    else if (ReferenceEquals(lSort, cSort.References))
                    {
                        if (lCapability.ThreadReferences) return await ZMessagesThreadAsync(lMC, lSession, pHandle, eMessageThreadAlgorithm.references, pFilter, pProperties, lContext).ConfigureAwait(false);
                        lProperties = pProperties | fMessageProperties.subject | fMessageProperties.received | fMessageProperties.references;
                    }
                    else if (ReferenceEquals(lSort, cSort.Refs))
                    {
                        if (lCapability.ThreadRefs) return await ZMessagesThreadAsync(lMC, lSession, pHandle, eMessageThreadAlgorithm.refs, pFilter, pProperties, lContext).ConfigureAwait(false);
                        lProperties = pProperties | fMessageProperties.subject | fMessageProperties.received | fMessageProperties.references;
                    }
                    else if (lSort.Items != null && lSort.Items.Count > 0)
                    {
                        var lSortProperties = lSort.Properties(out var lSortDisplay);

                        if (!lSortDisplay && lCapability.Sort || lSortDisplay && lCapability.SortDisplay)
                        {
                            if (lCapability.ESort) lHandles = await lSession.SortExtendedAsync(lMC, pHandle, pFilter, lSort, lContext).ConfigureAwait(false);
                            else lHandles = await lSession.SortAsync(lMC, pHandle, pFilter, lSort, lContext).ConfigureAwait(false);

                            await ZMessagesFetchAsync(lMC, lSession, lHandles, pProperties, lContext).ConfigureAwait(false);

                            return ZMessagesFlatMessageList(lHandles, lContext);
                        }

                        lProperties = pProperties | lSortProperties;
                    }
                    else throw new cInternalErrorException(lContext);
                }

                if (lCapability.ESearch) lHandles = await lSession.SearchExtendedAsync(lMC, pHandle, pFilter, lContext).ConfigureAwait(false);
                else lHandles = await lSession.SearchAsync(lMC, pHandle, pFilter, lContext).ConfigureAwait(false);

                await ZMessagesFetchAsync(lMC, lSession, lHandles, lProperties, lContext).ConfigureAwait(false);

                if (lSort == null) return ZMessagesFlatMessageList(lHandles, lContext);
            }
            finally { mAsyncCounter.Decrement(lContext); }

            // client side sorting

            if (ReferenceEquals(lSort, cSort.OrderedSubject)) return ZMessagesThreadOrderedSubject(lHandles, lContext);
            if (ReferenceEquals(lSort, cSort.References)) return ZMessagesThreadReferences(lHandles, lContext);
            if (ReferenceEquals(lSort, cSort.Refs)) return ZMessagesThreadRefs(lHandles, lContext);

            lHandles.Sort(lSort);
            return ZMessagesFlatMessageList(lHandles, lContext);
        }

        private async Task<List<cMessage>> ZMessagesThreadAsync(cFetchAttributesMethodControl pMC, cSession pSession, iMailboxHandle pHandle, eMessageThreadAlgorithm pAlgorithm, cFilter pFilter, fMessageProperties pProperties, cTrace.cContext pParentContext)
        {
            // this routine uses the thread command and then gets the properties of the returned messages (if required)
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadAsync), pMC, pHandle, pFilter, pProperties, pAlgorithm);
            throw new NotImplementedException();
            // TODO
            //  (this will call fetch if required)
            //  (note the UIDValidity check is required)
        }

        private async Task ZMessagesFetchAsync(cFetchAttributesMethodControl pMC, cSession pSession, cMessageHandleList pHandles, fMessageProperties pProperties, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesFetchAsync), pMC, pHandles, pProperties);

            if (pHandles.Count == 0) return;

            var lRequired = ZFetchAttributesRequired(pProperties);
            if (lRequired == 0) return; // nothing to do

            var lToFetch = ZFetchAttributesToFetch(pHandles, lRequired);
            if (lToFetch == 0) return; // got everything already

            await pSession.FetchAttributesAsync(pMC, pHandles, lToFetch, lContext).ConfigureAwait(false);
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
        }

        private List<cMessage> ZMessagesThreadRefs(cMessageHandleList pHandles, cTrace.cContext pParentContext)
        {
            // this routine is the client side implementation of the threading algorithm
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadRefs), pHandles);
            throw new NotImplementedException();
            // TODO
        }

        private List<cMessage> ZMessagesFlatMessageList(cMessageHandleList pHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesFlatMessageList), pHandles);
            var lResult = new List<cMessage>(pHandles.Count);
            foreach (var lHandle in pHandles) lResult.Add(new cMessage(this, lHandle));
            return lResult;
        }
    }
}