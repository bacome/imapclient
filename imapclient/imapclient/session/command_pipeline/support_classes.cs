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
                private enum eCommandState { pending, active, complete, abandoned  }

                private sealed class cPipelineCommand : iTextCodeProcessor, IDisposable
                {
                    private readonly cCommand mCommand;
                    private readonly object mPipelineLock;
                    private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(0, 1);

                    private eCommandState mState = eCommandState.pending;
                    private cCommandResult mResult = null;
                    private Exception mException = null;

                    public cPipelineCommand(cCommand pCommand, object pPipelineLock)
                    {
                        mCommand = pCommand ?? throw new ArgumentNullException(nameof(pCommand));
                        mPipelineLock = pPipelineLock ?? throw new ArgumentNullException(nameof(pPipelineLock));
                    }

                    public async Task<cCommandResult> WaitAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(WaitAsync), pMC, mCommand.Tag);

                        bool lEntered;

                        try { lEntered = await mSemaphore.WaitAsync(pMC.Timeout, pMC.CancellationToken).ConfigureAwait(false); }
                        finally
                        {
                            lock (mPipelineLock)
                            {
                                if (mState == eCommandState.pending)
                                {
                                    mCommand.Dispose(true);
                                    mState = eCommandState.abandoned;
                                }
                            }
                        }

                        if (!lEntered)
                        {
                            lContext.TraceInformation("timed out");
                            throw new TimeoutException();
                        }

                        if (mException != null) throw mException;

                        return mResult;
                    }

                    public eCommandState State => mState;

                    public void SetActive(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(SetActive), mCommand.Tag);
                        if (mState != eCommandState.pending) throw new InvalidOperationException();
                        mCommand.Hook?.CommandStarted(lContext);
                        mState = eCommandState.active;
                    }

                    public cCommandTag Tag => mCommand.Tag;
                    public ReadOnlyCollection<cCommandPart> Parts => mCommand.Parts;
                    public uint? UIDValidity => mCommand.UIDValidity;
                    public cSASLAuthentication SASLAuthentication => mCommand.SASLAuthentication;
                    public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext) => mCommand.Hook?.ProcessData(pData, pParentContext) ?? eProcessDataResult.notprocessed;
                    public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => mCommand.Hook?.ProcessData(pCursor, pParentContext) ?? eProcessDataResult.notprocessed;
                    public void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext) => mCommand.Hook?.ProcessTextCode(pData, pParentContext);
                    public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext) => mCommand.Hook?.ProcessTextCode(pCursor, pParentContext) ?? false;

                    public void SetResult(cCommandResult pResult, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cPipelineCommand), nameof(SetResult), mCommand.Tag, pResult);

                        if (mState != eCommandState.active) throw new InvalidOperationException();

                        mResult = pResult ?? throw new ArgumentNullException(nameof(pResult));
                        mCommand.Hook?.CommandCompleted(mResult, lContext);
                        mCommand.Dispose(true);
                        mState = eCommandState.complete;

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

                private class cCurrentPipelineCommand : iTextCodeProcessor
                {
                    public readonly cPipelineCommand Command;
                    private int mCurrentPart = 0;
                    public cCurrentPipelineCommand(cPipelineCommand pCommand) { Command = pCommand ?? throw new ArgumentNullException(nameof(pCommand)); }
                    public cCommandTag Tag => Command.Tag;
                    public cCommandPart CurrentPart => Command.Parts[mCurrentPart];
                    public bool MoveNext() => ++mCurrentPart < Command.Parts.Count;
                    public bool IsAuthentication => Command.SASLAuthentication != null;
                    public IList<byte> GetResponse(cByteList pChallenge) => Command.SASLAuthentication.GetResponse(pChallenge);
                    public void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext) => Command.ProcessTextCode(pData, pParentContext);
                    public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext) => Command.ProcessTextCode(pCursor, pParentContext);
                    public override string ToString() => $"{nameof(cCurrentPipelineCommand)}({Command},{mCurrentPart})";
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