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
                                lOutput.terminateline(2);
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
                            lOutput.terminateline(1);
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

            private readonly eQuotedPrintableQuotingRule mQuotingRule;
            private readonly Stream mTarget;
            private readonly Action<int> mIncrement;
            private readonly cBatchSizer mWriteSizer;

            private readonly List<cLine> mPendingLines = new List<cLine>();

            private List<byte> mCurrentLine = new List<byte>();
            private int mCurrentLineInputBytes = 0;
            private readonly List<byte> mPendingWSP = new List<byte>();

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

                if (mCurrentLine.Count + mPendingWSP.Count + lCount > 76) ZSoftLineBreak();

                if (pByte == cASCII.TAB || pByte == cASCII.SPACE) mPendingWSP.Add(pByte);
                else
                {
                    mCurrentLineInputBytes += mPendingWSP.Count + 1;

                    mCurrentLine.AddRange(mPendingWSP);
                    mPendingWSP.Clear();

                    if (lNeedsQuoting)
                    {
                        mCurrentLine.Add(cASCII.EQUALS);
                        mCurrentLine.AddRange(cTools.ByteToHexBytes(pByte));
                    }
                    else mCurrentLine.Add(pByte);
                }
            }


            // private 

            private class cLine
            {
                private int mInputByteCount = 0;
                private readonly List<byte> mBytes = new List<byte>();

                public cLine() { }


            }
        }
    }
}