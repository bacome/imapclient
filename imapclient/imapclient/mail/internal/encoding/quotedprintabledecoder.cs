using System;
using System.Diagnostics;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cQuotedPrintableDecoder : cDecoder
    {
        private static readonly cBytes kNewLine = new cBytes(Environment.NewLine);

        public cQuotedPrintableDecoder() { }

        protected sealed override void YDecode(bool pCRLF)
        {
            byte lByte;

            // strip trailing space

            int lEOL = mPendingInput.Count;

            while (lEOL != 0)
            {
                lByte = mPendingInput[lEOL - 1];
                if (lByte != cASCII.SPACE && lByte != cASCII.TAB) break;
                lEOL--;
            }

            // strip trailing =

            bool lSoftLineBreak;

            if (lEOL != 0 && mPendingInput[lEOL - 1] == cASCII.EQUALS)
            {
                lSoftLineBreak = true;
                lEOL--;
            }
            else lSoftLineBreak = false;

            // decode

            int lInputPosition = 0;

            while (lInputPosition < lEOL)
            {
                lByte = mPendingInput[lInputPosition++];

                if (lByte == cASCII.EQUALS)
                {
                    if (lInputPosition + 2 <= lEOL && ZGetHexEncodedNibble(lInputPosition, out int lMSN) && ZGetHexEncodedNibble(lInputPosition + 1, out int lLSN))
                    {
                        lInputPosition = lInputPosition + 2;
                        lByte = (byte)(lMSN << 4 | lLSN);
                    }
                }

                mOutput.Add(lByte);
            }

            // potentially add a line break 
            if (pCRLF && !lSoftLineBreak) mOutput.AddRange(kNewLine);
        }

        private bool ZGetHexEncodedNibble(int pInputPosition, out int rNibble)
        {
            int lByte = mPendingInput[pInputPosition];

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
                using (var lStream = new MemoryStream())
                {
                    cDecoder lDecoder = new cQuotedPrintableDecoder();

                    int lOffset = 4;
                    byte[] lBuffer;

                    foreach (var lLine in pLines)
                    {
                        var lBytes = new cBytes(lLine);
                        lBuffer = lDecoder.Decode(lBytes, lOffset, lBytes.Count - lOffset);
                        lStream.Write(lBuffer, 0, lBuffer.Length);
                        lOffset = 0;
                    }

                    lBuffer = lDecoder.Decode(cBytes.Empty, 0, 0);
                    lStream.Write(lBuffer, 0, lBuffer.Length);

                    return new string(System.Text.Encoding.UTF8.GetChars(lStream.GetBuffer(), 0, (int)lStream.Length));
                }
            }
        }
    }
}
