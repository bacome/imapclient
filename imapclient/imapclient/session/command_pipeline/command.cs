using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            {
                private enum eCommandState { queued, cancelled, sending, sent }

                private class cCommand
                {
                    public readonly cCommandTag Tag;
                    private readonly ReadOnlyCollection<cCommandPart> mParts;
                    private readonly cCommandDisposables mDisposables;
                    private readonly cSASLAuthentication mSASLAuthentication;
                    public readonly uint? UIDValidity;
                    public readonly cCommandHook Hook;
                    private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(0, 1);

                    // state
                    private eCommandState mState = eCommandState.queued;
                    private bool mAwaitingContinuation = false;
                    private cCommandResult mResult = null;
                    private Exception mException = null;

                    private int mCurrentPart = 0;

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
                    public bool AwaitingContinuation => mAwaitingContinuation;
                    public bool HasResult => mResult != null || mException != null;

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

                    public void SetCancelled(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetCancelled), Tag);
                        if (mState != eCommandState.queued) throw new InvalidOperationException();
                        mDisposables.Dispose();
                        mState = eCommandState.cancelled;
                    }

                    public void SetSending(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetSending), Tag);
                        if (mState != eCommandState.queued) throw new InvalidOperationException();
                        Hook.CommandStarted(lContext);
                        mState = eCommandState.sending;
                    }

                    public void SetAwaitingContinuation(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetAwaitingContinuation), Tag);
                        if (mState != eCommandState.sending || mAwaitingContinuation || HasResult) throw new InvalidOperationException();
                        mAwaitingContinuation = true;
                    }

                    public void ResetAwaitingContinuation(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(ResetAwaitingContinuation), Tag);
                        if (mState != eCommandState.sending || !mAwaitingContinuation || HasResult) throw new InvalidOperationException();
                        mAwaitingContinuation = false;
                    }

                    public cCommandPart GetCurrentPart()
                    {
                        if (mState != eCommandState.sending || mAwaitingContinuation) throw new InvalidOperationException();
                        return mParts[mCurrentPart];
                    }

                    public bool MoveNext()
                    {
                        if (mState != eCommandState.sending || mAwaitingContinuation || HasResult) throw new InvalidOperationException();
                        return ++mCurrentPart < mParts.Count;
                    }

                    public bool IsAuthentication => mSASLAuthentication != null;

                    public IList<byte> GetAuthenticationResponse(cByteList pChallenge)
                    {
                        if (mState != eCommandState.sending || !mAwaitingContinuation || HasResult || mSASLAuthentication == null) throw new InvalidOperationException();
                        return mSASLAuthentication.GetResponse(pChallenge);
                    }

                    public void SetSent(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetSent), Tag);
                        if (mState != eCommandState.sending || mAwaitingContinuation || HasResult || mSASLAuthentication != null) throw new InvalidOperationException();
                        mState = eCommandState.sent;
                    }

                    public void SetResult(cCommandResult pResult, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetResult), Tag, pResult);

                        if (mState == eCommandState.queued || mState == eCommandState.cancelled || HasResult) throw new InvalidOperationException();

                        mResult = pResult ?? throw new ArgumentNullException(nameof(pResult));

                        Hook.CommandCompleted(pResult, lContext);
                        mDisposables.Dispose();

                        // will throw objectdisposed if the wait finishes before the command does
                        try { mSemaphore.Release(); }
                        catch { }
                    }

                    public void SetException(Exception pException, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cCommand), nameof(SetException), Tag, pException);

                        mException = pException ?? throw new ArgumentNullException(nameof(pException));

                        mDisposables.Dispose();

                        // will throw objectdisposed if the wait finishes before the command does
                        try { mSemaphore.Release(); }
                        catch { }
                    }

                    public override string ToString() => $"{nameof(cCommand)}({Tag},{mState},{mAwaitingContinuation},{mResult},{mException})";
                }
            }
        }
    }
}