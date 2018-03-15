using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private sealed class cCommandDisposables : IDisposable
            {
                private bool mDisposed = false;

                private readonly List<IDisposable> mDisposables = new List<IDisposable>();

                private readonly List<cExclusiveAccess.cToken> mTokens = new List<cExclusiveAccess.cToken>();
                private readonly List<cExclusiveAccess.cBlock> mBlocks = new List<cExclusiveAccess.cBlock>();
                private int mExclusiveAccessSequence = -1;

                private cSASLAuthentication mSASLAuthentication = null;

                public cCommandDisposables() { }

                public void Add(IDisposable pDisposable)
                {
                    if (pDisposable == null) throw new ArgumentNullException(nameof(pDisposable));
                    mDisposables.Add(pDisposable);
                }

                public void Add(cExclusiveAccess.cToken pToken)
                {
                    if (pToken.Sequence <= mExclusiveAccessSequence) throw new ArgumentOutOfRangeException();
                    mTokens.Add(pToken);
                    mExclusiveAccessSequence = pToken.Sequence;
                }

                public void Add(cExclusiveAccess.cBlock pBlock)
                {
                    if (pBlock.Sequence <= mExclusiveAccessSequence) throw new ArgumentOutOfRangeException();
                    mBlocks.Add(pBlock);
                    mExclusiveAccessSequence = pBlock.Sequence;
                }

                public void Add(cSASLAuthentication pSASLAuthentication)
                {
                    if (mSASLAuthentication != null) throw new InvalidOperationException();
                    mSASLAuthentication = pSASLAuthentication ?? throw new ArgumentNullException(nameof(pSASLAuthentication));
                }

                public cSASLAuthentication SASLAuthentication => mSASLAuthentication;

                public void Dispose()
                {
                    if (mDisposed) return;

                    foreach (var lDisposable in mDisposables)
                    {
                        try { lDisposable.Dispose(); }
                        catch { }
                    }

                    foreach (var lToken in mTokens)
                    {
                        try { lToken.Dispose(); }
                        catch { }
                    }

                    foreach (var lBlock in mBlocks)
                    {
                        try { lBlock.Dispose(); }
                        catch { }
                    }

                    if (mSASLAuthentication != null)
                    {
                        try { mSASLAuthentication.Dispose(); }
                        catch { }
                    }

                    mDisposed = true;
                }
            }
        }
    }
}