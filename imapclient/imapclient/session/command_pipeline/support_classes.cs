using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
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
                private enum eCommandState { pending, abandoned, current, active, complete }
                // pending -> abandoned (and disposed) -> complete
                // pending -> current -> complete (and disposed)
                // pending -> current -> active -> complete (and disposed)

                private sealed class cPipelineCommand : IDisposable
                {
                    private bool mDisposed = false;

                    public readonly cCommandTag Tag;
                    private readonly ReadOnlyCollection<cCommandPart> mParts;
                    private readonly cCommandDisposables mDisposables;
                    private readonly cSASLAuthentication mSASLAuthentication;
                    public readonly uint? UIDValidity;
                    public readonly cCommandHook Hook;
                    private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(0, 1);

                    private eCommandState mState = eCommandState.pending;
                    private int mCurrentPart = 0;
                    private cCommandResult mResult = null;
                    private Exception mException = null;

                    public cPipelineCommand(sCommandDetails pCommandDetails)
                    {
                        Tag = pCommandDetails.Tag ?? throw new ArgumentOutOfRangeException(nameof(pCommandDetails));
                        mParts = pCommandDetails.Parts ?? throw new ArgumentOutOfRangeException(nameof(pCommandDetails));
                        mDisposables = pCommandDetails.Disposables ?? throw new ArgumentOutOfRangeException(nameof(pCommandDetails));
                        mSASLAuthentication = mDisposables.SASLAuthentication;
                        UIDValidity = pCommandDetails.UIDValidity;
                        Hook = pCommandDetails.Hook ?? throw new ArgumentOutOfRangeException(nameof(pCommandDetails));
                    }

                    public async Task<cCommandResult> WaitAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(WaitAsync), pMC, Tag);

                        bool lEntered = await mSemaphore.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false);

                        if (!lEntered)
                        {
                            lContext.TraceInformation("timed out");
                            throw new TimeoutException();
                        }

                        if (mException != null) throw mException;

                        return mResult;
                    }

                    public eCommandState State => mState;

                    public void SetAbandoned(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(SetAbandoned), Tag);
                        if (mState != eCommandState.pending) throw new InvalidOperationException();
                        mState = eCommandState.abandoned;
                    }

                    public void SetCurrent(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(SetActive), Tag);
                        if (mState != eCommandState.pending) throw new InvalidOperationException();
                        Hook.CommandStarted(lContext);
                        mState = eCommandState.current;
                    }

                    public cCommandPart CurrentPart()
                    {
                        if (mState != eCommandState.current) throw new InvalidOperationException();
                        return mParts[mCurrentPart];
                    }

                    public bool MoveNext()
                    {
                        if (mState != eCommandState.current) throw new InvalidOperationException();
                        return ++mCurrentPart < mParts.Count;
                    }

                    public bool IsAuthentication => mSASLAuthentication != null;

                    public IList<byte> GetAuthenticationResponse(cByteList pChallenge)
                    {
                        if (mState != eCommandState.current || mSASLAuthentication == null) throw new InvalidOperationException();
                        return mSASLAuthentication.GetResponse(pChallenge);
                    }

                    public void SetActive(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(SetActive), Tag);
                        if (mState != eCommandState.current) throw new InvalidOperationException();
                        mState = eCommandState.active;
                    }

                    public void SetResult(cCommandResult pResult, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(SetResult), Tag, pResult);

                        if (mState != eCommandState.current && mState != eCommandState.active) throw new InvalidOperationException();

                        mResult = pResult ?? throw new ArgumentNullException(nameof(pResult));

                        Hook.CommandCompleted(pResult, lContext);
                        mState = eCommandState.complete;

                        ;?;

                        // may throw objectdisposed if the caller stopped waiting 
                        try { mSemaphore.Release(); }
                        catch { }


                    }

                    public void SetException(Exception pException, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(SetException), mCommand.Tag, pException);

                        if (mState != eCommandState.pending && mState != eCommandState.active) throw new InvalidOperationException();

                        mException = pException ?? throw new ArgumentNullException(nameof(pException));
                        mCommand.Dispose(true);
                        mState = eCommandState.complete;

                        // may throw objectdisposed if the caller stopped waiting 
                        try { mSemaphore.Release(); }
                        catch { }
                    }

                    public override string ToString() => $"{nameof(cPipelineCommand)}({mCommand.Tag},{mState},{mResult},{mException})";

                    public void Dispose()
                    {
                        if (mSemaphore != null)
                        {
                            try { mSemaphore.Dispose(); }
                            catch { }
                        }
                    }
                }

                private class cActivePipelineCommands : iTextCodeProcessor, IEnumerable<cPipelineCommand>
                {
                    private readonly List<cPipelineCommand> mCommands = new List<cPipelineCommand>();

                    public cActivePipelineCommands() { }

                    public int Count => mCommands.Count;
                    public cPipelineCommand this[int pIndex] => mCommands[pIndex];
                    public void Add(cCurrentPipelineCommand pCommand) => mCommands.Add(pCommand.Command);
                    public void RemoveAt(int pIndex) => mCommands.RemoveAt(pIndex);

                    public void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cActivePipelineCommands), nameof(ProcessTextCode));
                        foreach (var lCommand in mCommands) lCommand.ProcessTextCode(pData, lContext);
                    }

                    public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cActivePipelineCommands), nameof(ProcessTextCode));

                        bool lProcessed = false;
                        var lBookmark = pCursor.Position;
                        var lPositionAtEnd = pCursor.Position;

                        foreach (var lCommand in mCommands)
                        {
                            if (lCommand.ProcessTextCode(pCursor, lContext) && !lProcessed)
                            {
                                lProcessed = true;
                                lPositionAtEnd = pCursor.Position;
                            }

                            pCursor.Position = lBookmark;
                        }

                        if (lProcessed) pCursor.Position = lPositionAtEnd;

                        return lProcessed;
                    }

                    public IEnumerator<cPipelineCommand> GetEnumerator() => mCommands.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                    public override string ToString()
                    {
                        var lBuilder = new cListBuilder(nameof(cActivePipelineCommands));
                        foreach (var lCommand in mCommands) lBuilder.Append(lCommand);
                        return lBuilder.ToString();
                    }
                }

                private class cAuthenticateState
                {
                    public bool CancelSent = false;
                    public cAuthenticateState() { }
                }
            }
        }
    }
}