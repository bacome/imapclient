using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// <para>Provides a method to control access to a resource that should only be used exclusively.</para>
    /// <para>Exclusive access to the resource is granted by the issue of a 'token'. Only one token can be issued at one time.</para>
    /// <para>Access to the token may be blocked by the issue of a 'block'. Several blocks can be on issue at the same time.</para>
    /// <para>If there are blocks issued, the token cannot be issued.</para>
    /// <para>If the token is issued, blocks cannot be issued.</para>
    /// <para>Use <see cref="GetTokenAsync(cMethodControl, cTrace.cContext)"/> to get the token. This method will not complete until the token can be issued or the passed <see cref="cMethodControl"/> terminates it.</para>
    /// <para>Use <see cref="GetBlockAsync(cMethodControl, cTrace.cContext)"/> to get a block. This method will not complete until a block can be issued or the passed <see cref="cMethodControl"/> terminates it.</para>
    /// <para>Use <see cref="TryGetBlock(cTrace.cContext)"/> to try to get a block. This method will return a block if the token is not currenly issued, otherwise it will return null.</para>
    /// <para>Note that the token and block objects implement <see cref="IDisposable"/> so they must be disposed when you are finished with them.</para>
    /// <para>Note that this class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// </summary>
    public sealed class cExclusiveAccess : IDisposable
    {
        private static int mInstanceSource = 7;

        /// <summary>
        /// Raised when the token is returned from issue.
        /// </summary>
        public event Action<cTrace.cContext> Released;

        private bool mDisposed = false;

        private readonly string mName;
        private readonly int mSequence; // the order in which this lock should be taken in the set of locks that the program has (for use by external code)
        private readonly int mInstance; // for debugging
        private int mBlocks = 0;
        private readonly SemaphoreSlim mExclusiveSemaphoreSlim = new SemaphoreSlim(1);
        private readonly SemaphoreSlim mBlockCheckSemaphoreSlim = new SemaphoreSlim(0);
        private cToken mToken = null;

        /// <summary>
        /// <para>For tracing purposes you may give the instance a name.</para>
        /// <para>For deadlock avoidance checking reasons you may give the instance a sequence. The sequence can be used by external code to ensure that the program's locks are being taken in the correct order.</para>
        /// </summary>
        /// <param name="pName">The name to include in trace messages written by the instance.</param>
        /// <param name="pSequence">The sequence to give the instance.</param>
        public cExclusiveAccess(string pName, int pSequence)
        {
            mName = pName;
            mSequence = pSequence;
            mInstance = Interlocked.Increment(ref mInstanceSource);
        }

        /// <summary>
        /// <para>Returns a disposable object that represents a block on the issue of the token.</para>
        /// <para>This method will not complete until the block is issued or the passed <paramref name="pMC"/> terminates it.</para>
        /// <para>Dispose the returned object to release the block.</para>
        /// </summary>
        /// <param name="pMC">Controls the execution of the method.</param>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns>An object that represents a block on the issue of the token.</returns>
        public async Task<cBlock> GetBlockAsync(cMethodControl pMC, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cExclusiveAccess), nameof(GetBlockAsync), mName, mInstance);

            if (mDisposed) throw new ObjectDisposedException(nameof(cExclusiveAccess));

            if (!await mExclusiveSemaphoreSlim.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false)) throw new TimeoutException();
            Interlocked.Increment(ref mBlocks);
            mExclusiveSemaphoreSlim.Release();

            return new cBlock(mName, mSequence, mInstance, ZReleaseBlock, pParentContext);
        }

        /// <summary>
        /// <para>May return a disposable object that represents a block on the issue of the token.</para>
        /// <para>This method will return a block if the token is not currently issued, otherwise it will return null.</para>
        /// <para>Dispose the returned object to release the block.</para>
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns>An object that represents a block on the issue of the token.</returns>
        public cBlock TryGetBlock(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cExclusiveAccess), nameof(TryGetBlock), mName, mInstance);
            if (mDisposed) throw new ObjectDisposedException(nameof(cExclusiveAccess));
            if (!mExclusiveSemaphoreSlim.Wait(0)) return null;
            Interlocked.Increment(ref mBlocks);
            mExclusiveSemaphoreSlim.Release();
            return new cBlock(mName, mSequence, mInstance, ZReleaseBlock, pParentContext);
        }

        /// <summary>
        /// <para>Returns a disposable object that represents a token that enables exclusive use of a resource.</para>
        /// <para>This method will not complete until the token is issued or the passed <paramref name="pMC"/> terminates it.</para>
        /// <para>Dispose the object to return the token.</para>
        /// </summary>
        /// <param name="pMC">Controls the execution of the method.</param>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns>An object that represents a token that enables exclusive use of a resource.</returns>
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

        /// <summary>
        /// <para>Instances represent a block on the issue of a token from a <see cref="cExclusiveAccess"/>.</para>
        /// <para>Dispose the instance to release the block.</para>
        /// </summary>
        public sealed class cBlock : IDisposable
        {
            private bool mDisposed = false;
            private readonly string mName;
            /**<summary>The sequence number of the issuing <see cref="cExclusiveAccess"/>.</summary>*/
            public readonly int Sequence;
            private readonly int mInstance;
            private readonly Action<cTrace.cContext> mReleaseBlock;
            private readonly cTrace.cContext mContextToUseWhenDisposing;

            internal cBlock(string pName, int pSeqeunce, int pInstance, Action<cTrace.cContext> pReleaseBlock, cTrace.cContext pContextToUseWhenDisposing)
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

        /// <summary>
        /// <para>Instances represent a token that grants exclusive use of a resource to the holder. Issued from a <see cref="cExclusiveAccess"/>.</para>
        /// <para>Dispose the instance to release the token.</para>
        /// </summary>
        public sealed class cToken : IDisposable
        {
            private bool mDisposed = false;
            private readonly string mName;
            /**<summary>The sequence number of the issuing <see cref="cExclusiveAccess"/>.</summary>*/
            public readonly int Sequence;
            private readonly int mInstance;
            private readonly Action<cTrace.cContext> mReleaseToken;
            private readonly cTrace.cContext mContextToUseWhenDisposing;

            internal cToken(string pName, int pSequence, int pInstance, Action<cTrace.cContext> pReleaseToken, cTrace.cContext pContextToUseWhenDisposing)
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
