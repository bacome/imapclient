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

                    if (mCurrentCommand != null)
                    {
                        mBackgroundSendTraceBufferStartPoints.Add(0);
                        await ZBackgroundSendAppendDataAndMoveNextAsync(lContext).ConfigureAwait(false);
                    }

                    while (true)
                    {
                        if (mCurrentCommand == null)
                        {
                            ZBackgroundSendDequeueAndAppendTag(lContext);
                            if (mCurrentCommand == null) break;

                            if (mCurrentCommand.IsAuthentication)
                            {
                                await ZBackgroundSendAppendAllTextPartsAsync(lContext).ConfigureAwait(false);
                                mCurrentCommand.WaitingForContinuationRequest = true;
                                break;
                            }
                        }

                        if (ZBackgroundSendAppendLiteralHeader())
                        {
                            mCurrentCommand.WaitingForContinuationRequest = true;
                            break;
                        }

                        await ZBackgroundSendAppendDataAndMoveNextAsync(lContext).ConfigureAwait(false);
                    }

                    await ZBackgroundSendWriteAndClearBuffersAsync(lContext).ConfigureAwait(false);
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

                private async Task ZBackgroundSendAppendAllTextPartsAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(true, nameof(cCommandPipeline), nameof(ZBackgroundSendAppendAllTextPartsAsync));

                    do
                    {
                        if (mCurrentCommand.CurrentPart() is cTextCommandPart lText) await ZBackgroundSendAppendDataAsync(lText.Secret, lText.Bytes, lText.Bytes.Count, lContext).ConfigureAwait(false);
                        else throw new cInternalErrorException();
                    }
                    while (mCurrentCommand.MoveNext());

                    mBackgroundSendBuffer.Add(cASCII.CR);
                    mBackgroundSendBuffer.Add(cASCII.LF);

                    mBackgroundSendTraceBuffer.Add(cASCII.CR);
                    mBackgroundSendTraceBuffer.Add(cASCII.LF);
                }

                private async Task ZBackgroundSendAppendDataAndMoveNextAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(true, nameof(cCommandPipeline), nameof(ZBackgroundSendAppendDataAndMoveNextAsync));

                    switch (mCurrentCommand.CurrentPart())
                    {
                        case cTextCommandPart lText:

                            await ZBackgroundSendAppendDataAsync(lText.Secret, lText.Bytes, lText.Bytes.Count, lContext).ConfigureAwait(false);
                            break;

                        case cLiteralCommandPart lLiteral:

                            await ZBackgroundSendAppendDataAsync(lLiteral.Secret, lLiteral.Bytes, lLiteral.Bytes.Count, lContext).ConfigureAwait(false);
                            break;

                        case cStreamCommandPart lStream:

                            int lBytesRemaining = lStream.Length;

                            if (lBytesRemaining > 0)
                            {
                                Stopwatch lStopwatch = new Stopwatch();
                                cBatchSizer lReadSizer = new cBatchSizer(lStream.ReadConfiguration);
                                byte[] lBuffer = new byte[lReadSizer.Current];

                                while (lBytesRemaining > 0)
                                {
                                    int lReadSize = Math.Min(lReadSizer.Current, lBytesRemaining);

                                    if (lReadSize > lBuffer.Length) lBuffer = new byte[lReadSize];

                                    lStopwatch.Restart();
                                    int lCount = await lStream.Stream.ReadAsync(lBuffer, 0, lReadSize, mBackgroundCancellationTokenSource.Token).ConfigureAwait(false);
                                    lStopwatch.Stop();

                                    // store the time taken so the next read is a better size
                                    lReadSizer.AddSample(lCount, lStopwatch.ElapsedMilliseconds);

                                    await ZBackgroundSendAppendDataAsync(lStream.Secret, lBuffer, lCount, lContext).ConfigureAwait(false);

                                    lBytesRemaining -= lCount;
                                }
                            }
                            
                            break;

                        default:

                            throw new cInternalErrorException();
                    }

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

                private async Task ZBackgroundSendAppendDataAsync(bool pSecret, IEnumerable<byte> pBytes, int pCount, cTrace.cContext pParentContext)
                {
                    // don't log details of the data - it may be secret
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendAppendDataAsync), pSecret);

                    bool lFirst = true;
                    int lSize = 1;

                    foreach (var lByte in pBytes)
                    {
                        if (pCount-- == 0) break;

                        if (lFirst)
                        {
                            lSize = mConnection.CurrentWriteSize;
                            if (pSecret) mBackgroundSendTraceBuffer.Add(cASCII.NUL);
                            lFirst = false;
                        }

                        if (mBackgroundSendBuffer.Count >= lSize)
                        {
                            await ZBackgroundSendWriteAndClearBuffersAsync(lContext).ConfigureAwait(false);

                            lSize = mConnection.CurrentWriteSize;

                            mBackgroundSendTraceBufferStartPoints.Add(0);
                            if (pSecret) mBackgroundSendTraceBuffer.Add(cASCII.NUL);
                        }

                        mBackgroundSendBuffer.Add(lByte);
                        if (!pSecret) mBackgroundSendTraceBuffer.Add(lByte);
                    }
                }

                public async Task ZBackgroundSendWriteAndClearBuffersAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendWriteAndClearBuffersAsync));

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