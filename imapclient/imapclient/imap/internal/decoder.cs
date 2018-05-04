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
        private readonly byte[] mBuffer = new byte[cMailClient.mLocalStreamBufferSize];

        public cDecoder(iFetchBodyTarget pTarget)
        {
            mTarget = pTarget;
        }

        public abstract Task WriteAsync(IList<byte> pBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext);

        protected async Task YWriteAsync(IList<byte> pBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDecoder), nameof(YWriteAsync), pOffset);

            while (pOffset < pBytes.Count)
            {
                int lBytesInBuffer = 0;
                while (lBytesInBuffer < cMailClient.mLocalStreamBufferSize && pOffset < pBytes.Count) mBuffer[lBytesInBuffer++] = pBytes[pOffset++];
                await mTarget.WriteAsync(mBuffer, lBytesInBuffer, pCancellationToken, lContext).ConfigureAwait(false);
            }
        }

        public virtual Task FlushAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext) => Task.WhenAll(); // TODO => Task.CompletedTask;

        public static cDecoder GetDecoder(bool pBinary, eDecodingRequired pDecoding, iFetchBodyTarget pTarget, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDecoder), nameof(GetDecoder), pBinary, pDecoding);
            if (pBinary || pDecoding == eDecodingRequired.none) return new cIdentityDecoder(pTarget);
            if (pDecoding == eDecodingRequired.base64) return new cBase64Decoder(pTarget);
            if (pDecoding == eDecodingRequired.quotedprintable) return new cQuotedPrintableDecoder(pTarget);
            throw new cContentTransferDecodingException("required decoding not supported", lContext);
        }
    }

    internal class cIdentityDecoder : cDecoder
    {
        public cIdentityDecoder(iFetchBodyTarget pTarget) : base(pTarget) { }
        public override Task WriteAsync(IList<byte> pBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext) => YWriteAsync(pBytes, pOffset, pCancellationToken, pParentContext);
    }

    internal abstract class cLineDecoder : cDecoder
    {              
        private List<byte> mLine = new List<byte>();
        private bool mBufferedCR = false;

        public cLineDecoder(iFetchBodyTarget pTarget) : base(pTarget) { }

        public async sealed override Task WriteAsync(IList<byte> pBytes, int pOffset, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cLineDecoder), nameof(WriteAsync), pOffset);

            while (pOffset < pBytes.Count)
            {
                byte lByte = pBytes[pOffset++];
                        
                if (mBufferedCR)
                {
                    mBufferedCR = false;

                    if (lByte == cASCII.LF)
                    {
                        await YWriteLineAsync(mLine, true, pCancellationToken, lContext).ConfigureAwait(false);
                        mLine.Clear();
                        continue;
                    }

                    mLine.Add(cASCII.CR);
                }

                if (lByte == cASCII.CR) mBufferedCR = true;
                else mLine.Add(lByte);
            }
        }

        public sealed override Task FlushAsync(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cLineDecoder), nameof(FlushAsync));
            if (mBufferedCR) mLine.Add(cASCII.CR);
            return YWriteLineAsync(mLine, false, pCancellationToken, lContext);
        }

        protected abstract Task YWriteLineAsync(List<byte> pLine, bool pCRLF, CancellationToken pCancellationToken, cTrace.cContext pParentContext);
    }

    internal class cBase64Decoder : cLineDecoder
    {
        private readonly List<byte> mBytes = new List<byte>();

        public cBase64Decoder(iFetchBodyTarget pTarget) : base(pTarget) { }

        protected async override Task YWriteLineAsync(List<byte> pLine, bool pCRLF, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBase64Decoder), nameof(YWriteLineAsync), pCRLF);

            // build the buffer to decode
            mBytes.Clear();
            foreach (var lByte in pLine) if (cBase64.IsInAlphabet(lByte)) mBytes.Add(lByte);

            // special case
            if (mBytes.Count == 0) return;

            // decode
            if (!cBase64.TryDecode(mBytes, out var lBytes, out var lError)) throw new cContentTransferDecodingException(lError, lContext);

            await YWriteAsync(lBytes, 0, pCancellationToken, lContext).ConfigureAwait(false);
        }
    }

    internal class cQuotedPrintableDecoder : cLineDecoder
    {
        private static readonly cBytes kNewLine = new cBytes(Environment.NewLine);

        private readonly List<byte> mBytes = new List<byte>();

        public cQuotedPrintableDecoder(iFetchBodyTarget pTarget) : base(pTarget) { }

        protected async override Task YWriteLineAsync(List<byte> pLine, bool pCRLF, CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cQuotedPrintableDecoder), nameof(YWriteLineAsync), pCRLF);

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

            await YWriteAsync(mBytes, 0, pCancellationToken, lContext).ConfigureAwait(false);
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

            cIMAPClient lClient = new cIMAPClient("test_decoder");
            cSectionCache.cAccessor lAccessor;
            var lAccountId = new cAccountId("localhost", "anything");
            var lMailboxName = new cMailboxName("anything", null);
            uint lUID = 1;

            using (var lCache = new cTempFileSectionCache("test_decoder", 1, 1, 60000))
            using (lAccessor = lCache.GetAccessor(lContext))
            {
                if (LTest(true, "testNow's the time =    \r\n", "for all folk to come=\t \t \r\n", " to the aid of their country.   \t\r\n") != "Now's the time for all folk to come to the aid of their country.\r\n") throw new cTestsException($"{nameof(cQuotedPrintableDecoder)}.1");
                if (LTest(false, "testNow's the time =    \r\n", "for all folk to come=\t \t \r\n", " to the aid of their country.   \t") != "Now's the time for all folk to come to the aid of their country.") throw new cTestsException($"{nameof(cQuotedPrintableDecoder)}.2");
            }

            string LTest(bool pCopyFirst, params string[] pLines)
            {
                using (var lReaderWriter = lAccessor.GetReaderWriter(new cSectionCachePersistentKey(lAccountId, lMailboxName, new cUID(1, lUID++), new cSection("1"), eDecodingRequired.none), lContext))
                using (var lMemoryStream = new MemoryStream())
                {
                    Task lTask = null;

                    if (pCopyFirst) lTask = lClient.CopySectionToStreamAsync(lReaderWriter, lMemoryStream, null);

                    cDecoder lDecoder = new cQuotedPrintableDecoder(lReaderWriter);

                    int lOffset = 4;

                    foreach (var lLine in pLines)
                    {
                        lDecoder.WriteAsync(cMethodControl.None, new cBytes(lLine), lOffset, lContext).Wait();
                        lOffset = 0;
                    }

                    lDecoder.FlushAsync(cMethodControl.None, lContext).Wait();

                    if (!pCopyFirst) lTask = lClient.CopySectionToStreamAsync(lReaderWriter, lMemoryStream, null);

                    lTask.Wait();

                    return new string(System.Text.Encoding.UTF8.GetChars(lMemoryStream.GetBuffer(), 0, (int)lMemoryStream.Length));
                }
            }
        }
    }
}