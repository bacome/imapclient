using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            {
                private enum eCommandState { queued, current, active, complete }

                private class cCommand
                {
                    public readonly cCommandTag Tag;
                    private readonly ReadOnlyCollection<cCommandPart> mParts;
                    private readonly cCommandDisposables mDisposables;
                    private readonly cSASLAuthentication mSASLAuthentication;
                    public readonly uint? UIDValidity;
                    public readonly cCommandHook Hook;
                    private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(0, 1);

                    private eCommandState mState = eCommandState.queued;
                    private bool mWaitingForContinuationRequest = false;

                    private int mCurrentPart = 0;
                    private cCommandResult mResult = null;
                    private Exception mException = null;

                    public cCommand(sCommandDetails pCommandDetails)
                    {
                        Tag = pCommandDetails.Tag ?? throw new ArgumentOutOfRangeException(nameof(pCommandDetails));
                        mParts = pCommandDetails.Parts ?? throw new ArgumentOutOfRangeException(nameof(pCommandDetails));
                        mDisposables = pCommandDetails.Disposables ?? throw new ArgumentOutOfRangeException(nameof(pCommandDetails));
                        mSASLAuthentication = mDisposables.SASLAuthentication;
                        UIDValidity = pCommandDetails.UIDValidity;
                        Hook = pCommandDetails.Hook ?? throw new ArgumentOutOfRangeException(nameof(pCommandDetails));
                    }

                    public eCommandState State => mState;

                    public async Task<cCommandResult> WaitAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(WaitAsync), pMC, Tag);

                        bool lEntered = await mSemaphore.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false);

                        try { mSemaphore.Dispose(); }
                        catch { }

                        if (!lEntered)
                        {
                            lContext.TraceInformation("timed out");
                            throw new TimeoutException();
                        }

                        if (mException != null) throw mException;

                        return mResult;
                    }

                    public void SetComplete(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetComplete), Tag);
                        if (mState != eCommandState.queued) throw new InvalidOperationException();
                        mDisposables.Dispose();
                        mState = eCommandState.complete;
                    }

                    public void SetCurrent(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetCurrent), Tag);
                        if (mState != eCommandState.queued) throw new InvalidOperationException();
                        Hook.CommandStarted(lContext);
                        mState = eCommandState.current;
                    }

                    public cCommandPart CurrentPart()
                    {
                        // note that complete is allowed to cater for early termination
                        if (mState != eCommandState.current && mState != eCommandState.complete) throw new InvalidOperationException();
                        return mParts[mCurrentPart];
                    }

                    public bool MoveNext()
                    {
                        // note that complete is allowed to cater for early termination
                        if (mState != eCommandState.current && mState != eCommandState.complete) throw new InvalidOperationException();
                        return ++mCurrentPart < mParts.Count;
                    }

                    public bool WaitingForContinuationRequest
                    {
                        get => mWaitingForContinuationRequest;

                        set
                        {
                            // note that complete is allowed to cater for early termination
                            if (mState != eCommandState.current && mState != eCommandState.complete) throw new InvalidOperationException();
                            if (value == mWaitingForContinuationRequest) throw new InvalidOperationException();
                            mWaitingForContinuationRequest = value;
                        }
                    }

                    public bool IsAuthentication => mSASLAuthentication != null;

                    public IList<byte> GetAuthenticationResponse(cByteList pChallenge)
                    {
                        if (mState != eCommandState.current || mSASLAuthentication == null) throw new InvalidOperationException();
                        return mSASLAuthentication.GetResponse(pChallenge);
                    }

                    public void SetActive(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetActive), Tag);
                        if (mState != eCommandState.current) throw new InvalidOperationException();
                        mState = eCommandState.active;
                    }

                    public void SetResult(cCommandResult pResult, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetResult), Tag, pResult);

                        if (mState != eCommandState.current && mState != eCommandState.active) throw new InvalidOperationException();

                        mResult = pResult ?? throw new ArgumentNullException(nameof(pResult));

                        Hook.CommandCompleted(pResult, lContext);
                        mDisposables.Dispose();
                        mState = eCommandState.complete;

                        // will throw objectdisposed if the wait finishes before the command does
                        try { mSemaphore.Release(); }
                        catch { }
                    }

                    public void SetException(Exception pException, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetException), Tag, pException);

                        mException = pException ?? throw new ArgumentNullException(nameof(pException));

                        mDisposables.Dispose();
                        mState = eCommandState.complete;

                        // will throw objectdisposed if the wait finishes before the command does
                        try { mSemaphore.Release(); }
                        catch { }
                    }

                    public override string ToString() => $"{nameof(cCommand)}({Tag},{mState},{mResult},{mException})";
                }
            }
        }
    }
}