using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        private static readonly eQuotedPrintableSourceType kDefaultQuotedPrintableSourceType = Environment.NewLine == "\n" ? eQuotedPrintableSourceType.LFTerminatedLines : eQuotedPrintableSourceType.CRLFTerminatedLines;

        public int ConvertToQuotedPrintable(Stream pSource, Stream pTarget = null, cConvertToQuotedPrintableConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(ConvertToQuotedPrintable), 1);
            var lTask = ZConvertToQuotedPrintableAsync(pSource, kDefaultQuotedPrintableSourceType, eQuotedPrintableQuotingRule.EBCDIC, pTarget, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<int> ConvertToQuotedPrintableAsync(Stream pSource, Stream pTarget = null, cConvertToQuotedPrintableConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(ConvertToQuotedPrintableAsync), 1);
            return ZConvertToQuotedPrintableAsync(pSource, kDefaultQuotedPrintableSourceType, eQuotedPrintableQuotingRule.EBCDIC, pTarget, pConfiguration, lContext);
        }

        public int ConvertToQuotedPrintable(Stream pSource, eQuotedPrintableSourceType pSourceType, eQuotedPrintableQuotingRule pQuotingRule, Stream pTarget = null, cConvertToQuotedPrintableConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(ConvertToQuotedPrintable), 2);
            var lTask = ZConvertToQuotedPrintableAsync(pSource, pSourceType, pQuotingRule, pTarget, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<int> ConvertToQuotedPrintableAsync(Stream pSource, eQuotedPrintableSourceType pSourceType, eQuotedPrintableQuotingRule pQuotingRule, Stream pTarget = null, cConvertToQuotedPrintableConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(ConvertToQuotedPrintableAsync), 2);
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
            cQuotedPrintableOutput lOutput = new cQuotedPrintableOutput(pMC, pQuotingRule, pTarget, mSynchroniser, pIncrement, pWriteSizer, lContext);

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
                                await lOutput.AddHardLineBreakAsync(2).ConfigureAwait(false);
                                continue;
                            }

                            await lOutput.AddAsync(cASCII.CR).ConfigureAwait(false);
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
                            await lOutput.AddHardLineBreakAsync(1).ConfigureAwait(false);
                            continue;
                        }
                    }

                    await lOutput.AddAsync(lByte).ConfigureAwait(false);
                }
            }

            // flush any cached output data
            if (lPendingCR) await lOutput.AddAsync(cASCII.CR).ConfigureAwait(false);
            await lOutput.FlushAsync().ConfigureAwait(false);

            // done
            return lOutput.BytesWritten;
        }

        private class cQuotedPrintableOutput
        {
            private static readonly cBytes kEBCDICNotSome = new cBytes("!\"#$@[\\]^`{|}~");

            private readonly cMethodControl mMCForWrite;
            private readonly eQuotedPrintableQuotingRule mQuotingRule;
            private readonly Stream mTarget;
            private readonly cCallbackSynchroniser mSynchroniser;
            private readonly Action<int> mIncrement;

            private List<byte> mPendingBytes = new List<byte>();
            private int mPendingBytesInputByteCount = 0;
            private readonly List<byte> mPendingWSP = new List<byte>();
            private readonly List<byte> mPendingNonWSP = new List<byte>();

            private readonly cBatchSizer mWriteSizer;
            private byte[] mWriteBuffer = null;
            private int mBytesInWriteBuffer = 0;
            private readonly Stopwatch mStopwatch = new Stopwatch();
            private int mWriteBufferInputByteCount = 0;

            private readonly cTrace.cContext mContextForIncrement;

            private int mBytesWritten = 0;

            public cQuotedPrintableOutput(cMethodControl pMCForWrite, eQuotedPrintableQuotingRule pQuotingRule, Stream pTarget, cCallbackSynchroniser pSynchroniser, Action<int> pIncrement, cBatchSizer pWriteSizer, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewObject(nameof(cQuotedPrintableOutput), pMCForWrite, pQuotingRule);

                mMCForWrite = pMCForWrite;
                mQuotingRule = pQuotingRule;

                if (pTarget == null) mTarget = Stream.Null;
                else mTarget = pTarget;

                mSynchroniser = pSynchroniser;

                mIncrement = pIncrement;

                mWriteSizer = pWriteSizer ?? throw new ArgumentNullException(nameof(pWriteSizer));
                mContextForIncrement = lContext.NewMethod(nameof(cQuotedPrintableOutput), nameof(ZWriteBufferAsync));
            }

            public async Task AddAsync(byte pByte)
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

                if (mPendingBytes.Count + mPendingWSP.Count + mPendingNonWSP.Count + lCount > 76) await ZSoftLineBreakAsync().ConfigureAwait(false);

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

            public async Task AddHardLineBreakAsync(int pInputBytes)
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
                    int lWSPThatWillFit = 74 - mPendingBytes.Count;

                    if (lWSPThatWillFit < mPendingWSP.Count) await ZSoftLineBreakAsync().ConfigureAwait(false);

                    if (mPendingWSP.Count > 0)
                    {
                        for (int i = 0; i < mPendingWSP.Count - 1; i++) mPendingBytes.Add(mPendingWSP[i]);
                        mPendingBytes.Add(cASCII.EQUALS);
                        mPendingBytes.AddRange(cTools.ByteToHexBytes(mPendingWSP[mPendingWSP.Count - 1]));
                        mPendingBytesInputByteCount += mPendingWSP.Count;

                        mPendingWSP.Clear();
                    }
                }

                mPendingBytesInputByteCount += pInputBytes;

                await ZWriteLineAsync().ConfigureAwait(false);
                mPendingBytes.Clear();
                mPendingBytesInputByteCount = 0;
            }

            public async Task FlushAsync()
            {
                if (mPendingBytes.Count != 0 || mPendingWSP.Count != 0) await ZSoftLineBreakAsync().ConfigureAwait(false);
                if (mPendingBytes.Count != 0 || mPendingWSP.Count != 0) await ZSoftLineBreakAsync().ConfigureAwait(false);
                if (mBytesInWriteBuffer > 0) await ZWriteBufferAsync().ConfigureAwait(false);
            }

            public int BytesWritten => mBytesWritten;

            private async Task ZSoftLineBreakAsync()
            {
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

                mPendingBytes.Add(cASCII.EQUALS);

                await ZWriteLineAsync().ConfigureAwait(false);
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

            private async Task ZWriteLineAsync()
            {
                if (mBytesInWriteBuffer == 0)
                {
                    int lCurrent = Math.Max(mWriteSizer.Current, 78);
                    if (mWriteBuffer == null || lCurrent > mWriteBuffer.Length) mWriteBuffer = new byte[lCurrent];
                }
                else if (mBytesInWriteBuffer + mPendingBytes.Count + 2 > mWriteBuffer.Length)
                {
                    await ZWriteBufferAsync().ConfigureAwait(false);
                    mBytesInWriteBuffer = 0;
                    mWriteBufferInputByteCount = 0;
                }

                foreach (var lByte in mPendingBytes) mWriteBuffer[mBytesInWriteBuffer++] = lByte;
                mWriteBuffer[mBytesInWriteBuffer++] = cASCII.CR;
                mWriteBuffer[mBytesInWriteBuffer++] = cASCII.LF;

                mWriteBufferInputByteCount += mPendingBytesInputByteCount;
            }

            private async Task ZWriteBufferAsync()
            {
                // write 
                mStopwatch.Restart();
                if (mTarget.CanTimeout) mTarget.WriteTimeout = mMCForWrite.Timeout;
                await mTarget.WriteAsync(mWriteBuffer, 0, mBytesInWriteBuffer, mMCForWrite.CancellationToken).ConfigureAwait(false);
                mStopwatch.Stop();
                mWriteSizer.AddSample(mBytesInWriteBuffer, mStopwatch.ElapsedMilliseconds);

                // keep track of the number of bytes written
                mBytesWritten += mBytesInWriteBuffer;

                // feedback
                mSynchroniser.InvokeActionInt(mIncrement, mWriteBufferInputByteCount, mContextForIncrement);
            }
        }

        private static void _TestQuotedPrintable(cTrace.cContext pParentContext)
        {
            _TestQuotedPrintable(
                "1",
                new string[] 
                {
                    //        1         2         3         4         5          6         7
                    // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuvw",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs=",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst=",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu=",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqr ",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs ",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst ",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu ",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv ",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqr\t",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrs\t",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrst\t",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu\t",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\t",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstu "
                    //        1         2         3         4         5          6         7
                    // 345678901234567890123456789012345678901234567890123 45678901234567890123456
                },
                pParentContext);



            // test naked CR and naked LF in a CRLF stream.
            //  test naked CR at end of stream

            // test that the trailing WSP is output in the order it is on input

            // test stream.null

            ;?;
        }


        private static void _TestQuotedPrintable(string pTestName, string[] pLines, cTrace.cContext pParentContext)
        {
            var lLFFalseMinimal = _TestQuotedPrintable(pTestName + ".lf.false.Mininal", pLines, eQuotedPrintableSourceType.LFTerminatedLines, false, eQuotedPrintableQuotingRule.Minimal, pParentContext);
            var lLFTrueMinimal = _TestQuotedPrintable(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableSourceType.LFTerminatedLines, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);

            if (lLFFalseMinimal >= lLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");

            var lCRLFFalseMinimal = _TestQuotedPrintable(pTestName + ".crlf.false.Mininal", pLines, eQuotedPrintableSourceType.CRLFTerminatedLines, false, eQuotedPrintableQuotingRule.Minimal, pParentContext);

            if (lLFFalseMinimal != lCRLFFalseMinimal) throw new cTestsException(pTestName + ".compare.2");

            var lCRLFTrueMinimal = _TestQuotedPrintable(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableSourceType.CRLFTerminatedLines, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);

            if (lCRLFFalseMinimal >= lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.3");

            var lBinaryFalseMinimal = _TestQuotedPrintable(pTestName + ".binary.false.Mininal", pLines, eQuotedPrintableSourceType.Binary, false, eQuotedPrintableQuotingRule.Minimal, pParentContext);

            if (lCRLFFalseMinimal >= lBinaryFalseMinimal) throw new cTestsException(pTestName + ".compare.4");

            var lBinaryTrueMinimal = _TestQuotedPrintable(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableSourceType.Binary, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);

            if (lBinaryFalseMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.5");

            var lLFFalseEBCDIC = _TestQuotedPrintable(pTestName + ".lf.false.EBCDIC", pLines, eQuotedPrintableSourceType.LFTerminatedLines, false, eQuotedPrintableQuotingRule.EBCDIC, pParentContext);

            if (lLFFalseMinimal >= lLFFalseEBCDIC) throw new cTestsException(pTestName + ".compare.6");
        }

        private static int _TestQuotedPrintable(string pTestName, string[] pLines, eQuotedPrintableSourceType pSourceType, bool pTerminateLastLine, eQuotedPrintableQuotingRule pQuotingRule, cTrace.cContext pParentContext)
        {
            int lBytesWritten;

            StringBuilder lBuilder = new StringBuilder();

            for (int i = 0; i < pLines.Length; i++)
            {
                lBuilder.Append(pLines[i]);

                if (i < pLines.Length - 1 || pTerminateLastLine)
                {
                    if (pSourceType == eQuotedPrintableSourceType.LFTerminatedLines) lBuilder.Append('\n');
                    else lBuilder.Append("\r\n");
                }
            }

            using (var lClient = new cIMAPClient())
            using (var lInput = new MemoryStream(Encoding.UTF8.GetBytes(lBuilder.ToString())))
            using (var lEncoded = new MemoryStream())
            {
                var lIncrement = new _TestActionInt();
                var lConfig = new cConvertToQuotedPrintableConfiguration(CancellationToken.None, lIncrement.ActionInt, null, new cBatchSizerConfiguration(1, 1, 1, 1));

                lBytesWritten = lClient.ConvertToQuotedPrintable(lInput, pSourceType, pQuotingRule, lEncoded, lConfig);

                // for debugging
                string lEncodedString = new string(Encoding.UTF8.GetChars(lEncoded.GetBuffer(), 0, (int)lEncoded.Length));

                // check the length outputs
                if (lBytesWritten != lEncoded.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.l.1");
                if (lIncrement.Total != lInput.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.l.2");

                // round trip test

                lEncoded.Position = 0;

                var lMC = new cMethodControl(-1, CancellationToken.None);
                var lWriteSizer = new cBatchSizer(new cBatchSizerConfiguration(1, 10, 1000, 1));

                using (var lDecoded = new MemoryStream())
                {
                    cDecoder lDecoder = new cQuotedPrintableDecoder(lMC, lDecoded, lWriteSizer);

                    var lReadBuffer = new byte[10];

                    while (true)
                    {
                        int lBytesRead = lEncoded.Read(lReadBuffer, 0, lReadBuffer.Length);
                        if (lBytesRead == 0) break;
                        var lWriteBuffer = new byte[lBytesRead];
                        Array.Copy(lReadBuffer, lWriteBuffer, lBytesRead);
                        lDecoder.WriteAsync(lWriteBuffer, 0, pParentContext).Wait();
                    }

                    lDecoder.FlushAsync(pParentContext).Wait();

                    int lLineNumber = 0;

                    using (var lReader = new StreamReader(lDecoded))
                    {
                        while (!lReader.EndOfStream)
                        {
                            var lLine = lReader.ReadLine();
                            if (lLine != pLines[lLineNumber++]) throw new cTestsException($"TestQuotedPrintable.{pTestName}.r");
                        }
                    }
                }

                // check lines are no longer than 76 chars
                //  every line in a binary file should end with an =
                //  the number of lines not ending with = should be the same as the number of input lines

                lEncoded.Position = 0;

                int lLinesNotEndingWithEquals = 0;

                using (var lReader = new StreamReader(lEncoded))
                {
                    while (!lReader.EndOfStream)
                    {
                        var lLine = lReader.ReadLine();
                        if (lLine.Length > 76) throw new cTestsException($"TestQuotedPrintable.{pTestName}.l.4");
                        if (lLine.Length == 0 || lLine[lLine.Length - 1] != '=') lLinesNotEndingWithEquals++;
                    }
                }

                if (pSourceType == eQuotedPrintableSourceType.Binary)
                {
                    if (lLinesNotEndingWithEquals != 0) throw new cTestsException($"TestQuotedPrintable.{pTestName}.i.1");
                }
                else if (pTerminateLastLine)
                {
                    if (lLinesNotEndingWithEquals != pLines.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.i.2");
                }
                else
                {
                    if (lLinesNotEndingWithEquals != pLines.Length - 1) throw new cTestsException($"TestQuotedPrintable.{pTestName}.i.3");
                }
            }

            return lBytesWritten;
        }
    }
}