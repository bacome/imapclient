using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
                private sealed class cItem : iTextCodeProcessor, IDisposable
                {
                    private readonly cCommand mCommand;
                    private readonly object mCommandQueueLock;
                    private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(0, 1);
                    private cCommandResult mResult = null;
                    private Exception mException = null;
                    private bool mStarted = false; // command has gone past the point of no return: it will be or has been sent to the server
                    public bool WaitOver { get; private set; } = false; // the submitter of the command is no longer waiting for the result
                    public ReadOnlyCollection<cCommandPart> Parts;

                    public cItem(cCommand pCommand, object pCommandQueueLock)
                    {
                        mCommand = pCommand;
                        mCommandQueueLock = pCommandQueueLock;
                        Parts = new ReadOnlyCollection<cCommandPart>(mCommand.Parts);
                    }

                    public async Task<cCommandResult> WaitAsync(int pTimeout, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(WaitAsync), mCommand.Tag);

                        bool lEntered;

                        lContext.TraceVerbose("waiting");

                        try { lEntered = await mSemaphore.WaitAsync(pTimeout, pCancellationToken).ConfigureAwait(false); }
                        catch (Exception e)
                        {
                            lContext.TraceException(TraceEventType.Verbose, "exception while waiting for command completion", e);
                            throw;
                        }
                        finally
                        {
                            lock (mCommandQueueLock)
                            {
                                if (!mStarted) mCommand.CommandCompleted(mResult, mException, lContext);
                                WaitOver = true;
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

                    public void SetStarted(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetStarted), mCommand.Tag);
                        if (mStarted) throw new InvalidOperationException();
                        mStarted = true;
                        mCommand.CommandStarted(lContext);
                    }

                    public void SetResult(cCommandResult pResult, uint? pUIDValidity, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetResult), mCommand.Tag, pResult);

                        if (mResult != null) throw new InvalidOperationException();
                        if (mException != null) throw new InvalidOperationException();

                        mResult = pResult ?? throw new ArgumentNullException(nameof(pResult));
                        if (mCommand.UIDValidity != null && mCommand.UIDValidity != pUIDValidity) mException = new cUIDValidityChangedException(lContext);

                        mCommand.CommandCompleted(mResult, mException, lContext);

                        // may throw objectdisposed if the caller stopped waiting 
                        try { mSemaphore.Release(); }
                        catch { }
                    }

                    public void SetException(Exception pException, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cItem), nameof(SetException), mCommand.Tag, pException);

                        if (mResult != null) throw new InvalidOperationException();
                        if (mException != null) throw new InvalidOperationException();

                        mException = pException ?? throw new ArgumentNullException(nameof(pException));

                        mCommand.CommandCompleted(mResult, mException, lContext);

                        // may throw objectdisposed if the caller stopped waiting 
                        try { mSemaphore.Release(); }
                        catch { }
                    }

                    public cCommandTag Tag => mCommand.Tag;
                    public uint? UIDValidity => mCommand.UIDValidity;
                    public cSASLAuthentication Authentication => mCommand.Authentication;
                    public eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext) => mCommand.ProcessData(pData, pParentContext);
                    public eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext) => mCommand.ProcessData(pCursor, pParentContext);
                    public void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext) => mCommand.ProcessTextCode(pData, pParentContext);
                    public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext) => mCommand.ProcessTextCode(pCursor, pParentContext);

                    public override string ToString() => $"{nameof(cItem)}({mCommand.Tag},{mResult},{mException},{mStarted},{WaitOver})";

                    public void Dispose()
                    {
                        if (mSemaphore != null)
                        {
                            try { mSemaphore.Dispose(); }
                            catch { }
                        }
                    }
                }

                private class cCurrentItem
                {
                    public readonly cItem Item;
                    private int mCurrentPart = 0;
                    public cCurrentItem(cItem pItem) { Item = pItem; }
                    public cCommandPart CurrentPart => Item.Parts[mCurrentPart];
                    public bool MoveNext() => ++mCurrentPart < Item.Parts.Count;
                    public override string ToString() => $"{nameof(cCurrentItem)}({Item},{mCurrentPart})";
                }

                private class cActiveItems : iTextCodeProcessor, IEnumerable<cItem>
                {
                    private readonly List<cItem> mItems = new List<cItem>();

                    public cActiveItems() { }

                    public int Count => mItems.Count;
                    public cItem this[int pIndex] => mItems[pIndex];
                    public void Add(cItem pHandle) => mItems.Add(pHandle);
                    public void RemoveAt(int pIndex) => mItems.RemoveAt(pIndex);

                    public void ProcessTextCode(cResponseData pData, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cActiveItems), nameof(ProcessTextCode));
                        foreach (var lItem in mItems) lItem.ProcessTextCode(pData, lContext);
                    }

                    public bool ProcessTextCode(cBytesCursor pCursor, cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cActiveItems), nameof(ProcessTextCode));

                        bool lProcessed = false;
                        var lBookmark = pCursor.Position;
                        var lPositionAtEnd = pCursor.Position;

                        foreach (var lItem in mItems)
                        {
                            if (lItem.ProcessTextCode(pCursor, lContext) && !lProcessed)
                            {
                                lProcessed = true;
                                lPositionAtEnd = pCursor.Position;
                            }

                            pCursor.Position = lBookmark;
                        }

                        if (lProcessed) pCursor.Position = lPositionAtEnd;

                        return lProcessed;
                    }

                    public IEnumerator<cItem> GetEnumerator() => mItems.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                    public override string ToString()
                    {
                        var lBuilder = new cListBuilder(nameof(cActiveItems));
                        foreach (var lItem in mItems) lBuilder.Append(lItem);
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