using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    public sealed class cExclusiveAccess : IDisposable
    {
        private static int mInstanceSource = 7;

        public event Action<cTrace.cContext> Released;

        private bool mDisposed = false;
        private readonly string mName;
        private readonly int mSequence; // the order in which this lock should be taken in the set of locks that the program has (for use by external code)
        private readonly int mInstance; // for debugging
        private int mBlocks = 0;
        private readonly SemaphoreSlim mExclusiveSemaphoreSlim = new SemaphoreSlim(1);
        private readonly SemaphoreSlim mBlockCheckSemaphoreSlim = new SemaphoreSlim(0);
        private cToken mToken = null;

        public cExclusiveAccess(string pName, int pSequence)
        {
            mName = pName;
            mSequence = pSequence;
            mInstance = Interlocked.Increment(ref mInstanceSource);
        }

        public async Task<cBlock> GetBlockAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cExclusiveAccess), nameof(GetBlockAsync), mName, mInstance);

            if (mDisposed) throw new ObjectDisposedException(nameof(cExclusiveAccess));

            if (!await mExclusiveSemaphoreSlim.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false)) throw new TimeoutException();
            Interlocked.Increment(ref mBlocks);
            mExclusiveSemaphoreSlim.Release();

            return new cBlock(mName, mSequence, mInstance, ZReleaseBlock, pParentContext);
        }

        public cBlock TryGetBlock(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cExclusiveAccess), nameof(TryGetBlock), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cExclusiveAccess));
            if (!mExclusiveSemaphoreSlim.Wait(0)) return null;
            Interlocked.Increment(ref mBlocks);
            mExclusiveSemaphoreSlim.Release();
            return new cBlock(mName, mSequence, mInstance, ZReleaseBlock, pParentContext);
        }

        public async Task<cToken> GetTokenAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cExclusiveAccess), nameof(GetTokenAsync), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cExclusiveAccess));
            if (!await mExclusiveSemaphoreSlim.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false)) throw new TimeoutException();
            while (mBlocks > 0) if (!await mBlockCheckSemaphoreSlim.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false)) throw new TimeoutException();
            mToken = new cToken(mName, mSequence, mInstance, ZReleaseToken, pParentContext);
            return mToken;
        }

        private void ZReleaseBlock(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cExclusiveAccess), nameof(ZReleaseBlock), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cExclusiveAccess));
            if (Interlocked.Decrement(ref mBlocks) == 0 && mBlockCheckSemaphoreSlim.CurrentCount == 0) mBlockCheckSemaphoreSlim.Release();
        }

        private void ZReleaseToken(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cExclusiveAccess), nameof(ZReleaseToken), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cExclusiveAccess));
            mToken = null;
            Released?.Invoke(lContext);
            mExclusiveSemaphoreSlim.Release();
        }

        public void Dispose()
        {
            if (mDisposed) return;

            try { mExclusiveSemaphoreSlim.Dispose(); }
            catch { }

            try { mBlockCheckSemaphoreSlim.Dispose(); }
            catch { }

            mDisposed = true;
        }

        public override string ToString() => $"{nameof(cExclusiveAccess)}({mName},{mInstance},{mBlocks})";

        public sealed class cBlock : IDisposable
        {
            private bool mDisposed = false;
            private readonly string mName;
            public readonly int Sequence;
            private readonly int mInstance;
            private readonly Action<cTrace.cContext> mReleaseBlock;
            private readonly cTrace.cContext mContextToUseWhenDisposing;

            public cBlock(string pName, int pSeqeunce, int pInstance, Action<cTrace.cContext> pReleaseBlock, cTrace.cContext pContextToUseWhenDisposing)
            {
                mName = pName;
                Sequence = pSeqeunce;
                mInstance = pInstance;
                mReleaseBlock = pReleaseBlock;
                mContextToUseWhenDisposing = pContextToUseWhenDisposing;
            }

            public void Dispose()
            {
                if (mDisposed) return;

                try { mReleaseBlock(mContextToUseWhenDisposing); }
                catch { }

                mDisposed = true;
            }

            public override string ToString() => $"{nameof(cBlock)}({mName},{mInstance})";
        }

        public sealed class cToken : IDisposable
        {
            private bool mDisposed = false;
            private readonly string mName;
            public readonly int Sequence;
            private readonly int mInstance;
            private readonly Action<cTrace.cContext> mReleaseToken;
            private readonly cTrace.cContext mContextToUseWhenDisposing;

            public cToken(string pName, int pSequence, int pInstance, Action<cTrace.cContext> pReleaseToken, cTrace.cContext pContextToUseWhenDisposing)
            {
                mName = pName;
                Sequence = pSequence;
                mInstance = pInstance;
                mReleaseToken = pReleaseToken;
                mContextToUseWhenDisposing = pContextToUseWhenDisposing;
            }

            public void Dispose()
            {
                if (mDisposed) return;

                try { mReleaseToken(mContextToUseWhenDisposing); }
                catch { }

                mDisposed = true;
            }

            public override string ToString() => $"{nameof(cToken)}({mName},{mInstance})";
        }
    }
}
