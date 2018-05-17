using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    internal abstract class cDecoder
    {
        private readonly iFetchBodyTarget mTarget;
        private readonly byte[] mBuffer = new byte[cMailClient.LocalStreamBufferSize];

        public cDecoder(iFetchBodyTarget pTarget)
        {
            mTarget = pTarget ?? throw new ArgumentNullException(nameof(pTarget));
        }

        public Task WriteAsync(IList<byte> pFetchedBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDecoder), nameof(WriteAsync), pOffset);
            if (pFetchedBytes == null) throw new ArgumentNullException(nameof(pFetchedBytes));
            if (pOffset > pFetchedBytes.Count) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pOffset == pFetchedBytes.Count) return Task.WhenAll(); // TODO => Task.CompletedTask;
            return YWriteAsync(pFetchedBytes, pOffset, pCancellationToken, lContext);
        }

        protected abstract Task YWriteAsync(IList<byte> pFetchedBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext);

        protected async Task YWriteAsync(int pFetchedBytesProcessed, IList<byte> pDecodedBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDecoder), nameof(YWriteAsync), pOffset);

            if (pFetchedBytesProcessed < 0) throw new ArgumentOutOfRangeException(nameof(pFetchedBytesProcessed));
            if (pDecodedBytes == null) throw new ArgumentNullException(nameof(pDecodedBytes));
            if (pOffset < 0 || pOffset > pDecodedBytes.Count) throw new ArgumentOutOfRangeException(nameof(pOffset));
            if (pFetchedBytesProcessed < pDecodedBytes.Count - pOffset) throw new ArgumentOutOfRangeException(nameof(pDecodedBytes));
            if (pFetchedBytesProcessed == 0) return;

            int lExtraFetchedBytesProcessed = pFetchedBytesProcessed - pDecodedBytes.Count + pOffset;

            do
            {
                int lBytesInBuffer = 0;
                while (lBytesInBuffer < cMailClient.LocalStreamBufferSize && pOffset < pDecodedBytes.Count) mBuffer[lBytesInBuffer++] = pDecodedBytes[pOffset++];
                await mTarget.WriteAsync(mBuffer, lBytesInBuffer, lBytesInBuffer + lExtraFetchedBytesProcessed, pCancellationToken, lContext).ConfigureAwait(false);
                lExtraFetchedBytesProcessed = 0;
            } while (pOffset < pDecodedBytes.Count);
        }

        public virtual Task FlushAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext) => Task.WhenAll(); // TODO => Task.CompletedTask;

        public static cDecoder GetDecoder(bool pBinary, eDecodingRequired pDecoding, iFetchBodyTarget pTarget, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDecoder), nameof(GetDecoder), pBinary, pDecoding);
            if (pTarget == null) throw new ArgumentNullException(nameof(pTarget));
            if (pBinary || pDecoding == eDecodingRequired.none) return new cIdentityDecoder(pTarget);
            if (pDecoding == eDecodingRequired.base64) return new cBase64Decoder(pTarget);
            if (pDecoding == eDecodingRequired.quotedprintable) return new cQuotedPrintableDecoder(pTarget);
            throw new cContentTransferDecodingException("required decoding not supported", lContext);
        }

        public class _Tester : iFetchBodyTarget, IDisposable
        {
            private long mFetchedBytesWritten = 0;
            private MemoryStream mStream = new MemoryStream();
            public _Tester() { }

            public Task WriteAsync(byte[] pBuffer, int pCount, int pFetchedBytesInBuffer, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
            {
                mFetchedBytesWritten += pFetchedBytesInBuffer;
                return mStream.WriteAsync(pBuffer, 0, pCount, pCancellationToken);
            }

            public byte[] GetBuffer() => mStream.GetBuffer();
            public long FetchedBytesWritten => mFetchedBytesWritten;
            public int Length => (int)mStream.Length;
            public void Dispose() => mStream.Dispose();
        }
    }

    internal class cIdentityDecoder : cDecoder
    {
        public cIdentityDecoder(iFetchBodyTarget pTarget) : base(pTarget) { }
        protected override Task YWriteAsync(IList<byte> pFetchedBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext) => YWriteAsync(pFetchedBytes.Count - pOffset, pFetchedBytes, pOffset, pCancellationToken, pParentContext);
    }

    internal abstract class cLineDecoder : cDecoder
    {              
        private List<byte> mLineBytes = new List<byte>();
        private bool mBufferedCR = false;

        public cLineDecoder(iFetchBodyTarget pTarget) : base(pTarget) { }

        protected async sealed override Task YWriteAsync(IList<byte> pFetchedBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cLineDecoder), nameof(YWriteAsync), pOffset);

            while (pOffset < pFetchedBytes.Count)
            {
                byte lByte = pFetchedBytes[pOffset++];
                        
                if (mBufferedCR)
                {
                    mBufferedCR = false;

                    if (lByte == cASCII.LF)
                    {
                        await YWriteLineAsync(mLineBytes.Count + 2, mLineBytes, true, pCancellationToken, lContext).ConfigureAwait(false);
                        mLineBytes.Clear();
                        continue;
                    }

                    mLineBytes.Add(cASCII.CR);
                }

                if (lByte == cASCII.CR) mBufferedCR = true;
                else mLineBytes.Add(lByte);
            }
        }

        public sealed override Task FlushAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cLineDecoder), nameof(FlushAsync));
            if (mBufferedCR) mLineBytes.Add(cASCII.CR);
            return YWriteLineAsync(mLineBytes.Count, mLineBytes, false, pCancellationToken, lContext);
        }

        protected abstract Task YWriteLineAsync(int pFetchedBytesInLine, List<byte> pLineBytes, bool pCRLF, CancellationToken pCancellationToken, cTrace.cContext pParentContext);
    }

    internal class cBase64Decoder : cLineDecoder
    {
        private readonly List<byte> mEncodedBytes = new List<byte>();

        public cBase64Decoder(iFetchBodyTarget pTarget) : base(pTarget) { }

        protected override Task YWriteLineAsync(int pFetchedBytesInLine, List<byte> pLineBytes, bool pCRLF, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBase64Decoder), nameof(YWriteLineAsync), pCRLF);

            // build the buffer to decode
            mEncodedBytes.Clear();
            foreach (var lByte in pLineBytes) if (cBase64.IsInAlphabet(lByte)) mEncodedBytes.Add(lByte);

            // decode
            if (!cBase64.TryDecode(mEncodedBytes, out var lDecodedBytes, out var lError)) throw new cContentTransferDecodingException(lError, lContext);

            return YWriteAsync(pFetchedBytesInLine, lDecodedBytes, 0, pCancellationToken, lContext);
        }
    }

    internal class cQuotedPrintableDecoder : cLineDecoder
    {
        private static readonly cBytes kNewLine = new cBytes(Environment.NewLine);

        private readonly List<byte> mDecodedBytes = new List<byte>();

        public cQuotedPrintableDecoder(iFetchBodyTarget pTarget) : base(pTarget) { }

        protected override Task YWriteLineAsync(int pFetchedBytesInLine, List<byte> pLineBytes, bool pCRLF, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cQuotedPrintableDecoder), nameof(YWriteLineAsync), pCRLF);

            byte lByte;

            // strip trailing space

            int lEOL = pLineBytes.Count;

            while (lEOL != 0)
            {
                lByte = pLineBytes[lEOL - 1];
                if (lByte != cASCII.SPACE && lByte != cASCII.TAB) break;
                lEOL--;
            }

            // strip trailing =

            bool lSoftLineBreak;

            if (lEOL != 0 && pLineBytes[lEOL - 1] == cASCII.EQUALS)
            {
                lSoftLineBreak = true;
                lEOL--;
            }
            else lSoftLineBreak = false;

            // decode

            mDecodedBytes.Clear();

            int lInputByte = 0;

            while (lInputByte < lEOL)
            {
                lByte = pLineBytes[lInputByte++];

                if (lByte == cASCII.EQUALS)
                {
                    if (lInputByte + 2 <= lEOL && ZGetHexEncodedNibble(pLineBytes, lInputByte, out int lMSN) && ZGetHexEncodedNibble(pLineBytes, lInputByte + 1, out int lLSN))
                    {
                        lInputByte = lInputByte + 2;
                        lByte = (byte)(lMSN << 4 | lLSN);
                    }
                }

                mDecodedBytes.Add(lByte);
            }

            // potentially add a line break 
            if (pCRLF && !lSoftLineBreak) mDecodedBytes.AddRange(kNewLine);

            return YWriteAsync(pFetchedBytesInLine, mDecodedBytes, 0, pCancellationToken, lContext);
        }

        private bool ZGetHexEncodedNibble(List<byte> pLineBytes, int pByte, out int rNibble)
        {
            int lByte = pLineBytes[pByte];

            if (lByte < cASCII.ZERO) { rNibble = 0; return false; }
            if (lByte <= cASCII.NINE) { rNibble = lByte - cASCII.ZERO; return true; }

            if (lByte < cASCII.A) { rNibble = 0; return false; }
            if (lByte <= cASCII.F) { rNibble = 10 + lByte - cASCII.A; return true; }

            if (lByte < cASCII.a) { rNibble = 0; return false; }
            if (lByte <= cASCII.f) { rNibble = 10 + lByte - cASCII.a; return true; }

            rNibble = 0;
            return false;
        }

        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cQuotedPrintableDecoder), nameof(_Tests));

            using (var lCache = new cTempFileSectionCache("test_decoder", 60000, 1000, 1, 2))
            {
                if (LTest("testNow's the time =    \r\n", "for all folk to come=\t \t \r\n", " to the aid of their country.   \t\r\n") != "Now's the time for all folk to come to the aid of their country.\r\n") throw new cTestsException($"{nameof(cQuotedPrintableDecoder)}.1");
                if (LTest("testNow's the time =    \r\n", "for all folk to come=\t \t \r\n", " to the aid of their country.   \t") != "Now's the time for all folk to come to the aid of their country.") throw new cTestsException($"{nameof(cQuotedPrintableDecoder)}.2");
            }

            string LTest(params string[] pLines)
            {
                using (var lTester = new _Tester())
                {
                    cDecoder lDecoder = new cQuotedPrintableDecoder(lTester);

                    long lBytesSent = 0;
                    int lOffset = 4;

                    foreach (var lLine in pLines)
                    {
                        var lBytes = new cBytes(lLine);
                        lDecoder.WriteAsync(lBytes, lOffset, CancellationToken.None, lContext).Wait();
                        lBytesSent += lBytes.Count - lOffset;
                        lOffset = 0;
                    }

                    lDecoder.FlushAsync(CancellationToken.None, lContext).Wait();

                    if (lTester.FetchedBytesWritten != lBytesSent) throw new cTestsException("bytes sent/written mismatch");

                    return new string(System.Text.Encoding.UTF8.GetChars(lTester.GetBuffer(), 0, lTester.Length));
                }
            }
        }
    }
}