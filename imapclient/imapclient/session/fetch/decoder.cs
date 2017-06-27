using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cDecoder
            {
                private readonly Stream mStream;

                public cDecoder(Stream pStream)
                {
                    mStream = pStream;
                }

                protected virtual Task YWriteAsync(cMethodControl pMC, byte[] lBuffer, int pCount)
                {
                    if (mStream.CanTimeout) mStream.WriteTimeout = pMC.Timeout;
                    return mStream.WriteAsync(lBuffer, 0, pCount, pMC.CancellationToken);
                }

                public abstract Task WriteAsync(cMethodControl pMC, cBytes pBytes, int pOffset);
                public abstract bool HasUndecodedBytes();
            }

            private class cIdentityDecoder : cDecoder
            {
                public cIdentityDecoder(Stream pStream) : base(pStream) { }

                public override Task WriteAsync(cMethodControl pMC, cBytes pBytes, int pOffset)
                {
                    int lCount = pBytes.Count - pOffset;
                    byte[] lBuffer = new byte[lCount];
                    for (int i = 0, j = pOffset; i < lCount; i++, j++) lBuffer[i] = pBytes[j];
                    return YWriteAsync(pMC, lBuffer, lCount);
                }

                public override bool HasUndecodedBytes() => false;
            }

            private abstract class cLineDecoder : cDecoder
            {              
                private List<byte> mLine = new List<byte>();
                private bool mBufferedCR = false;

                public cLineDecoder(Stream pStream) : base(pStream) { }

                public async sealed override Task WriteAsync(cMethodControl pMC, cBytes pBytes, int pOffset)
                {
                    while (pOffset < pBytes.Count)
                    {
                        byte lByte = pBytes[pOffset++];
                        
                        if (mBufferedCR)
                        {
                            mBufferedCR = false;

                            if (lByte == cASCII.LF)
                            {
                                await YWriteAsync(pMC, mLine).ConfigureAwait(false);
                                mLine.Clear();
                                continue;
                            }

                            mLine.Add(cASCII.CR);
                        }

                        if (lByte == cASCII.CR) mBufferedCR = true;
                        else mLine.Add(lByte);
                    }
                }

                protected abstract Task YWriteAsync(cMethodControl pMC, List<byte> pLine);

                public sealed override bool HasUndecodedBytes() => mLine.Count > 0 || mBufferedCR; 
            }

            private class cBase64Decoder : cLineDecoder
            {
                private byte[] mInputBytes;

                public cBase64Decoder(Stream pStream) : base(pStream)
                {
                    mInputBytes = new byte[76];
                }

                protected async override Task YWriteAsync(cMethodControl pMC, List<byte> pLine)
                {
                    // expand buffer if required
                    if (pLine.Count > mInputBytes.Length) mInputBytes = new byte[pLine.Count];

                    // build the buffer to decode
                    int lInputByte = 0;
                    foreach (var lByte in pLine) if (cBase64.IsValidByte(lByte)) mInputBytes[lInputByte++] = lByte;

                    // special case
                    if (lInputByte == 0) return;

                    // decode
                    if (!cBase64.TryDecode(mInputBytes, out var lOutputBytes, out var lError)) throw new cContentTransferDecodingException(lError);

                    await YWriteAsync(pMC, lOutputBytes.ToArray(), lOutputBytes.Count).ConfigureAwait(false);
                }
            }

            private class cQuotedPrintableDecoder : cLineDecoder
            {
                private byte[] mOutputBytes;

                public cQuotedPrintableDecoder(Stream pStream) : base(pStream)
                {
                    mOutputBytes = new byte[78];
                }

                protected async override Task YWriteAsync(cMethodControl pMC, List<byte> pLine)
                {
                    byte lByte;

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

                    // expand buffer if required
                    if (lEOL + 2 > mOutputBytes.Length) mOutputBytes = new byte[lEOL + 2];

                    // decode

                    int lOutputByte = 0;
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

                        mOutputBytes[lOutputByte++] = lByte;
                    }

                    if (!lSoftLineBreak)
                    {
                        mOutputBytes[lOutputByte++] = cASCII.CR;
                        mOutputBytes[lOutputByte++] = cASCII.LF;
                    }

                    await YWriteAsync(pMC, mOutputBytes, lOutputByte).ConfigureAwait(false);
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
                        cMethodControl lMC = new cMethodControl(-1, System.Threading.CancellationToken.None);

                        using (var lStream = new MemoryStream())
                        {
                            cDecoder lDecoder = new cQuotedPrintableDecoder(lStream);

                            int lOffset = 4;

                            foreach (var lLine in pLines)
                            {
                                lDecoder.WriteAsync(lMC, new cBytes(lLine), lOffset).Wait();
                                lOffset = 0;
                            }

                            return new string(System.Text.Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                        }
                    }
                }
            }
        }
    }
}