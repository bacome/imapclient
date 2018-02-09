using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public enum eQuotedPrintableSourceType { Binary, CRLFTerminatedLines, LFTerminatedLines }
    public enum eQuotedPrintableQuotingRule { Minimal, EBCDIC }

    public class cConvertToQuotedPrintableConfiguration
    {
        /**<summary>The timeout for the operation. May be <see cref="Timeout.Infinite"/>.</summary>*/
        public readonly int Timeout;

        /**<summary>The cancellation token for the operation. May be <see cref="CancellationToken.None"/>.</summary>*/
        public readonly CancellationToken CancellationToken;

        /// <summary>
        /// The progress-increment callback for the operation. May be <see langword="null"/>. Invoked once for each batch of bytes encoded, the argument specifies how many source bytes were encoded.
        /// </summary>
        /// <inheritdoc cref="cAPIDocumentationTemplate.Event" select="remarks"/>
        public readonly Action<int> Increment;

        public readonly cBatchSizerConfiguration ReadConfiguration;
        public readonly cBatchSizerConfiguration WriteConfiguration;

        public cConvertToQuotedPrintableConfiguration(int pTimeout, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            Increment = null;
            ReadConfiguration = pReadConfiguration;
            WriteConfiguration = pWriteConfiguration;
        }

        public cConvertToQuotedPrintableConfiguration(CancellationToken pCancellationToken, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            Increment = pIncrement;
            ReadConfiguration = pReadConfiguration;
            WriteConfiguration = pWriteConfiguration;
        }
    }

    public partial class cIMAPClient
    {
        public int ConvertToQuotedPrintable(Stream pSource, eQuotedPrintableSourceType pSourceType, eQuotedPrintableQuotingRule pQuotingRule, Stream pTarget = null, cConvertToQuotedPrintableConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertToQuotedPrintable));
            var lTask = ZConvertToQuotedPrintableAsync(pSource, pSourceType, pQuotingRule, pTarget, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<int> ConvertToQuotedPrintableAsync(Stream pSource, eQuotedPrintableSourceType pSourceType, eQuotedPrintableQuotingRule pQuotingRule, Stream pTarget = null, cConvertToQuotedPrintableConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(ConvertToQuotedPrintableAsync));
            return ZConvertToQuotedPrintableAsync(pSource, pSourceType, pQuotingRule, pTarget, pConfiguration, lContext);
        }

        private async Task<int> ZConvertToQuotedPrintableAsync(Stream pSource, eQuotedPrintableSourceType pSourceType, eQuotedPrintableQuotingRule pQuotingRule, Stream pTarget, cConvertToQuotedPrintableConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZConvertToQuotedPrintableAsync), pSourceType, pQuotingRule);

            if (pSource == null) throw new ArgumentNullException(nameof(pSource));
            if (!pSource.CanRead) throw new ArgumentOutOfRangeException(nameof(pSource));

            if (pTarget != null && !pTarget.CanWrite) throw new ArgumentOutOfRangeException(nameof(pSource));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    var lReadSizer = new cBatchSizer(mQuotedPrintableReadConfiguration);
                    var lWriteSizer = new cBatchSizer(mQuotedPrintableWriteConfiguration);
                    return await ZZConvertToQuotedPrintableAsync(lMC, pSource, pSourceType, pQuotingRule, pTarget, null, lReadSizer, lWriteSizer, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lReadSizer = new cBatchSizer(pConfiguration.ReadConfiguration ?? mQuotedPrintableReadConfiguration);
                var lWriteSizer = new cBatchSizer(pConfiguration.WriteConfiguration ?? mQuotedPrintableWriteConfiguration);
                return await ZZConvertToQuotedPrintableAsync(lMC, pSource, pSourceType, pQuotingRule, pTarget, pConfiguration.Increment, lReadSizer, lWriteSizer, lContext).ConfigureAwait(false);
            }
        }

        private async Task<int> ZZConvertToQuotedPrintableAsync(cMethodControl pMC, Stream pSource, eQuotedPrintableSourceType pSourceType, eQuotedPrintableQuotingRule pQuotingRule, Stream pTarget, Action<int> pIncrement, cBatchSizer pReadSizer, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZConvertToQuotedPrintableAsync), pMC, pSourceType, pQuotingRule);

            byte[] lReadBuffer = null;
            Stopwatch lStopwatch = new Stopwatch();

            bool lPendingCR = false;
            cQuotedPrintableOutput lOutput = new cQuotedPrintableOutput(pQuotingRule, pTarget, pIncrement, pWriteSizer);

            while (true)
            {
                // read some data
                int lCurrent = pReadSizer.Current;
                if (lReadBuffer == null || lCurrent > lReadBuffer.Length) lReadBuffer = new byte[lCurrent];
                lStopwatch.Restart();
                if (pSource.CanTimeout) pSource.ReadTimeout = pMC.Timeout;
                int lBytesReadIntoBuffer = await pSource.ReadAsync(lReadBuffer, 0, lReadBuffer.Length, pMC.CancellationToken).ConfigureAwait(false);
                lStopwatch.Stop();
                if (lBytesReadIntoBuffer == 0) break;
                pReadSizer.AddSample(lBytesReadIntoBuffer, lStopwatch.ElapsedMilliseconds);

                // process the data

                int lReadBufferPosition = 0;

                while (lReadBufferPosition < lBytesReadIntoBuffer)
                {
                    var lByte = lReadBuffer[lReadBufferPosition++];

                    if (pSourceType == eQuotedPrintableSourceType.CRLFTerminatedLines)
                    {
                        if (lPendingCR)
                        {
                            lPendingCR = false;

                            if (lByte == cASCII.LF)
                            {
                                lOutput.AddHardLineBreak(2);
                                continue;
                            }

                            lOutput.Add(cASCII.CR);
                        }

                        if (lByte == cASCII.CR)
                        {
                            lPendingCR = true;
                            continue;
                        }
                    }
                    else if (pSourceType == eQuotedPrintableSourceType.LFTerminatedLines)
                    {
                        if (lByte == cASCII.LF)
                        {
                            lOutput.AddHardLineBreak(1);
                            continue;
                        }
                    }

                    lOutput.Add(lByte);
                }

                // write the data out in chunks determined by the write sizer
                await lOutput.writeasync(pMC).configureawait(false);
            }

            // flush any cached output data
            if (lPendingCR) lOutput.Add(cASCII.CR);
            await lOutput.flushasync(pMC).configureawait(false);

            // done
            return lOutput.byteswritten;
        }

        private class cQuotedPrintableOutput
        {
            private static readonly cBytes kEBCDICNotSome = new cBytes("!\"#$@[\\]^`{|}~");
            private static readonly cBytes kEQUALSCRLF = new cBytes("=\r\n");
            private static readonly cBytes kCRLF = new cBytes("\r\n");

            private readonly eQuotedPrintableQuotingRule mQuotingRule;
            private readonly Stream mTarget;
            private readonly Action<int> mIncrement;
            private readonly cBatchSizer mWriteSizer;

            private readonly Queue<cLine> mPendingLines = new Queue<cLine>();

            private List<byte> mPendingBytes = new List<byte>();
            private int mPendingBytesInputByteCount = 0;
            private readonly List<byte> mPendingWSP = new List<byte>();
            private readonly List<byte> mPendingNonWSP = new List<byte>();

            private readonly List<byte> mCarriedWSP = new List<byte>();

            private int mBytesWritten = 0;



            public cQuotedPrintableOutput(eQuotedPrintableQuotingRule pQuotingRule, Stream pTarget, Action<int> pIncrement, cBatchSizer pWriteSizer)
            {
                mQuotingRule = pQuotingRule;

                if (pTarget == null) mTarget = Stream.Null;
                else mTarget = pTarget;

                mIncrement = pIncrement;
                mWriteSizer = pWriteSizer ?? throw new ArgumentNullException(nameof(pWriteSizer));
            }

            public void Add(byte pByte)
            {
                bool lNeedsQuoting;

                if (pByte == cASCII.TAB) lNeedsQuoting = false;
                else if (pByte < cASCII.SPACE) lNeedsQuoting = true;
                else if (pByte == cASCII.EQUALS) lNeedsQuoting = true;
                else if (pByte < cASCII.DEL)
                {
                    if (mQuotingRule == eQuotedPrintableQuotingRule.EBCDIC && kEBCDICNotSome.Contains(pByte)) lNeedsQuoting = true;
                    else lNeedsQuoting = false;
                }
                else lNeedsQuoting = true;

                int lCount;
                if (lNeedsQuoting) lCount = 3;
                else lCount = 1;

                if (mPendingBytes.Count + mPendingWSP.Count + mPendingNonWSP.Count + lCount > 76) ZSoftLineBreak();

                if (mPendingNonWSP.Count != 0) throw new cInternalErrorException();

                if (pByte == cASCII.TAB || pByte == cASCII.SPACE)
                {
                    mPendingWSP.Add(pByte);
                    return;
                }

                if (mPendingBytes.Count + mPendingWSP.Count + lCount == 76)
                {
                    if (lNeedsQuoting)
                    {
                        mPendingNonWSP.Add(cASCII.EQUALS);
                        mPendingNonWSP.AddRange(cTools.ByteToHexBytes(pByte));
                    }
                    else mPendingNonWSP.Add(pByte);

                    return;
                }

                mPendingBytes.AddRange(mPendingWSP);
                mPendingBytesInputByteCount += mPendingWSP.Count;
                mPendingWSP.Clear();

                if (lNeedsQuoting)
                {
                    mPendingBytes.Add(cASCII.EQUALS);
                    mPendingBytes.AddRange(cTools.ByteToHexBytes(pByte));
                }
                else mPendingBytes.Add(pByte);

                mPendingBytesInputByteCount++;
            }

            public void AddHardLineBreak(int pInputBytes)
            {
                if (mPendingNonWSP.Count > 0)
                {
                    mPendingBytes.AddRange(mPendingWSP);
                    mPendingBytesInputByteCount += mPendingWSP.Count;
                    mPendingWSP.Clear();

                    mPendingBytes.AddRange(mPendingNonWSP);
                    mPendingBytesInputByteCount++;
                    mPendingNonWSP.Clear();
                }
                else if (mPendingWSP.Count > 0)
                {
                    int lWSPToAdd = Math.Max(74 - mPendingBytes.Count, mPendingWSP.Count);

                    if (lWSPToAdd > 0)
                    {
                        for (int i = 0; i < lWSPToAdd - 1; i++) mPendingBytes.Add(mPendingWSP[i]);
                        mPendingBytes.Add(cASCII.EQUALS);
                        mPendingBytes.AddRange(cTools.ByteToHexBytes(mPendingWSP[lWSPToAdd - 1]));
                        mPendingBytesInputByteCount += lWSPToAdd;

                        mCarriedWSP.Clear();
                        for (int i = lWSPToAdd; i < mPendingWSP.Count; i++) mCarriedWSP.Add(mPendingWSP[i]);
                        mPendingWSP.Clear();
                        mPendingWSP.AddRange(mCarriedWSP);
                    }
                }

                mPendingBytes.AddRange(kCRLF);
                mPendingBytesInputByteCount += pInputBytes;

                mPendingLines.Enqueue(new cLine(mPendingBytes.ToArray(), mPendingBytesInputByteCount));

                mPendingBytes.Clear();
                mPendingBytesInputByteCount = 0;
            }

            public async Task WriteAsync(cMethodControl pMC)
            {
                // if there are no pending lines, return
                // if the current output buffer is empty, find the current size and possibly expand the output buffer. Note that the output buffer has to be 78 bytes bigger than the batch sizer because we will only output whole lines
                // while there are lines
                //  add the bytes of the line to the buffer
                //  if buffer is over current, write async, increment bytes wrtiien, give feedback (incrent), re-get current, possibly re-size buffer


                // 


                // find the current size
                //  

                while (true)
                {
                    if ()
                }
            }

            private void ZSoftLineBreak()
            {
                // note that flush may have to call this 0, 1, or 2 times.

                if (mPendingBytes.Count + mPendingWSP.Count + 1 > 76)
                {
                    if (mPendingWSP.Count == 0) throw new cInternalErrorException();

                    if (mPendingWSP.Count > 1)
                    {
                        for (int i = 0; i < mPendingWSP.Count - 1; i++) mPendingBytes.Add(mPendingWSP[i]);
                        mPendingBytesInputByteCount += mPendingWSP.Count - 1;
                        byte lCarriedWSP = mPendingWSP[mPendingWSP.Count - 1];
                        mPendingWSP.Clear();
                        mPendingWSP.Add(lCarriedWSP);
                    }
                }
                else
                {
                    mPendingBytes.AddRange(mPendingWSP);
                    mPendingBytesInputByteCount += mPendingWSP.Count;
                    mPendingWSP.Clear();
                }

                mPendingBytes.AddRange(kEQUALSCRLF);

                mPendingLines.Enqueue(new cLine(mPendingBytes.ToArray(), mPendingBytesInputByteCount));

                mPendingBytes.Clear();
                mPendingBytesInputByteCount = 0;

                if (mPendingNonWSP.Count > 0)
                {
                    mPendingBytes.AddRange(mPendingWSP);
                    mPendingBytesInputByteCount += mPendingWSP.Count;
                    mPendingWSP.Clear();

                    mPendingBytes.AddRange(mPendingNonWSP);
                    mPendingBytesInputByteCount++;
                    mPendingNonWSP.Clear();
                }
            }

            private class cLine
            {
                private readonly byte[] mBytes;
                private int mInputByteCount = 0;

                public cLine(byte[] pBytes, int pInputByteCount)
                {
                    mBytes = pBytes;
                    mInputByteCount = pInputByteCount;
                }
            }
        }

        private static void _TestQuotedPrintable()
        {
            // test lf, crlf and binary
            //  test lines ending exactly at 75, 76 and 77 chars on;
            //   a non-wsp that doesn't need to be encoded
            //   a non-wsp that does need to be encoded
            //   a space
            //   a tab

            // test that the increment bytes adds up ok
            // test that the output bytes is accurate

            // test that no output lines are > 76 chars

            // test a stream that ends with a CRLF and one that doesn't

            // test naked CR and naked LF in a CRLF stream.
            //  test naked CR at end of stream

            // test the two differnt quoting rules

            // test that the trailing WSP is output in the order it is on input

            // roundtrip testing
        }
    }
}