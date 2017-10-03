using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        public enum eStoreOperation { add, remove, replace }

        // true means ok, false falied
        public bool Store(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pUnchangedSinceModSeq = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Fetch));

            if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            if (pHandle.Contains(pItems)) return true;

            var lTask = ZFetchCacheItemsAsync(cMessageHandleList.FromHandle(pHandle), pItems, null, lContext);
            mSynchroniser.Wait(lTask, lContext);

            return pHandle.Contains(pItems);
        }

        // returns the list of not updated messages  ... NOTE: UID store would return a uintlist.
        //  note that the non-updated may be because they are expunged OR because they failed the unchangedsince test
        //   
        //  what about OK/No and MODIFIED
        public cMessageHandleList Store(IList<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pUnchangedSinceModSeq = null)
        {

        }

        public async Task StoreAsync(iMessageHandle pHandle, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pUnchangedSinceModSeq = null)
        {

        }

        public async Task StoreAsync(IList<iMessageHandle> pHandles, eStoreOperation pOperation, cSettableFlags pFlags, ulong? pUnchangedSinceModSeq = null)
        {

        }
    }
}