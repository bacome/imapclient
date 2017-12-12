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
            private abstract class cDecoder
            {
                private readonly Stream mStream;
                private readonly Stopwatch mStopwatch = new Stopwatch();
                private byte[] mBuffer = null;
                private int mCount = 0;

                public cDecoder(Stream pStream)
                {
                    mStream = pStream;
                }

                protected async Task YWriteAsync(cMethodControl pMC, IList<byte> pBytes, int pOffset, cBatchSizer pWriteSizer, cTrace.cContext pContext)
                {
                    if (mStream.CanTimeout) mStream.WriteTimeout = pMC.Timeout;

                    while (pOffset < pBytes.Count)
                    {
                        if (mCount == 0) mBuffer = new byte[pWriteSizer.Current];

                        while (mCount < mBuffer.Length && pOffset < pBytes.Count) mBuffer[mCount++] = pBytes[pOffset++];
                        if (mCount < mBuffer.Length) return;

                        await YFlushAsync(pMC, pWriteSizer, pContext).ConfigureAwait(false);
                    }
                }

                protected async Task YFlushAsync(cMethodControl pMC, cBatchSizer pWriteSizer, cTrace.cContext pContext)
                {
                    if (mCount == 0) return;

                    pContext.TraceVerbose("writing {0} bytes to stream", mCount);

                    mStopwatch.Restart();
                    await mStream.WriteAsync(mBuffer, 0, mCount, pMC.CancellationToken).ConfigureAwait(false);
                    mStopwatch.Stop();

                    // store the time taken so the next write is a better size
                    pWriteSizer.AddSample(mCount, mStopwatch.ElapsedMilliseconds);

                    mCount = 0;
                }

                public abstract Task WriteAsync(cMethodControl pMC, cBytes pBytes, int pOffset, cBatchSizer pWriteSizer, cTrace.cContext pContext);
                public abstract Task FlushAsync(cMethodControl pMC, cBatchSizer pWriteSizer, cTrace.cContext pContext);
            }

            private class cIdentityDecoder : cDecoder
            {
                public cIdentityDecoder(Stream pStream) : base(pStream) { }
                public override Task WriteAsync(cMethodControl pMC, cBytes pBytes, int pOffset, cBatchSizer pWriteSizer, cTrace.cContext pContext) => YWriteAsync(pMC, pBytes, pOffset, pWriteSizer, pContext);
                public override Task FlushAsync(cMethodControl pMC, cBatchSizer pWriteSizer, cTrace.cContext pContext) => YFlushAsync(pMC, pWriteSizer, pContext);
            }

            private abstract class cLineDecoder : cDecoder
            {              
                private List<byte> mLine = new List<byte>();
                private bool mBufferedCR = false;

                public cLineDecoder(Stream pStream) : base(pStream) { }

                public async sealed override Task WriteAsync(cMethodControl pMC, cBytes pBytes, int pOffset, cBatchSizer pWriteSizer, cTrace.cContext pContext)
                {
                    while (pOffset < pBytes.Count)
                    {
                        byte lByte = pBytes[pOffset++];
                        
                        if (mBufferedCR)
                        {
                            mBufferedCR = false;

                            if (lByte == cASCII.LF)
                            {
                                await YWriteLineAsync(pMC, mLine, pWriteSizer, pContext).ConfigureAwait(false);
                                mLine.Clear();
                                continue;
                            }

                            mLine.Add(cASCII.CR);
                        }

                        if (lByte == cASCII.CR) mBufferedCR = true;
                        else mLine.Add(lByte);
                    }
                }

                public async sealed override Task FlushAsync(cMethodControl pMC, cBatchSizer pWriteSizer, cTrace.cContext pContext)
                {
                    if (mBufferedCR) mLine.Add(cASCII.CR);
                    await YWriteLineAsync(pMC, mLine, pWriteSizer, pContext).ConfigureAwait(false);
                    await YFlushAsync(pMC, pWriteSizer, pContext).ConfigureAwait(false);
                }

                protected abstract Task YWriteLineAsync(cMethodControl pMC, List<byte> pLine, cBatchSizer pWriteSizer, cTrace.cContext pContext);
            }

            private class cBase64Decoder : cLineDecoder
            {
                private readonly List<byte> mBytes = new List<byte>();

                public cBase64Decoder(Stream pStream) : base(pStream) { }

                protected async override Task YWriteLineAsync(cMethodControl pMC, List<byte> pLine, cBatchSizer pWriteSizer, cTrace.cContext pContext)
                {
                    // build the buffer to decode
                    mBytes.Clear();
                    foreach (var lByte in pLine) if (cBase64.IsInAlphabet(lByte)) mBytes.Add(lByte);

                    // special case
                    if (mBytes.Count == 0) return;

                    // decode
                    if (!cBase64.TryDecode(mBytes, out var lBytes, out var lError)) throw new cContentTransferDecodingException(lError, pContext);

                    await YWriteAsync(pMC, lBytes, 0, pWriteSizer, pContext).ConfigureAwait(false);
                }
            }

            private class cQuotedPrintableDecoder : cLineDecoder
            {
                private readonly List<byte> mBytes = new List<byte>();

                public cQuotedPrintableDecoder(Stream pStream) : base(pStream) { }

                protected async override Task YWriteLineAsync(cMethodControl pMC, List<byte> pLine, cBatchSizer pWriteSizer, cTrace.cContext pContext)
                {
                    byte lByte;

                    // special case
                    if (pLine.Count == 0) return;

                    // strip trailing space

                    int lEOL = pLine.Count;

                    while (true)
                    {
                        lByte = pLine[lEOL - 1];
                        if (lByte != cASCII.SPACE && lByte != cASCII.TAB) break;
                        if (--lEOL == 0) return;
                    }

                    // strip trailing =

                    bool lSoftLineBreak;

                    if (pLine[lEOL - 1] == cASCII.EQUALS)
                    {
                        if (--lEOL == 0) return;
                        lSoftLineBreak = true;
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

                    if (!lSoftLineBreak)
                    {
                        mBytes.Add(cASCII.CR);
                        mBytes.Add(cASCII.LF);
                    }

                    await YWriteAsync(pMC, mBytes, 0, pWriteSizer, pContext).ConfigureAwait(false);
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





                    string LTest(params string[] pLines)
                    {
                        var lMC = new cMethodControl(-1, System.Threading.CancellationToken.None);
                        var lWriteSizer = new cBatchSizer(new cBatchSizerConfiguration(1, 10, 1000, 1));

                        using (var lStream = new MemoryStream())
                        {
                            cDecoder lDecoder = new cQuotedPrintableDecoder(lStream);

                            int lOffset = 4;

                            foreach (var lLine in pLines)
                            {
                                lDecoder.WriteAsync(lMC, new cBytes(lLine), lOffset, lWriteSizer, lContext).Wait();
                                lOffset = 0;
                            }

                            lDecoder.FlushAsync(lMC, lWriteSizer, lContext).Wait();

                            return new string(System.Text.Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                        }
                    }
                }
            }
        }
    }
}