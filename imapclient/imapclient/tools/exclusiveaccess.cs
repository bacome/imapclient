﻿using System;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.trace;

namespace work.bacome.async
{
    /// <summary>
    /// Provides a mechanism to control exclusive access using tokens and blocks.
    /// </summary>
    /// <remarks>
    /// <para>The granting of exclusive access is done by issuing a token. Exclusive access is released by disposing the token object.</para>
    /// <para>The granting of exclusive access may be blocked by the previous issue of blocks. Several blocks can be on issue at the same time. Blocks are released by disposing the block objects.</para>
    /// <para>Blocks will not be issued while a token is on issue. Tokens will not be issued while blocks are on issue (nor will they be issued while a token is on issue).</para>
    /// <para>Instance sequence numbers (specified in the constructor) can be used by external code to ensure that the program's tokens are requested in a consistent order (to avoid deadlocks).</para>
    /// <para>Each instance of this class is allocated a unique instance number that is used in <see cref="cTrace"/> messages to aid debugging.</para>
    /// <para>This class implements <see cref="IDisposable"/>, so you should dispose instances when you are finished with them.</para>
    /// </remarks>
    public sealed class cExclusiveAccess : IDisposable
    {
        private static int mInstanceSource = 7;

        /// <summary>
        /// Raised when exclusive access is released by the disposal of a token object.
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
        /// Initialises a new instance with the specified name and sequence number.
        /// </summary>
        /// <param name="pName">The instance name to include in trace messages written by the instance.</param>
        /// <param name="pSequence">The sequence number to give the instance. Sequence numbers can be used by external code to ensure that the program's tokens are requested in a consistent order (to avoid deadlocks).</param>
        public cExclusiveAccess(string pName, int pSequence)
        {
            mName = pName;
            mSequence = pSequence;
            mInstance = Interlocked.Increment(ref mInstanceSource);
        }

        /// <summary>
        /// Gets a disposable object that represents a block on the granting of exclusive access.
        /// This method will not complete until the block is issued or it throws due to <see cref="cMethodControl"/>.
        /// Dispose the returned object to release the block.
        /// </summary>
        /// <param name="pMC">Controls the execution of the method.</param>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns></returns>
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
        /// May return a disposable object that represents a block on the issue of exclusive access.
        /// This method will return a block if the exclusive access is not currently granted, otherwise it will return <see langword="null"/>.
        /// Dispose the returned object (if any) to release the block.
        /// </summary>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns></returns>
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
        /// Gets a disposable object that represents a grant of exclusive access.
        /// This method will not complete until the exclusive access is granted or it throws due to <see cref="cMethodControl"/>.
        /// Dispose the object to release the exclusive access.
        /// </summary>
        /// <param name="pMC">Controls the execution of the method.</param>
        /// <param name="pParentContext">Context for trace messages.</param>
        /// <returns></returns>
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

        /**<summary></summary>*/
        public void Dispose()
        {
            if (mDisposed) return;

            try { mExclusiveSemaphoreSlim.Dispose(); }
            catch { }

            try { mBlockCheckSemaphoreSlim.Dispose(); }
            catch { }

            mDisposed = true;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cExclusiveAccess)}({mName},{mInstance},{mBlocks})";

        /// <summary>
        /// Represents a block on the issue of excusive access.
        /// </summary>
        /// <remarks>
        /// Dispose the instance to release the block.
        /// </remarks>
        /// <seealso cref="cExclusiveAccess"/>
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

            /**<summary></summary>*/
            public void Dispose()
            {
                if (mDisposed) return;

                try { mReleaseBlock(mContextToUseWhenDisposing); }
                catch { }

                mDisposed = true;
            }

            /// <inheritdoc />
            public override string ToString() => $"{nameof(cBlock)}({mName},{mInstance})";
        }

        /// <summary>
        /// Represents a grant of exclusive access.
        /// </summary>
        /// <remarks>
        /// Dispose the instance to release the exclusive access.
        /// </remarks>
        /// <seealso cref="cExclusiveAccess"/>
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

            /**<summary></summary>*/
            public void Dispose()
            {
                if (mDisposed) return;

                try { mReleaseToken(mContextToUseWhenDisposing); }
                catch { }

                mDisposed = true;
            }

            /// <inheritdoc/>
            public override string ToString() => $"{nameof(cToken)}({mName},{mInstance})";
        }
    }
}
