using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public abstract class cFlagCache : cPersistentCacheComponent
    {
        public readonly string InstanceName;
        protected readonly cTrace.cContext mRootContext;

        protected cFlagCache(string pInstanceName)
        {
            InstanceName = pInstanceName ?? throw new ArgumentNullException(nameof(pInstanceName));
            mRootContext = cMailClient.Trace.NewRoot(pInstanceName);
        }

        // comment back in later(commented out to stop me using the wrong ones) public bool TryGetModSeqFlags(cMessageUID pMessageUID, out cModSeqFlags rModSeqFlags) => TryGetModSeqFlags(pMessageUID, out rModSeqFlags, mRootContext);

        // the implementation must set the touched for the messageuid
        protected internal abstract bool TryGetModSeqFlags(cMessageUID pMessageUID, out cModSeqFlags rModSeqFlags, cTrace.cContext pParentContext);

        // the cache must defend against reversions (using modseq) but must always take 'zero' modseq updates as the latest ones
        //  a zero modseq update MUST also set the highestmodseq for the mailbox to zero AND leave it set that way (no further updates to the highestmodseq can be made until the cache is re-instantiated)
        //  the cache must defend against multiple threads setting at the same time
        // the implementation must set the touched for the messageuid
        //
        protected internal abstract void SetModSeqFlags(cMessageUID pMessageUID, cModSeqFlags pModSeqFlags, cTrace.cContext pParentContext); 
    }
}
