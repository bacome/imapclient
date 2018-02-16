﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal abstract class cDecoder
    {
        private readonly cMethodControl mMC;
        private readonly Stream mStream;
        private readonly cBatchSizer mWriteSizer;
        private readonly Stopwatch mStopwatch = new Stopwatch();
        private byte[] mBuffer = null;
        private int mBufferSize = 0;
        private int mBytesInBuffer = 0;

        public cDecoder(cMethodControl pMC, Stream pStream, cBatchSizer pWriteSizer)
        {
            mMC = pMC;
            mStream = pStream;
            mWriteSizer = pWriteSizer;
        }

        protected async Task YWriteAsync(IList<byte> pBytes, int pOffset, cTrace.cContext pContext)
        {
            if (mStream.CanTimeout) mStream.WriteTimeout = mMC.Timeout;
            else _ = mMC.Timeout; // check for timeout

            while (pOffset < pBytes.Count)
            {
                if (mBytesInBuffer == 0)
                {
                    mBufferSize = mWriteSizer.Current;
                    if (mBuffer == null || mBufferSize > mBuffer.Length) mBuffer = new byte[mBufferSize];
                }

                while (mBytesInBuffer < mBufferSize && pOffset < pBytes.Count) mBuffer[mBytesInBuffer++] = pBytes[pOffset++];
                if (mBytesInBuffer < mBufferSize) return;

                await YFlushAsync(pContext).ConfigureAwait(false);
            }
        }

        protected async Task YFlushAsync(cTrace.cContext pContext)
        {
            if (mBytesInBuffer == 0) return;

            pContext.TraceVerbose("writing {0} bytes to stream", mBytesInBuffer);

            mStopwatch.Restart();
            await mStream.WriteAsync(mBuffer, 0, mBytesInBuffer, mMC.CancellationToken).ConfigureAwait(false);
            mStopwatch.Stop();

            // store the time taken so the next write is a better size
            mWriteSizer.AddSample(mBytesInBuffer, mStopwatch.ElapsedMilliseconds);

            mBytesInBuffer = 0;
        }

        public abstract Task WriteAsync(IList<byte> pBytes, int pOffset, cTrace.cContext pContext);
        public abstract Task FlushAsync(cTrace.cContext pContext);
    }

    internal class cIdentityDecoder : cDecoder
    {
        public cIdentityDecoder(cMethodControl pMC, Stream pStream, cBatchSizer pWriteSizer) : base(pMC, pStream, pWriteSizer) { }
        public override Task WriteAsync(IList<byte> pBytes, int pOffset, cTrace.cContext pContext) => YWriteAsync(pBytes, pOffset, pContext);
        public override Task FlushAsync(cTrace.cContext pContext) => YFlushAsync(pContext);
    }

    internal abstract class cLineDecoder : cDecoder
    {              
        private List<byte> mLine = new List<byte>();
        private bool mBufferedCR = false;

        public cLineDecoder(cMethodControl pMC, Stream pStream, cBatchSizer pWriteSizer) : base(pMC, pStream, pWriteSizer) { }

        public async sealed override Task WriteAsync(IList<byte> pBytes, int pOffset, cTrace.cContext pContext)
        {
            while (pOffset < pBytes.Count)
            {
                byte lByte = pBytes[pOffset++];
                        
                if (mBufferedCR)
                {
                    mBufferedCR = false;

                    if (lByte == cASCII.LF)
                    {
                        await YWriteLineAsync(mLine, true, pContext).ConfigureAwait(false);
                        mLine.Clear();
                        continue;
                    }

                    mLine.Add(cASCII.CR);
                }

                if (lByte == cASCII.CR) mBufferedCR = true;
                else mLine.Add(lByte);
            }
        }

        public async sealed override Task FlushAsync(cTrace.cContext pContext)
        {
            if (mBufferedCR) mLine.Add(cASCII.CR);
            await YWriteLineAsync(mLine, false, pContext).ConfigureAwait(false);
            await YFlushAsync(pContext).ConfigureAwait(false);
        }

        protected abstract Task YWriteLineAsync(List<byte> pLine, bool pCRLF, cTrace.cContext pContext);
    }

    internal class cBase64Decoder : cLineDecoder
    {
        private readonly List<byte> mBytes = new List<byte>();

        public cBase64Decoder(cMethodControl pMC, Stream pStream, cBatchSizer pWriteSizer) : base(pMC, pStream, pWriteSizer) { }

        protected async override Task YWriteLineAsync(List<byte> pLine, bool pCRLF, cTrace.cContext pContext)
        {
            // build the buffer to decode
            mBytes.Clear();
            foreach (var lByte in pLine) if (cBase64.IsInAlphabet(lByte)) mBytes.Add(lByte);

            // special case
            if (mBytes.Count == 0) return;

            // decode
            if (!cBase64.TryDecode(mBytes, out var lBytes, out var lError)) throw new cContentTransferDecodingException(lError, pContext);

            await YWriteAsync(lBytes, 0, pContext).ConfigureAwait(false);
        }
    }

    internal class cQuotedPrintableDecoder : cLineDecoder
    {
        private static readonly cBytes kNewLine = new cBytes(Environment.NewLine);

        private readonly List<byte> mBytes = new List<byte>();

        public cQuotedPrintableDecoder(cMethodControl pMC, Stream pStream, cBatchSizer pWriteSizer) : base(pMC, pStream, pWriteSizer) { }

        protected async override Task YWriteLineAsync(List<byte> pLine, bool pCRLF, cTrace.cContext pContext)
        {
            byte lByte;

            // strip trailing space

            int lEOL = pLine.Count;

            while (lEOL != 0)
            {
                lByte = pLine[lEOL - 1];
                if (lByte != cASCII.SPACE && lByte != cASCII.TAB) break;
                lEOL--;
            }

            // strip trailing =

            bool lSoftLineBreak;

            if (lEOL != 0 && pLine[lEOL - 1] == cASCII.EQUALS)
            {
                lSoftLineBreak = true;
                lEOL--;
            }
            else lSoftLineBreak = false;

            // decode

            mBytes.Clear();

            int lInputByte = 0;

            while (lInputByte < lEOL)
            {
                lByte = pLine[lInputByte++];

                if (lByte == cASCII.EQUALS)
                {
                    if (lInputByte + 2 <= lEOL && ZGetHexEncodedNibble(pLine, lInputByte, out int lMSN) && ZGetHexEncodedNibble(pLine, lInputByte + 1, out int lLSN))
                    {
                        lInputByte = lInputByte + 2;
                        lByte = (byte)(lMSN << 4 | lLSN);
                    }
                }

                mBytes.Add(lByte);
            }

            // potentially add a line break 
            if (pCRLF && !lSoftLineBreak) mBytes.AddRange(kNewLine);

            await YWriteAsync(mBytes, 0, pContext).ConfigureAwait(false);
        }

        private bool ZGetHexEncodedNibble(List<byte> pLine, int pByte, out int rNibble)
        {
            int lByte = pLine[pByte];

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

            if (LTest("testNow's the time =    \r\n", "for all folk to come=\t \t \r\n", " to the aid of their country.   \t\r\n") != "Now's the time for all folk to come to the aid of their country.\r\n") throw new cTestsException($"{nameof(cQuotedPrintableDecoder)}.1");
            if (LTest("testNow's the time =    \r\n", "for all folk to come=\t \t \r\n", " to the aid of their country.   \t") != "Now's the time for all folk to come to the aid of their country.") throw new cTestsException($"{nameof(cQuotedPrintableDecoder)}.2");





            string LTest(params string[] pLines)
            {
                var lMC = new cMethodControl(-1, System.Threading.CancellationToken.None);
                var lWriteSizer = new cBatchSizer(new cBatchSizerConfiguration(1, 10, 1000, 1));

                using (var lStream = new MemoryStream())
                {
                    cDecoder lDecoder = new cQuotedPrintableDecoder(lMC, lStream, lWriteSizer);

                    int lOffset = 4;

                    foreach (var lLine in pLines)
                    {
                        lDecoder.WriteAsync(new cBytes(lLine), lOffset, lContext).Wait();
                        lOffset = 0;
                    }

                    lDecoder.FlushAsync(lContext).Wait();

                    return new string(System.Text.Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                }
            }
        }
    }
}