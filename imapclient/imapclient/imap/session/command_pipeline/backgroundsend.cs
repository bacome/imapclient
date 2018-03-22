using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

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

                    // initialise the send buffer
                    mBackgroundSendBuffer.SetTracing(lContext);

                    // send the literal data that has just been requested (if any)

                    cLiteralCommandPartBase lLiteral;

                    lock (mPipelineLock)
                    {
                        if (mCurrentCommand == null) lLiteral = null;
                        else
                        {
                            lLiteral = mCurrentCommand.GetCurrentPart() as cLiteralCommandPartBase;
                            if (lLiteral == null) throw new cInternalErrorException(lContext);
                        }
                    }

                    if (lLiteral != null) await ZBackgroundSendAppendLiteralDataAsync(lLiteral, lContext).ConfigureAwait(false);

                    // main processing
                    await ZBackgroundSendWorkerAsync(lContext);
                
                    // send any bytes that are remaining in the buffer
                    if (mBackgroundSendBuffer.Count > 0) await mBackgroundSendBuffer.WriteAsync(lContext).ConfigureAwait(false);
                }

                private async Task ZBackgroundSendWorkerAsync(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewRootMethod(true, nameof(cCommandPipeline), nameof(ZBackgroundSendWorkerAsync));

                    // outer loop is for sending literal data outside the pipeline lock (required because we want this to be done in parallel with the receive)
                    //
                    while (true)
                    {
                        cLiteralCommandPartBase lLiteral;

                        lock (mPipelineLock)
                        {
                            // inner loop is for advancing to the next command part to send
                            //
                            while (true)
                            {
                                cCommandPart lPart;

                                if (mCurrentCommand == null)
                                {
                                    // try to get another command to send (there might not be one available)
                                    //
                                    while (mQueuedCommands.Count > 0)
                                    {
                                        var lCommand = mQueuedCommands.Dequeue();

                                        // the command may have been cancelled by the enqueuer
                                        if (lCommand.State == eCommandState.queued)
                                        {
                                            // the uidvalidity may have changed                                        
                                            if (lCommand.UIDValidity != null && lCommand.UIDValidity != mMailboxCache?.SelectedMailboxDetails?.MessageCache.UIDValidity) lCommand.SetException(new cUIDValidityException(lContext), lContext);
                                            else
                                            {
                                                // found a command to send
                                                lCommand.SetSending(lContext);
                                                mCurrentCommand = lCommand;
                                                break;
                                            }
                                        }
                                    }

                                    if (mCurrentCommand == null) return; // nothing else to send

                                    // add the command tag to the send buffer
                                    mBackgroundSendBuffer.AddTag(mCurrentCommand.Tag);

                                    // get the first part
                                    lPart = mCurrentCommand.GetCurrentPart();
                                }
                                else
                                {
                                    // check if the server has completed the command before we have finished sending it
                                    if (mCurrentCommand.State == eCommandState.complete)
                                    {
                                        // terminate the current command line
                                        mBackgroundSendBuffer.AddCRLF();
                                        mCurrentCommand = null;
                                        continue;
                                    }
                                    
                                    // check if we have sent everything that there is to send
                                    if (!mCurrentCommand.MoveNext())
                                    {
                                        // terminate the current command line
                                        mBackgroundSendBuffer.AddCRLF();

                                        // authentication is a special case ... the challenge response mechanism is in the background task
                                        if (mCurrentCommand.IsAuthentication)
                                        {
                                            mCurrentCommand.SetAwaitingContinuation(lContext);
                                            return;
                                        }

                                        // put the command in the list of active commands 
                                        mCurrentCommand.SetSent(lContext);
                                        mActiveCommands.Add(mCurrentCommand);
                                        mCurrentCommand = null;
                                        continue;
                                    }

                                    lPart = mCurrentCommand.GetCurrentPart();
                                }

                                if (lPart is cTextCommandPart lText)
                                {
                                    mBackgroundSendBuffer.AddText(lText.Secret, lText.Bytes);
                                    continue;
                                }

                                lLiteral = lPart as cLiteralCommandPartBase;

                                if (lLiteral == null) throw new cInternalErrorException(lContext);

                                bool lSynchronising;

                                if (mLiteralPlus || mLiteralMinus && lLiteral.Length < 4097) lSynchronising = false;
                                else lSynchronising = true;

                                mBackgroundSendBuffer.AddLiteralHeader(lLiteral.Secret, lLiteral.Binary, lLiteral.Length, lSynchronising);

                                if (lSynchronising)
                                {
                                    mCurrentCommand.SetAwaitingContinuation(lContext);
                                    return;
                                }

                                break;
                            }
                        }

                        // send literal data
                        await ZBackgroundSendAppendLiteralDataAsync(lLiteral, lContext).ConfigureAwait(false);
                    }
                }

                private async Task ZBackgroundSendAppendLiteralDataAsync(cLiteralCommandPartBase pLiteral, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ZBackgroundSendAppendDataAsync), pLiteral);

                    switch (pLiteral)
                    {
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

                                        throw new cInternalErrorException(lContext, 1);
                                }
                            }

                            break;

                        default:

                            throw new cInternalErrorException(lContext, 2);
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