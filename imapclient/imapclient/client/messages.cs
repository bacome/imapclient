using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private fMessageProperties mDefaultMessageProperties = fMessageProperties.none;

        public fMessageProperties DefaultMessageProperties
        {
            get => mDefaultMessageProperties;

            set
            {
                if ((value & fMessageProperties.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mDefaultMessageProperties = value;
            }
        }

        public cSort DefaultMessageSort { get; set; } = null;

        private enum eMessageThreadAlgorithm { orderedsubject, references, refs }

        public List<cMessage> Messages(cMailboxId pMailboxId, cFilter pFilter, cSort pSort, fMessageProperties pProperties, cFetchControl pFC = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Messages));
            var lTask = ZMessagesAsync(pMailboxId, pFilter, pSort, pProperties, pFC, lContext);
            mEventSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<List<cMessage>> MessagesAsync(cMailboxId pMailboxId, cFilter pFilter, cSort pSort, fMessageProperties pProperties, cFetchControl pFC = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(MessagesAsync));
            return ZMessagesAsync(pMailboxId, pFilter, pSort, pProperties, pFC, lContext);
        }

        private async Task<List<cMessage>> ZMessagesAsync(cMailboxId pMailboxId, cFilter pFilter, cSort pSort, fMessageProperties pProperties, cFetchControl pFC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesAsync), pMailboxId, pFilter, pSort, pProperties, pFC);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || lSession.State != eState.selected) throw new cMailboxNotSelectedException(lContext);

            if (pMailboxId == null) throw new ArgumentNullException(nameof(pMailboxId));

            fMessageProperties lProperties = pProperties & fMessageProperties.allmask;
            if ((pProperties & fMessageProperties.clientdefault) != 0) lProperties |= mDefaultMessageProperties;

            cSort lSort;
            if (pSort == null) lSort = DefaultMessageSort;
            else lSort = pSort;

            var lCapability = lSession.Capability;

            cHandleList lHandles;

            mAsyncCounter.Increment(lContext);

            try
            {
                cFetchPropertiesMethodControl lMC;
                if (pFC == null) lMC = new cFetchPropertiesMethodControl(mTimeout, CancellationToken, null);
                else lMC = new cFetchPropertiesMethodControl(pFC.Timeout, pFC.CancellationToken, pFC.IncrementProgress);

                if (lSort != null)
                {
                    // may have to add to the properties to get the data we need to do the sort if we are doing the sort
                    if (ReferenceEquals(lSort, cSort.OrderedSubject))
                    {
                        if (lCapability.ThreadOrderedSubject) return await ZMessagesThreadAsync(lMC, lSession, pMailboxId, eMessageThreadAlgorithm.orderedsubject, pFilter, lProperties, lContext).ConfigureAwait(false);
                        lProperties |= fMessageProperties.envelope | fMessageProperties.received;
                    }
                    else if (ReferenceEquals(lSort, cSort.References))
                    {
                        if (lCapability.ThreadReferences) return await ZMessagesThreadAsync(lMC, lSession, pMailboxId, eMessageThreadAlgorithm.references, pFilter, lProperties, lContext).ConfigureAwait(false);
                        lProperties |= fMessageProperties.envelope | fMessageProperties.received | fMessageProperties.references;
                    }
                    else if (ReferenceEquals(lSort, cSort.Refs))
                    {
                        if (lCapability.ThreadRefs) return await ZMessagesThreadAsync(lMC, lSession, pMailboxId, eMessageThreadAlgorithm.refs, pFilter, lProperties, lContext).ConfigureAwait(false);
                        lProperties |= fMessageProperties.envelope | fMessageProperties.received | fMessageProperties.references;
                    }
                    else if (lSort.Items != null && lSort.Items.Count > 0)
                    {
                        bool lSortDisplay = false;
                        fMessageProperties lSortProperties = 0;

                        foreach (var lItem in lSort.Items)
                        {
                            switch (lItem.Type)
                            {
                                case cSortItem.eType.received:

                                    lSortProperties |= fMessageProperties.received;
                                    break;

                                case cSortItem.eType.cc:
                                case cSortItem.eType.from:
                                case cSortItem.eType.subject:
                                case cSortItem.eType.to:

                                    lSortProperties |= fMessageProperties.envelope;
                                    break;

                                case cSortItem.eType.sent:

                                    lSortProperties |= fMessageProperties.envelope | fMessageProperties.received;
                                    break;

                                case cSortItem.eType.size:

                                    lSortProperties |= fMessageProperties.size;
                                    break;

                                case cSortItem.eType.displayfrom:
                                case cSortItem.eType.displayto:

                                    lSortProperties |= fMessageProperties.envelope;
                                    lSortDisplay = true;
                                    break;

                                default:

                                    throw new cInternalErrorException(lContext);
                            }
                        }

                        if (!lSortDisplay && lCapability.Sort || lSortDisplay && lCapability.SortDisplay)
                        {
                            if (lCapability.ESort) lHandles = await lSession.SortExtendedAsync(lMC, pMailboxId, pFilter, lSort, lContext).ConfigureAwait(false);
                            else lHandles = await lSession.SortAsync(lMC, pMailboxId, pFilter, lSort, lContext).ConfigureAwait(false);
                            if (lProperties != 0 && lHandles.Count > 0) await lSession.FetchAsync(lMC, pMailboxId, lHandles, lProperties, lContext).ConfigureAwait(false);
                            return ZMessagesFlatMessageList(pMailboxId, lHandles, lContext);
                        }

                        lProperties |= lSortProperties;
                    }
                    else throw new cInternalErrorException(lContext);
                }

                if (lCapability.ESearch) lHandles = await lSession.SearchExtendedAsync(lMC, pMailboxId, pFilter, lContext).ConfigureAwait(false);
                else lHandles = await lSession.SearchAsync(lMC, pMailboxId, pFilter, lContext).ConfigureAwait(false);
                if (lProperties != 0 && lHandles.Count > 0) await lSession.FetchAsync(lMC, pMailboxId, lHandles, lProperties, lContext).ConfigureAwait(false);
                if (lSort == null) return ZMessagesFlatMessageList(pMailboxId, lHandles, lContext);
            }
            finally { mAsyncCounter.Decrement(lContext); }

            // client side sorting

            if (ReferenceEquals(lSort, cSort.OrderedSubject)) return ZMessagesThreadOrderedSubject(pMailboxId, lHandles, lContext);
            if (ReferenceEquals(lSort, cSort.References)) return ZMessagesThreadReferences(pMailboxId, lHandles, lContext);
            if (ReferenceEquals(lSort, cSort.Refs)) return ZMessagesThreadRefs(pMailboxId, lHandles, lContext);

            lHandles.Sort(lSort);
            return ZMessagesFlatMessageList(pMailboxId, lHandles, lContext);
        }

        private async Task<List<cMessage>> ZMessagesThreadAsync(cFetchPropertiesMethodControl pMC, cSession pSession, cMailboxId pMailboxId, eMessageThreadAlgorithm pAlgorithm, cFilter pFilter, fMessageProperties pProperties, cTrace.cContext pParentContext)
        {
            // this routine uses the thread command and then gets the properties of the returned messages (if required)
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadAsync), pMC, pMailboxId, pFilter, pProperties, pAlgorithm);
            throw new NotImplementedException();
            // TODO
            //  (this will call fetch if required)
            //  (note the UIDValidity check is required)
        }

        private List<cMessage> ZMessagesThreadOrderedSubject(cMailboxId pMailboxId, cHandleList pHandles, cTrace.cContext pParentContext)
        {
            // this routine is the client side implementation of the threading algorithm

            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadOrderedSubject), pHandles);

            bool lFirst;

            // sort
            pHandles.Sort(new cSort(cSortItem.Subject, cSortItem.Sent));

            // split into threads

            List<cHandleList> lThreads = new List<cHandleList>();
            cHandleList lCurrentThread = null;
            string lCurrentThreadBaseSubject = null;
            
            lFirst = true;

            foreach (var lHandle in pHandles)
            {
                string lBaseSubject = lHandle.Envelope.BaseSubject;

                if (lFirst || lBaseSubject != lCurrentThreadBaseSubject)
                {
                    lFirst = false;
                    lCurrentThread = new cHandleList();
                    lThreads.Add(lCurrentThread);
                    lCurrentThreadBaseSubject = lBaseSubject;
                }

                lCurrentThread.Add(lHandle);
            }

            // sort threads
            lThreads.Sort(ZCompareFirstMessageSentDate);

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
                        lResult.Add(new cMessage(this, pMailboxId, lHandle, 0));
                    }
                    else lResult.Add(new cMessage(this, pMailboxId, lHandle, 1));
                }
            }

            // done

            return lResult;
        }

        private static int ZCompareFirstMessageSentDate(cHandleList pX, cHandleList pY)
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

        private List<cMessage> ZMessagesThreadReferences(cMailboxId pMailboxId, cHandleList pHandles, cTrace.cContext pParentContext)
        {
            // this routine is the client side implementation of the threading algorithm
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadReferences), pHandles);
            throw new NotImplementedException();
            // TODO
        }

        private List<cMessage> ZMessagesThreadRefs(cMailboxId pMailboxId, cHandleList pHandles, cTrace.cContext pParentContext)
        {
            // this routine is the client side implementation of the threading algorithm
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesThreadRefs), pHandles);
            throw new NotImplementedException();
            // TODO
        }

        private List<cMessage> ZMessagesFlatMessageList(cMailboxId pMailboxId, cHandleList pHandles, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZMessagesFlatMessageList), pMailboxId, pHandles);
            var lResult = new List<cMessage>(pHandles.Count);
            foreach (var lHandle in pHandles) lResult.Add(new cMessage(this, pMailboxId, lHandle));
            return lResult;
        }
    }
}