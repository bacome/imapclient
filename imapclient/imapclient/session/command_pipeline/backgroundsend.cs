using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                // growable buffers
                private readonly cByteList mBackgroundSendBuffer = new cByteList();
                private readonly List<int> mBackgroundSendTraceBufferStartPoints = new List<int>();
                private readonly cByteList mBackgroundSendTraceBuffer = new cByteList();

                private async Task ZBackgroundSendAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewRootMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendAsync));

                    if (mCurrentCommand != null) await ZBackgroundSendAppendDataAndMoveNextPartAsync(lContext).ConfigureAwait(false);

                    while (true)
                    {
                        if (mCurrentCommand == null)
                        {
                            ZBackgroundSendDequeueAndAppendTag(lContext);
                            if (mCurrentCommand == null) break;

                            if (mCurrentCommand.IsAuthentication)
                            {
                                ZBackgroundSendAppendAllTextParts(lContext);
                                mCurrentCommand.WaitingForContinuationRequest = true;
                                break;
                            }
                        }

                        if (ZBackgroundSendAppendLiteralHeader())
                        {
                            mCurrentCommand.WaitingForContinuationRequest = true;
                            break;
                        }

                        await ZBackgroundSendAppendDataAndMoveNextPartAsync(lContext).ConfigureAwait(false);
                    }

                    await ZBackgroundSendWriteAndClearBufferAsync(lContext).ConfigureAwait(false);
                }

                private void ZBackgroundSendDequeueAndAppendTag(cTrace.cContext pParentContext)
                {
                    // gets the next command (if there is one) appends the tag part and returns true if it did

                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendDequeueAndAppendTag));

                    lock (mPipelineLock)
                    {
                        while (mQueuedCommands.Count > 0)
                        {
                            var lCommand = mQueuedCommands.Dequeue();

                            if (lCommand.State == eCommandState.queued)
                            {
                                if (lCommand.UIDValidity != null && lCommand.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.Cache.UIDValidity) lCommand.SetException(new cUIDValidityChangedException(lContext), lContext);
                                else
                                {
                                    mCurrentCommand = lCommand;
                                    lCommand.SetCurrent(lContext);
                                    break;
                                }
                            }
                        }
                    }

                    if (mCurrentCommand == null) return;

                    mBackgroundSendBuffer.AddRange(mCurrentCommand.Tag);
                    mBackgroundSendBuffer.Add(cASCII.SPACE);

                    mBackgroundSendTraceBufferStartPoints.Add(mBackgroundSendTraceBuffer.Count);

                    mBackgroundSendTraceBuffer.AddRange(mCurrentCommand.Tag);
                    mBackgroundSendTraceBuffer.Add(cASCII.SPACE);
                }

                private bool ZBackgroundSendAppendLiteralHeader()
                {
                    // if the current part is a literal, appends the literal header
                    //  if the current part is a synchronising literal, returns true

                    if (!(mCurrentCommand.CurrentPart() is cLiteralCommandPartBase lLiteral)) return false;

                    if (lLiteral.Binary) mBackgroundSendBuffer.Add(cASCII.TILDA);

                    cByteList lLengthBytes = cTools.IntToBytesReverse(lLiteral.Length);
                    lLengthBytes.Reverse();

                    mBackgroundSendBuffer.Add(cASCII.LBRACE);
                    mBackgroundSendBuffer.AddRange(lLengthBytes);

                    mBackgroundSendTraceBuffer.Add(cASCII.LBRACE);
                    if (lLiteral.Secret) mBackgroundSendTraceBuffer.Add(cASCII.NUL);
                    else mBackgroundSendTraceBuffer.AddRange(lLengthBytes);

                    bool lSynchronising;

                    if (mLiteralPlus || mLiteralMinus && lLiteral.Length < 4097)
                    {
                        mBackgroundSendBuffer.Add(cASCII.PLUS);
                        mBackgroundSendTraceBuffer.Add(cASCII.PLUS);
                        lSynchronising = false;
                    }
                    else lSynchronising = true;

                    mBackgroundSendBuffer.Add(cASCII.RBRACE);
                    mBackgroundSendBuffer.Add(cASCII.CR);
                    mBackgroundSendBuffer.Add(cASCII.LF);

                    mBackgroundSendTraceBuffer.Add(cASCII.RBRACE);
                    mBackgroundSendTraceBuffer.Add(cASCII.CR);
                    mBackgroundSendTraceBuffer.Add(cASCII.LF);

                    return lSynchronising;
                }

                private void ZBackgroundSendAppendAllTextParts(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendAppendAllTextParts));

                    do
                    {
                        if (mCurrentCommand.CurrentPart() is cTextCommandPart lText) ZBackgroundSendAppendBytes(lText.Secret, lText.Bytes);
                        else throw new cInternalErrorException();
                    }
                    while (mCurrentCommand.MoveNext());

                    mBackgroundSendBuffer.Add(cASCII.CR);
                    mBackgroundSendBuffer.Add(cASCII.LF);

                    mBackgroundSendTraceBuffer.Add(cASCII.CR);
                    mBackgroundSendTraceBuffer.Add(cASCII.LF);
                }

                private static cBytes kBackgroundSendStreamingData = new cBytes("<streaming data> ...");

                private async Task ZBackgroundSendAppendDataAndMoveNextPartAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(true, nameof(cCommandPipeline), nameof(ZBackgroundSendAppendDataAndMoveNextPartAsync));

                    var lPart = mCurrentCommand.CurrentPart();

                    if (lPart is cLiteralCommandPart lLiteral)
                    {
                        if (mBackgroundSendBuffer.Count == 0) mBackgroundSendTraceBufferStartPoints.Add(0);
                        ZBackgroundSendAppendBytes(lLiteral.Secret, lLiteral.Bytes);
                        ZBackgroundSendMoveNext(lContext);
                        return;
                    }

                    if (lPart is cTextCommandPart lText)
                    {
                        if (mBackgroundSendBuffer.Count == 0) mBackgroundSendTraceBufferStartPoints.Add(0);
                        ZBackgroundSendAppendBytes(lText.Secret, lText.Bytes);
                        ZBackgroundSendMoveNext(lContext);
                        return;
                    }

                    if (!(lPart is cStreamCommandPart lStream)) throw new cInternalErrorException();

                    int lBytesRemaining = lStream.Length;
                    Stopwatch lStopwatch = new Stopwatch();

                    if (lBytesRemaining > 0)
                    {
                        if (mBackgroundSendBuffer.Count > 0) await ZBackgroundSendWriteAndClearBufferAsync(lContext).ConfigureAwait(false);

                        cBatchSizer lReadSizer = new cBatchSizer(lStream.ReadConfiguration);

                        while (lBytesRemaining > 0)
                        {
                            int lCount = Math.Min(lReadSizer.Current, mConnection.CurrentWriteSize);
                            if (lCount > lBytesRemaining) lCount = lBytesRemaining;
                            byte[] lBuffer = new byte[lCount];

                            lStopwatch.Restart();
                            await lStream.Stream.ReadAsync(lBuffer, 0, lCount, mBackgroundCancellationTokenSource.Token).ConfigureAwait(false);
                            lStopwatch.Stop();

                            // store the time taken so the next read is a better size
                            lReadSizer.AddSample(lCount, lStopwatch.ElapsedMilliseconds);

                            lContext.TraceVerbose("sending {0} bytes", lCount);
                            mSynchroniser.InvokeNetworkActivity(kBufferStartPointsBeginning, kBackgroundSendStreamingData, lContext);

                            await mConnection.WriteAsync(lBuffer, mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);

                            lBytesRemaining -= lCount;
                        }
                    }

                    ZBackgroundSendMoveNext(lContext);
                }

                private void ZBackgroundSendAppendBytes(bool pSecret, cBytes pBytes)
                {
                    // DO NOT LOG THE parameters: the bytes may be secret
                    mBackgroundSendBuffer.AddRange(pBytes);
                    if (pSecret) mBackgroundSendTraceBuffer.Add(cASCII.NUL);
                    else mBackgroundSendTraceBuffer.AddRange(pBytes);
                }

                private void ZBackgroundSendMoveNext(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(true, nameof(cCommandPipeline), nameof(ZBackgroundSendMoveNext));

                    if (mCurrentCommand.MoveNext()) return;

                    mBackgroundSendBuffer.Add(cASCII.CR);
                    mBackgroundSendBuffer.Add(cASCII.LF);

                    mBackgroundSendTraceBuffer.Add(cASCII.CR);
                    mBackgroundSendTraceBuffer.Add(cASCII.LF);

                    // the current command can be added to the list of active commands now
                    lock (mPipelineLock)
                    {
                        mActiveCommands.Add(mCurrentCommand);
                        mCurrentCommand.SetActive(lContext);
                        mCurrentCommand = null;
                    }
                }

                public async Task ZBackgroundSendWriteAndClearBufferAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendWriteAndClearBufferAsync));

                    lContext.TraceVerbose("sending {0}", mBackgroundSendTraceBuffer);
                    mSynchroniser.InvokeNetworkActivity(mBackgroundSendTraceBufferStartPoints, mBackgroundSendTraceBuffer, lContext);

                    await mConnection.WriteAsync(mBackgroundSendBuffer.ToArray(), mBackgroundCancellationTokenSource.Token, lContext).ConfigureAwait(false);

                    mBackgroundSendBuffer.Clear();
                    mBackgroundSendTraceBufferStartPoints.Clear();
                    mBackgroundSendTraceBuffer.Clear();
                }
            }
        }
    }
}