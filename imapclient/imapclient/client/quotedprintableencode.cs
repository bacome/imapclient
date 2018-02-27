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
    public enum eQuotedPrintableEncodeSourceType { Binary, CRLFTerminatedLines, LFTerminatedLines }
    public enum eQuotedPrintableEncodeQuotingRule { Minimal, EBCDIC }

    public partial class cIMAPClient
    {
        private static readonly eQuotedPrintableEncodeSourceType kQuotedPrintableEncodeDefaultSourceType = Environment.NewLine == "\n" ? eQuotedPrintableEncodeSourceType.LFTerminatedLines : eQuotedPrintableEncodeSourceType.CRLFTerminatedLines;

        public long QuotedPrintableEncode(Stream pSource, Stream pTarget = null, cQuotedPrintableEncodeConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(QuotedPrintableEncode), 1);
            var lTask = ZQuotedPrintableEncodeAsync(pSource, kQuotedPrintableEncodeDefaultSourceType, eQuotedPrintableEncodeQuotingRule.EBCDIC, pTarget, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<long> QuotedPrintableEncodeAsync(Stream pSource, Stream pTarget, cQuotedPrintableEncodeConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(QuotedPrintableEncodeAsync), 1);
            return ZQuotedPrintableEncodeAsync(pSource, kQuotedPrintableEncodeDefaultSourceType, eQuotedPrintableEncodeQuotingRule.EBCDIC, pTarget, pConfiguration, lContext);
        }

        public long QuotedPrintableEncode(Stream pSource, eQuotedPrintableEncodeSourceType pSourceType, eQuotedPrintableEncodeQuotingRule pQuotingRule, Stream pTarget = null, cQuotedPrintableEncodeConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(QuotedPrintableEncode), 2);
            var lTask = ZQuotedPrintableEncodeAsync(pSource, pSourceType, pQuotingRule, pTarget, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        public Task<long> QuotedPrintableEncodeAsync(Stream pSource, eQuotedPrintableEncodeSourceType pSourceType, eQuotedPrintableEncodeQuotingRule pQuotingRule, Stream pTarget = null, cQuotedPrintableEncodeConfiguration pConfiguration = null)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(QuotedPrintableEncodeAsync), 2);
            return ZQuotedPrintableEncodeAsync(pSource, pSourceType, pQuotingRule, pTarget, pConfiguration, lContext);
        }

        private async Task<long> ZQuotedPrintableEncodeAsync(Stream pSource, eQuotedPrintableEncodeSourceType pSourceType, eQuotedPrintableEncodeQuotingRule pQuotingRule, Stream pTarget, cQuotedPrintableEncodeConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZQuotedPrintableEncodeAsync), pSourceType, pQuotingRule, pConfiguration);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pSource == null) throw new ArgumentNullException(nameof(pSource));
            if (!pSource.CanRead) throw new ArgumentOutOfRangeException(nameof(pSource));
            if (pTarget != null && !pTarget.CanWrite) throw new ArgumentNullException(nameof(pTarget));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    return await ZZQuotedPrintableEncodeAsync(lMC, pSource, pSourceType, pQuotingRule, pTarget, null, mQuotedPrintableEncodeReadWriteConfiguration, mQuotedPrintableEncodeReadWriteConfiguration, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await ZZQuotedPrintableEncodeAsync(lMC, pSource, pSourceType, pQuotingRule, pTarget, pConfiguration.Increment, pConfiguration.ReadConfiguration ?? mQuotedPrintableEncodeReadWriteConfiguration, pConfiguration.WriteConfiguration ?? mQuotedPrintableEncodeReadWriteConfiguration, lContext).ConfigureAwait(false);
            }
        }

        private async Task<long> ZZQuotedPrintableEncodeAsync(cMethodControl pMC, Stream pSource, eQuotedPrintableEncodeSourceType pSourceType, eQuotedPrintableEncodeQuotingRule pQuotingRule, Stream pTarget, Action<int> pIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZQuotedPrintableEncodeAsync), pMC, pSourceType, pQuotingRule, pReadConfiguration, pWriteConfiguration);

            byte[] lReadBuffer = null;
            Stopwatch lStopwatch = new Stopwatch();

            bool lPendingCR = false;
            cQuotedPrintableTarget lTarget = new cQuotedPrintableTarget(pMC, pQuotingRule, pTarget, mSynchroniser, pIncrement, lContext, pWriteConfiguration);

            cBatchSizer lReadSizer = new cBatchSizer(pReadConfiguration);

            while (true)
            {
                // read some data

                int lCurrent = lReadSizer.Current;
                if (lReadBuffer == null || lCurrent > lReadBuffer.Length) lReadBuffer = new byte[lCurrent];

                lStopwatch.Restart();

                if (pSource.CanTimeout) pSource.ReadTimeout = pMC.Timeout;
                else _ = pMC.Timeout; // check for timeout

                int lBytesReadIntoBuffer = await pSource.ReadAsync(lReadBuffer, 0, lReadBuffer.Length, pMC.CancellationToken).ConfigureAwait(false);

                lStopwatch.Stop();

                if (lBytesReadIntoBuffer == 0) break;

                lReadSizer.AddSample(lBytesReadIntoBuffer, lStopwatch.ElapsedMilliseconds);

                // process the data

                int lReadBufferPosition = 0;

                while (lReadBufferPosition < lBytesReadIntoBuffer)
                {
                    var lByte = lReadBuffer[lReadBufferPosition++];

                    if (pSourceType == eQuotedPrintableEncodeSourceType.CRLFTerminatedLines)
                    {
                        if (lPendingCR)
                        {
                            lPendingCR = false;

                            if (lByte == cASCII.LF)
                            {
                                await lTarget.AddHardLineBreakAsync(2).ConfigureAwait(false);
                                continue;
                            }

                            await lTarget.AddAsync(cASCII.CR).ConfigureAwait(false);
                        }

                        if (lByte == cASCII.CR)
                        {
                            lPendingCR = true;
                            continue;
                        }
                    }
                    else if (pSourceType == eQuotedPrintableEncodeSourceType.LFTerminatedLines)
                    {
                        if (lByte == cASCII.LF)
                        {
                            await lTarget.AddHardLineBreakAsync(1).ConfigureAwait(false);
                            continue;
                        }
                    }

                    await lTarget.AddAsync(lByte).ConfigureAwait(false);
                }
            }

            // flush any cached output data
            if (lPendingCR) await lTarget.AddAsync(cASCII.CR).ConfigureAwait(false);
            await lTarget.FlushAsync().ConfigureAwait(false);

            // done
            return lTarget.BytesWritten;
        }

        private class cQuotedPrintableTarget
        {
            private static readonly cBytes kCRLF = new cBytes("\r\n");
            private static readonly cBytes kEQUALSCRLF = new cBytes("=\r\n");
            private static readonly cBytes kEBCDICNotSome = new cBytes("!\"#$@[\\]^`{|}~");

            private readonly cMethodControl mMC;
            private readonly eQuotedPrintableEncodeQuotingRule mQuotingRule;
            private readonly Stream mTarget;
            private readonly cCallbackSynchroniser mSynchroniser;
            private readonly Action<int> mIncrement;
            private readonly cTrace.cContext mContextForIncrement;
            private readonly cBatchSizer mWriteSizer;

            private List<byte> mPendingBytes = new List<byte>();
            private int mPendingBytesInputByteCount = 0;
            private readonly List<byte> mPendingWSP = new List<byte>();
            private readonly List<byte> mPendingNonWSP = new List<byte>();

            private byte[] mWriteBuffer = null;
            private int mWriteBufferSize = 0;
            private int mBytesInWriteBuffer = 0;
            private readonly Stopwatch mStopwatch = new Stopwatch();
            private int mWriteBufferInputByteCount = 0;

            private long mBytesWritten = 0;

            public cQuotedPrintableTarget(cMethodControl pMC, eQuotedPrintableEncodeQuotingRule pQuotingRule, Stream pTarget, cCallbackSynchroniser pSynchroniser, Action<int> pIncrement, cTrace.cContext pContextForIncrement, cBatchSizerConfiguration pConfiguration)
            {
                mMC = pMC;
                mQuotingRule = pQuotingRule;

                if (pTarget == null) mTarget = Stream.Null;
                else mTarget = pTarget;

                mSynchroniser = pSynchroniser;
                mIncrement = pIncrement;
                mContextForIncrement = pContextForIncrement;

                mWriteSizer = new cBatchSizer(pConfiguration);
            }

            public async Task AddAsync(byte pByte)
            {
                bool lNeedsQuoting;

                if (pByte == cASCII.TAB) lNeedsQuoting = false;
                else if (pByte < cASCII.SPACE) lNeedsQuoting = true;
                else if (pByte == cASCII.EQUALS) lNeedsQuoting = true;
                else if (pByte < cASCII.DEL)
                {
                    if (mQuotingRule == eQuotedPrintableEncodeQuotingRule.EBCDIC && kEBCDICNotSome.Contains(pByte)) lNeedsQuoting = true;
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

            public long BytesWritten => mBytesWritten;

            private async Task ZSoftLineBreakAsync(bool pInHardLineBreak)
            {
                // pInHardLineBreak protects against a special case - the end of file where the file doesn't finish with a line terminator
                //  in that case I have to avoid leaving the output finishing with a soft line break
                //   consider the input line          "12345678901234567890123456789012345678901234567890123456789012345678901234 "
                //     which might go to              "12345678901234567890123456789012345678901234567890123456789012345678901234=20" , but this is too long [over the 76 char line length limit]
                //     BUT in soft line break form is "12345678901234567890123456789012345678901234567890123456789012345678901234 ="  , which is normally fine, but,
                //      as the last line in a quoted-printable encoding it is not fine - it is explicitly disallowed in rfc 2045 page 21 section (3)

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
                // write some data

                mStopwatch.Restart();

                if (mTarget.CanTimeout) mTarget.WriteTimeout = mMC.Timeout;
                else _ = mMC.Timeout; // check for timeout

                await mTarget.WriteAsync(mWriteBuffer, 0, mBytesInWriteBuffer, mMC.CancellationToken).ConfigureAwait(false);

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
            public static void QuotedPrintableEncodeTests(cTrace.cContext pParentContext)
            {


                ZQuotedPrintableEncodeTest(
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

                ZQuotedPrintableEncodeTest(
                    "2",
                    new string[]
                    {
                    //        1         2         3         4         5          6         7
                    // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \r@bcdefghijklmnopqrstu",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \n@bcdefghijklmnopqrstuv",
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ \t@bcdefghijklmnopqrstuv\r",
                    },
                    eQuotedPrintableEncodeSourceType.CRLFTerminatedLines,
                    false,
                    eQuotedPrintableEncodeQuotingRule.Minimal,
                    pParentContext);

                ZQuotedPrintableEncodeTest(
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

                ZQuotedPrintableEncodeTest(
                    "3.1",
                    new string[]
                    {
                    //        1         2         3         4         5          6         7
                    // 345678901234567890123456789012345678901234567890123 4567890123456789012345
                    "",
                    " \t\t   \t\t\t\t     \t\t\t\t\t\t       \t\t\t\t\t\t\t\t         \t\t\t\t\t\t\t\t\t\t           \t\t\t\t\t\t\t\t\t\t\t\t             \t\t\t\t\t\t\t\t\t\t\t\t\t\t               ",
                    " \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t \t  \t\t",
                    ""
                    },
                    true,
                    pParentContext);

                ZQuotedPrintableEncodeTest(
                    "4",
                    new string[]
                    {
                    "@",
                    "12345678901234567890123456789012345678901234567890123456789012345678901234 "
                    },
                    pParentContext);

                ZQuotedPrintableEncodeTest(
                    "5",
                    new string[]
                    {
                    "@",
                    "1234567890123456789012345678901234567890123456789012345678901234567890123  "
                    },
                    pParentContext);

                ZQuotedPrintableEncodeTest(
                    "6",
                    new string[]
                    {
                    "@",
                    "123456789012345678901234567890123456789012345678901234567890123456789012   "
                    },
                    pParentContext);





                ZQuotedPrintableEncodeTestRandomLines(pParentContext);


                long lExpectedLength =
                    ZQuotedPrintableEncodeTest(
                        "poem",
                        new string[]
                        {
                                    "All doggies go to heaven (or so I've been told).",
                                    "They run and play along the streets of Gold.",
                                    "Why is heaven such a doggie-delight?",
                                    "Why, because there's not a single cat in sight!"
                        },
                        eQuotedPrintableEncodeSourceType.CRLFTerminatedLines,
                        false,
                        eQuotedPrintableEncodeQuotingRule.EBCDIC, pParentContext);

                using (var lInput = new MemoryStream(Encoding.UTF8.GetBytes("All doggies go to heaven (or so I've been told).\r\nThey run and play along the streets of Gold.\r\nWhy is heaven such a doggie-delight?\r\nWhy, because there's not a single cat in sight!")))
                {
                    long lLength = mClient.QuotedPrintableEncode(lInput);
                    if (lLength != lExpectedLength) throw new cTestsException($"dev/null: {lLength} vs {lExpectedLength}");
                }
            }

            private static void ZQuotedPrintableEncodeTestRandomLines(cTrace.cContext pParentContext)
            {
                Random lRandom = new Random();

                for (int s = 0; s < 10; s++)
                {
                    int lLineCount = lRandom.Next(100) + 1;

                    string[] lLines = new string[lLineCount];

                    for (int i = 0; i < lLineCount; i++)
                    {
                        int lLength = lRandom.Next(160);

                        StringBuilder lBuilder = new StringBuilder();
                        if (i == 0) lBuilder.Append('@');

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

                    ZQuotedPrintableEncodeTest(
                        "ZTestQuotedPrintableRandomLines",
                        lLines,
                        pParentContext);
                }
            }

            private static void ZQuotedPrintableEncodeTest(string pTestName, string[] pLines, cTrace.cContext pParentContext)
            {
                var lLFFalseMinimal = ZQuotedPrintableEncodeTest(pTestName + ".lf.false.Mininal", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, false, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);
                var lLFTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

                if (lLFFalseMinimal >= lLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");

                var lCRLFFalseMinimal = ZQuotedPrintableEncodeTest(pTestName + ".crlf.false.Mininal", pLines, eQuotedPrintableEncodeSourceType.CRLFTerminatedLines, false, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

                if (lLFFalseMinimal != lCRLFFalseMinimal) throw new cTestsException(pTestName + ".compare.2");

                var lCRLFTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.CRLFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

                if (lCRLFFalseMinimal >= lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.3");

                var lBinaryFalseMinimal = ZQuotedPrintableEncodeTest(pTestName + ".binary.false.Mininal", pLines, eQuotedPrintableEncodeSourceType.Binary, false, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

                if (lCRLFFalseMinimal >= lBinaryFalseMinimal) throw new cTestsException(pTestName + ".compare.4");

                var lBinaryTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.Binary, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);

                if (lBinaryFalseMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.5");

                var lLFFalseEBCDIC = ZQuotedPrintableEncodeTest(pTestName + ".lf.false.EBCDIC", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, false, eQuotedPrintableEncodeQuotingRule.EBCDIC, pParentContext);

                if (lLFFalseMinimal >= lLFFalseEBCDIC) throw new cTestsException(pTestName + ".compare.6");
            }

            private static void ZQuotedPrintableEncodeTest(string pTestName, string[] pLines, bool pNoNo, cTrace.cContext pParentContext)
            {
                var lLFTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".lf.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);
                var lCRLFTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".crlf.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.CRLFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);
                if (lLFTrueMinimal != lCRLFTrueMinimal) throw new cTestsException(pTestName + ".compare.1");
                var lBinaryTrueMinimal = ZQuotedPrintableEncodeTest(pTestName + ".binary.true.Mininal", pLines, eQuotedPrintableEncodeSourceType.Binary, true, eQuotedPrintableEncodeQuotingRule.Minimal, pParentContext);
                if (lCRLFTrueMinimal >= lBinaryTrueMinimal) throw new cTestsException(pTestName + ".compare.2");
                var lLFTrueEBCDIC = ZQuotedPrintableEncodeTest(pTestName + ".lf.true.EBCDIC", pLines, eQuotedPrintableEncodeSourceType.LFTerminatedLines, true, eQuotedPrintableEncodeQuotingRule.EBCDIC, pParentContext);
            }

            private static long ZQuotedPrintableEncodeTest(string pTestName, string[] pLines, eQuotedPrintableEncodeSourceType pSourceType, bool pTerminateLastLine, eQuotedPrintableEncodeQuotingRule pQuotingRule, cTrace.cContext pParentContext)
            {
                long lBytesWritten;

                StringBuilder lBuilder = new StringBuilder();

                for (int i = 0; i < pLines.Length; i++)
                {
                    lBuilder.Append(pLines[i]);

                    if (i < pLines.Length - 1 || pTerminateLastLine)
                    {
                        if (pSourceType == eQuotedPrintableEncodeSourceType.LFTerminatedLines) lBuilder.Append('\n');
                        else lBuilder.Append("\r\n");
                    }
                }

                using (var lClient = new cIMAPClient())
                using (var lInput = new MemoryStream(Encoding.UTF8.GetBytes(lBuilder.ToString())))
                using (var lEncoded = new MemoryStream())
                {
                    var lIncrement = new cTestActionInt();
                    cQuotedPrintableEncodeConfiguration lConfig = new cQuotedPrintableEncodeConfiguration(CancellationToken.None, lIncrement.ActionInt);

                    lBytesWritten = mClient.QuotedPrintableEncode(lInput, pSourceType, pQuotingRule, lEncoded, lConfig);

                    string lEncodedString = new string(Encoding.UTF8.GetChars(lEncoded.GetBuffer(), 0, (int)lEncoded.Length));
                    if (lBytesWritten > 0 && lEncodedString[lEncodedString.Length - 1] == '=') throw new cTestsException($"TestQuotedPrintable.{pTestName}.e.1");
                    if (lBytesWritten > 1 && lEncodedString[lEncodedString.Length - 2] == '=') throw new cTestsException($"TestQuotedPrintable.{pTestName}.e.2");

                    // check the length outputs
                    if (lBytesWritten != lEncoded.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.l.1");
                    if (lIncrement.Total != lInput.Length) throw new cTestsException($"TestQuotedPrintable.{pTestName}.l.2");

                    // round trip test

                    lEncoded.Position = 0;

                    var lMC = new cMethodControl(-1, CancellationToken.None);
                    var lWriteSizer = new cBatchSizer(new cBatchSizerConfiguration(1000, 100000, 10000, 1000));

                    using (var lDecoded = new MemoryStream())
                    {
                        cDecoder lDecoder = new cQuotedPrintableDecoder(lMC, lDecoded, lWriteSizer);

                        var lReadBuffer = new byte[10000];

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

                            for (int i = 0; i < Math.Max(pLines.Length, lLines.Count); i++)
                            {
                                if (i >= lLines.Count || i >= pLines.Length || lLines[i] != pLines[i])
                                {
                                    for (int j = i; j < Math.Max(pLines.Length, lLines.Count) && j < i + 3; j++)
                                    {
                                        if (j < pLines.Length) pParentContext.TraceError(pLines[j]);
                                        if (j < lLines.Count) pParentContext.TraceWarning(lLines[j]);
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

                    if (pSourceType == eQuotedPrintableEncodeSourceType.Binary)
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