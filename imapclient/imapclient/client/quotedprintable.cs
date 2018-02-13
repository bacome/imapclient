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
                    var lReadSizer = new cBatchSizer(mConvertToQuotedPrintableReadConfiguration);
                    var lWriteSizer = new cBatchSizer(mConvertToQuotedPrintableWriteConfiguration);
                    return await ZZConvertToQuotedPrintableAsync(lMC, pSource, pSourceType, pQuotingRule, pTarget, null, lReadSizer, lWriteSizer, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                var lReadSizer = new cBatchSizer(pConfiguration.ReadConfiguration ?? mConvertToQuotedPrintableReadConfiguration);
                var lWriteSizer = new cBatchSizer(pConfiguration.WriteConfiguration ?? mConvertToQuotedPrintableWriteConfiguration);
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
            private static readonly cBytes kCRLF = new cBytes("\r\n");
            private static readonly cBytes kEQUALSCRLF = new cBytes("=\r\n");
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
            private int mWriteBufferSize = 0;
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

                if (mPendingBytes.Count + mPendingWSP.Count + mPendingNonWSP.Count + lCount > 76) await ZSoftLineBreakAsync(false).ConfigureAwait(false);

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

                    if (lWSPThatWillFit < mPendingWSP.Count)
                    {
                        await ZSoftLineBreakAsync(true).ConfigureAwait(false);
                        if (mPendingWSP.Count == 0) throw new cInternalErrorException();
                    }

                    for (int i = 0; i < mPendingWSP.Count - 1; i++) mPendingBytes.Add(mPendingWSP[i]);
                    mPendingBytes.Add(cASCII.EQUALS);
                    mPendingBytes.AddRange(cTools.ByteToHexBytes(mPendingWSP[mPendingWSP.Count - 1]));
                    mPendingBytesInputByteCount += mPendingWSP.Count;

                    mPendingWSP.Clear();
                }

                mPendingBytesInputByteCount += pInputBytes;

                if (pInputBytes != 0) mPendingBytes.AddRange(kCRLF);

                await ZWriteLineAsync().ConfigureAwait(false);
                mPendingBytes.Clear();
                mPendingBytesInputByteCount = 0;
            }

            public async Task FlushAsync()
            {
                if (mPendingBytes.Count != 0 || mPendingWSP.Count != 0) await AddHardLineBreakAsync(0).ConfigureAwait(false);
                if (mBytesInWriteBuffer != 0) await ZWriteBufferAsync().ConfigureAwait(false);
            }

            public int BytesWritten => mBytesWritten;

            private async Task ZSoftLineBreakAsync(bool pInHardLineBreak)
            {
                // pInHardLineBreak protects against a special case - the end of file where the file doesn't finish with a line terminator
                //  in that case I have to avoid leaving the output finishing with a soft line break
                //   consider the input line          "12345678901234567890123456789012345678901234567890123456789012345678901234 "
                //     which might go to              "12345678901234567890123456789012345678901234567890123456789012345678901234=20" , but this is too long [over the 76 char line length limit]
                //     BUT in soft line break form is "12345678901234567890123456789012345678901234567890123456789012345678901234 ="  , which is noramlly fine, but as the last line in a quoted-printable encoding it is not fine;
                //      it is explicitly disallowed in rfc 2045 page 21 section (3)

                if (mPendingBytes.Count + mPendingWSP.Count + 1 > 76 || pInHardLineBreak)
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
                    mWriteBufferSize = Math.Max(mWriteSizer.Current, 78);
                    if (mWriteBuffer == null || mWriteBufferSize > mWriteBuffer.Length) mWriteBuffer = new byte[mWriteBufferSize];
                }
                else if (mBytesInWriteBuffer + mPendingBytes.Count > mWriteBufferSize)
                {
                    await ZWriteBufferAsync().ConfigureAwait(false);
                    mBytesInWriteBuffer = 0;
                    mWriteBufferInputByteCount = 0;
                }

                foreach (var lByte in mPendingBytes) mWriteBuffer[mBytesInWriteBuffer++] = lByte;

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



        private static partial class cTests
        {
            public static void TestQuotedPrintable(cTrace.cContext pParentContext)
            {
                ZTestQuotedPrintable(
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

                ZTestQuotedPrintable(
                    "2",
                    new string[]
                    {
                        //        1         2         3         4         5          6         7
                        // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \r@bcdefghijklmnopqrstu",
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \n@bcdefghijklmnopqrstuv",
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\r",
                    }, 
                    eQuotedPrintableSourceType.CRLFTerminatedLines,
                    false,
                    eQuotedPrintableQuotingRule.Minimal,
                    pParentContext);

                ZTestQuotedPrintable(
                    "3",
                    new string[]
                    {
                        //        1         2         3         4         5          6         7
                        // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                        " \t\t   \t\t\t\t     \t\t\t\t\t\t       \t\t\t\t\t\t\t\t         \t\t\t\t\t\t\t\t\t\t           \t\t\t\t\t\t\t\t\t\t\t\t             \t\t\t\t\t\t\t\t\t\t\t\t\t\t               ",
                        "",
                        " \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t",
                        ""
                    },
                    true,
                    pParentContext);

                ZTestQuotedPrintable(
                    "4",
                    new string[]
                    {
                        "@",
                        "12345678901234567890123456789012345678901234567890123456789012345678901234 "
                    },
                    pParentContext);

                ZTestQuotedPrintable(
                    "5",
                    new string[]
                    {
                        "@",
                        "1234567890123456789012345678901234567890123456789012345678901234567890123  "
                    },
                    pParentContext);

                ZTestQuotedPrintable(
                    "6",
                    new string[]
                    {
                        "@",
                        "123456789012345678901234567890123456789012345678901234567890123456789012   "
                    },
                    pParentContext);





                ZTestQuotedPrintableRandomLines(pParentContext);


                int lExpectedLength = 
                    ZTestQuotedPrintable(
                        "poem",
                        new string[]
                        {
                            "All doggies go to heaven (or so I've been told).",
                            "They run and play along the streets of Gold.",
                            "Why is heaven such a doggie-delight?",
                            "Why, because there's not a single cat in sight!"
                        },
                        eQuotedPrintableSourceType.CRLFTerminatedLines,
                        false,
                        eQuotedPrintableQuotingRule.EBCDIC, pParentContext);

                using (var lClient = new cIMAPClient())
                using (var lInput = new MemoryStream(Encoding.UTF8.GetBytes("All doggies go to heaven (or so I've been told).\r\nThey run and play along the streets of Gold.\r\nWhy is heaven such a doggie-delight?\r\nWhy, because there's not a single cat in sight!")))
                {
                    int lLength = lClient.ConvertToQuotedPrintable(lInput);
                    if (lLength != lExpectedLength) throw new cTestsException($"dev/null: {lLength} vs {lExpectedLength}");
                }
            }

            private static void ZTestQuotedPrintableRandomLines(cTrace.cContext pParentContext)
            {
                Random lRandom = new Random();

                for (int s = 0; s < 10; s++)
                {
                    int lLineCount = lRandom.Next(1000);

                    string[] lLines = new string[lLineCount];

                    for (int i = 0; i < lLineCount; i++)
                    {
                        int lLength = lRandom.Next(160);

                        StringBuilder lBuilder = new StringBuilder();

                        while (lBuilder.Length < lLength)
                        {
                            char lChar = (char)lRandom.Next(0xFFFF);
                            System.Globalization.UnicodeCategory lCat = char.GetUnicodeCategory(lChar);
                            if (lCat == System.Globalization.UnicodeCategory.ClosePunctuation ||
                                lCat == System.Globalization.UnicodeCategory.ConnectorPunctuation ||
                                lCat == System.Globalization.UnicodeCategory.CurrencySymbol ||
                                lCat == System.Globalization.UnicodeCategory.DashPunctuation ||
                                lCat == System.Globalization.UnicodeCategory.DecimalDigitNumber ||
                                lCat == System.Globalization.UnicodeCategory.FinalQuotePunctuation ||
                                lCat == System.Globalization.UnicodeCategory.InitialQuotePunctuation ||
                                lCat == System.Globalization.UnicodeCategory.LowercaseLetter ||
                                lCat == System.Globalization.UnicodeCategory.MathSymbol ||
                                lCat == System.Globalization.UnicodeCategory.OpenPunctuation ||
                                lCat == System.Globalization.UnicodeCategory.OtherLetter ||
                                lCat == System.Globalization.UnicodeCategory.OtherNumber ||
                                lCat == System.Globalization.UnicodeCategory.OtherPunctuation ||
                                lCat == System.Globalization.UnicodeCategory.OtherSymbol ||
                                lCat == System.Globalization.UnicodeCategory.SpaceSeparator ||
                                lCat == System.Globalization.UnicodeCategory.TitlecaseLetter ||
                                lCat == System.Globalization.UnicodeCategory.UppercaseLetter
                                ) lBuilder.Append(lChar);
                        }

                        lLines[i] = lBuilder.ToString();
                    }

                    ZTestQuotedPrintable(
                        "ZTestQuotedPrintableRandomLines",
                        lLines,
                        pParentContext);
                }
            }

            private static void ZTestQuotedPrintable(string pTestName, string[] pLines, cTrace.cContext pParentContext)
            {
                var lLFFalseMinimal = ZTestQuotedPrintable(pTestName + ".lf.false.Mininal", pLines, eQuotedPrintableSourceType.LFTerminatedLines, false, eQuotedPrintableQuotingRule.Minimal, pParentContext);
                var lLFTrueMinimal = ZTestQuotedPrintable(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableSourceType.LFTerminatedLines, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);

                if (lLFFalseMinimal >= lLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");

                var lCRLFFalseMinimal = ZTestQuotedPrintable(pTestName + ".crlf.false.Mininal", pLines, eQuotedPrintableSourceType.CRLFTerminatedLines, false, eQuotedPrintableQuotingRule.Minimal, pParentContext);

                if (lLFFalseMinimal != lCRLFFalseMinimal) throw new cTestsException(pTestName + ".compare.2");

                var lCRLFTrueMinimal = ZTestQuotedPrintable(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableSourceType.CRLFTerminatedLines, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);

                if (lCRLFFalseMinimal >= lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.3");

                var lBinaryFalseMinimal = ZTestQuotedPrintable(pTestName + ".binary.false.Mininal", pLines, eQuotedPrintableSourceType.Binary, false, eQuotedPrintableQuotingRule.Minimal, pParentContext);

                if (lCRLFFalseMinimal >= lBinaryFalseMinimal) throw new cTestsException(pTestName + ".compare.4");

                var lBinaryTrueMinimal = ZTestQuotedPrintable(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableSourceType.Binary, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);

                if (lBinaryFalseMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.5");

                var lLFFalseEBCDIC = ZTestQuotedPrintable(pTestName + ".lf.false.EBCDIC", pLines, eQuotedPrintableSourceType.LFTerminatedLines, false, eQuotedPrintableQuotingRule.EBCDIC, pParentContext);

                if (lLFFalseMinimal >= lLFFalseEBCDIC) throw new cTestsException(pTestName + ".compare.6");
            }

            private static void ZTestQuotedPrintable(string pTestName, string[] pLines, bool pNoNo, cTrace.cContext pParentContext)
            {
                var lLFTrueMinimal = ZTestQuotedPrintable(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableSourceType.LFTerminatedLines, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);
                var lCRLFTrueMinimal = ZTestQuotedPrintable(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableSourceType.CRLFTerminatedLines, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);
                if (lLFTrueMinimal != lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");
                var lBinaryTrueMinimal = ZTestQuotedPrintable(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableSourceType.Binary, true, eQuotedPrintableQuotingRule.Minimal, pParentContext);
                if (lCRLFTrueMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.2");
                var lLFTrueEBCDIC = ZTestQuotedPrintable(pTestName + ".lf.true.EBCDIC", pLines, eQuotedPrintableSourceType.LFTerminatedLines, true, eQuotedPrintableQuotingRule.EBCDIC, pParentContext);
            }

            private static int ZTestQuotedPrintable(string pTestName, string[] pLines, eQuotedPrintableSourceType pSourceType, bool pTerminateLastLine, eQuotedPrintableQuotingRule pQuotingRule, cTrace.cContext pParentContext)
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

                    string lEncodedString = new string(Encoding.UTF8.GetChars(lEncoded.GetBuffer(), 0, (int)lEncoded.Length));
                    if (lBytesWritten > 0 && lEncodedString[lEncodedString.Length - 1] == '=') throw new cTestsException($"TestQuotedPrintable.{pTestName}.e.1");
                    if (lBytesWritten > 1 && lEncodedString[lEncodedString.Length - 2] == '=') throw new cTestsException($"TestQuotedPrintable.{pTestName}.e.2");

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

                        var lTemp1 = new string(Encoding.UTF8.GetChars(lDecoded.GetBuffer(), 0, (int)lDecoded.Length));

                        var lLines = new List<string>();

                        int lStartIndex = 0;

                        while (lStartIndex < lTemp1.Length)
                        {
                            int lEOL = lTemp1.IndexOf("\r\n", lStartIndex, StringComparison.Ordinal);
                            if (lEOL == -1) lEOL = lTemp1.Length;
                            lLines.Add(lTemp1.Substring(lStartIndex, lEOL - lStartIndex));
                            lStartIndex = lEOL + 2;
                        }

                        bool lDump = false;
                        if (lLines.Count != pLines.Length) lDump = true;
                        for (int i = 0; i < lLines.Count; i++) if (lLines[i] != pLines[i]) lDump = true;

                        if (lDump)
                        {
                            // note: this is the error that lead to the inclusion of ordinal string searches ... the occasional \r\n was missed without it

                            pParentContext.TraceError("TestQuotedPrintable {0}: {1} roundtrip vs {2} input", pTestName, lLines.Count, pLines.Length);

                            for (int i = 0; i < lLines.Count; i++)
                            {
                                if (lLines[i] != pLines[i])
                                {
                                    for (int j = i; j < lLines.Count && j < i + 3; j++)
                                    {
                                        pParentContext.TraceError(pLines[j]);
                                        pParentContext.TraceError(lLines[j]);
                                    }

                                    break;
                                }

                                pParentContext.TraceInformation(pLines[i]);
                            }

                            throw new cTestsException($"TestQuotedPrintable.{pTestName}.r.1");
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
                        if (lLinesNotEndingWithEquals != 1) throw new cTestsException($"TestQuotedPrintable.{pTestName}.i.1");
                    }
                    else 
                    {
                        if (lLinesNotEndingWithEquals != pLines.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.i.2");
                    }
                }

                return lBytesWritten;
            }
        }
    }
}