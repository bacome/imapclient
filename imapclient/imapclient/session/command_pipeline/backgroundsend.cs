using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                private async Task ZBackgroundSendAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewRootMethod(true, nameof(cCommandPipeline), nameof(ZBackgroundSendAsync));

                    mBackgroundSendBuffer.SetTracing(lContext);

                    if (mCurrentCommand != null) await ZBackgroundSendAppendDataAndMoveNextAsync(lContext).ConfigureAwait(false);

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

                        if (mCurrentCommand.State == eCommandState.current)
                        {
                            if (ZBackgroundSendAppendLiteralHeader())
                            {
                                mCurrentCommand.WaitingForContinuationRequest = true;
                                break;
                            }

                            await ZBackgroundSendAppendDataAndMoveNextAsync(lContext).ConfigureAwait(false);
                        }
                        else ZBackgroundSendTerminateCommand(lContext);
                    }
                
                    await mBackgroundSendBuffer.WriteAsync(lContext).ConfigureAwait(false);
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
                                if (lCommand.UIDValidity != null && lCommand.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.MessageCache.UIDValidity) lCommand.SetException(new cUIDValidityException(lContext), lContext);
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

                    mBackgroundSendBuffer.AddTag(mCurrentCommand.Tag);
                }

                private bool ZBackgroundSendAppendLiteralHeader()
                {
                    // if the current part is a literal, appends the literal header
                    //  if the current part is a synchronising literal, returns true

                    if (!(mCurrentCommand.CurrentPart() is cLiteralCommandPartBase lLiteral)) return false;

                    bool lSynchronising;

                    if (mLiteralPlus || mLiteralMinus && lLiteral.Length < 4097) lSynchronising = false;
                    else lSynchronising = true;

                    mBackgroundSendBuffer.AddLiteralHeader(lLiteral.Secret, lLiteral.Binary, lLiteral.Length, lSynchronising);

                    return lSynchronising;
                }

                private async Task ZBackgroundSendAppendAllTextPartsAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendAppendAllTextPartsAsync));

                    do
                    {
                        if (mCurrentCommand.CurrentPart() is cTextCommandPart lText) await ZBackgroundSendAppendDataAsync(lText.Secret, lText.Bytes, lText.Bytes.Count, null, lContext).ConfigureAwait(false);
                        else throw new cInternalErrorException();
                    }
                    while (mCurrentCommand.MoveNext());

                    mBackgroundSendBuffer.AddCRLF();
                }

                private async Task ZBackgroundSendAppendDataAndMoveNextAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendAppendDataAndMoveNextAsync));

                    switch (mCurrentCommand.CurrentPart())
                    {
                        case cTextCommandPart lText:

                            await ZBackgroundSendAppendDataAsync(lText.Secret, lText.Bytes, lText.Bytes.Count, null, lContext).ConfigureAwait(false);
                            break;

                        case cLiteralCommandPart lLiteral:

                            await ZBackgroundSendAppendDataAsync(lLiteral.Secret, lLiteral.Bytes, lLiteral.Bytes.Count, lLiteral.Increment, lContext).ConfigureAwait(false);
                            break;

                        case cStreamCommandPart lStream:

                            await ZBackgroundSendAppendStreamDataAsync(lStream.Secret, lStream.Stream, lStream.Length, lStream.Increment, lStream.ReadConfiguration, lContext).ConfigureAwait(false);
                            break;

                        case cMultiPartLiteralCommandPart lMultiPart:

                            foreach (var lPart in lMultiPart.Parts)
                            {
                                switch (lPart)
                                {
                                    case cMultiPartLiteralPart lLiteral:

                                        await ZBackgroundSendAppendDataAsync(lMultiPart.Secret, lLiteral.Bytes, lLiteral.Bytes.Count, lLiteral.Increment, lContext).ConfigureAwait(false);
                                        break;

                                    case cMultiPartLiteralStreamPart lStream:

                                        await ZBackgroundSendAppendStreamDataAsync(lMultiPart.Secret, lStream.Stream, lStream.Length, lStream.Increment, lStream.ReadConfiguration, lContext).ConfigureAwait(false);
                                        break;

                                    default:

                                        throw new cInternalErrorException();
                                }
                            }

                            break;

                        default:

                            throw new cInternalErrorException();
                    }

                    if (mCurrentCommand.MoveNext()) return;

                    ZBackgroundSendTerminateCommand(lContext);
                }

                private void ZBackgroundSendTerminateCommand(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendTerminateCommand));

                    mBackgroundSendBuffer.AddCRLF();

                    lock (mPipelineLock)
                    {
                        if (mCurrentCommand.State == eCommandState.current)
                        {
                            mActiveCommands.Add(mCurrentCommand);
                            mCurrentCommand.SetActive(lContext);
                        }

                        mCurrentCommand = null;
                    }
                }

                private async Task ZBackgroundSendAppendStreamDataAsync(bool pSecret, Stream pStream, uint pLength, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cTrace.cContext pParentContext)
                {
                    // don't log details of the data (i.e. the length) - it may be secret
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendAppendStreamDataAsync));

                    if (pLength == 0) return;

                    long lBytesToGo = pLength;

                    Stopwatch lStopwatch = new Stopwatch();
                    cBatchSizer lReadSizer = new cBatchSizer(pReadConfiguration);
                    byte[] lBuffer = new byte[lReadSizer.Current];

                    while (lBytesToGo > 0)
                    {
                        int lReadSize = (int)Math.Min(lReadSizer.Current, lBytesToGo);

                        if (lReadSize > lBuffer.Length) lBuffer = new byte[lReadSize];

                        if (!pSecret) lContext.TraceVerbose("reading {0} bytes from stream", lReadSize);

                        lStopwatch.Restart();

                        int lBytesRead = await pStream.ReadAsync(lBuffer, 0, lReadSize, mBackgroundCancellationTokenSource.Token).ConfigureAwait(false);
                            
                        lStopwatch.Stop();

                        if (lBytesRead == 0) throw new cStreamRanOutOfDataException();

                        // store the time taken so the next read is a better size
                        lReadSizer.AddSample(lBytesRead, lStopwatch.ElapsedMilliseconds);

                        await ZBackgroundSendAppendDataAsync(pSecret, lBuffer, lBytesRead, pIncrement, lContext).ConfigureAwait(false);

                        lBytesToGo -= lBytesRead;
                    }
                }

                private async Task ZBackgroundSendAppendDataAsync(bool pSecret, IEnumerable<byte> pBytes, int pCount, Action<int> pIncrement, cTrace.cContext pParentContext)
                {
                    // don't log details of the data (i.e. the length) - it may be secret
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendAppendDataAsync));

                    mBackgroundSendBuffer.BeginAddBytes(pSecret, pIncrement);

                    int lSize = mConnection.CurrentWriteSize;

                    foreach (var lByte in pBytes)
                    {
                        if (pCount-- == 0) break;

                        if (mBackgroundSendBuffer.Count >= lSize)
                        {
                            await mBackgroundSendBuffer.WriteAsync(lContext).ConfigureAwait(false);
                            lSize = mConnection.CurrentWriteSize;
                        }

                        mBackgroundSendBuffer.AddByte(lByte);
                    }

                    mBackgroundSendBuffer.EndAddBytes();
                }
            }
        }
    }
}