using System;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    public class cQuotedPrintableDecoder : cDecoder
    {
        private static readonly cBytes kNewLine = new cBytes(Environment.NewLine);

        public cQuotedPrintableDecoder() { }

        protected sealed override void YDecode(bool pCRLF)
        {
            byte lByte;

            // strip trailing space

            int lEOL = mBufferedInputBytes.Count;

            while (lEOL != 0)
            {
                lByte = mBufferedInputBytes[lEOL - 1];
                if (lByte != cASCII.SPACE && lByte != cASCII.TAB) break;
                lEOL--;
            }

            // strip trailing =

            bool lSoftLineBreak;

            if (lEOL != 0 && mBufferedInputBytes[lEOL - 1] == cASCII.EQUALS)
            {
                lSoftLineBreak = true;
                lEOL--;
            }
            else lSoftLineBreak = false;

            // decode

            int lInputPosition = 0;

            while (lInputPosition < lEOL)
            {
                lByte = mBufferedInputBytes[lInputPosition++];

                if (lByte == cASCII.EQUALS)
                {
                    if (lInputPosition + 2 <= lEOL && ZGetHexEncodedNibble(lInputPosition, out int lMSN) && ZGetHexEncodedNibble(lInputPosition + 1, out int lLSN))
                    {
                        lInputPosition = lInputPosition + 2;
                        lByte = (byte)(lMSN << 4 | lLSN);
                    }
                }

                mDecodedBytes.Add(lByte);
            }

            // potentially add a line break 
            if (pCRLF && !lSoftLineBreak) mDecodedBytes.AddRange(kNewLine);
        }

        private bool ZGetHexEncodedNibble(int pInputPosition, out int rNibble)
        {
            int lByte = mBufferedInputBytes[pInputPosition];

            if (lByte < cASCII.ZERO) { rNibble = 0; return false; }
            if (lByte <= cASCII.NINE) { rNibble = lByte - cASCII.ZERO; return true; }

            if (lByte < cASCII.A) { rNibble = 0; return false; }
            if (lByte <= cASCII.F) { rNibble = 10 + lByte - cASCII.A; return true; }

            if (lByte < cASCII.a) { rNibble = 0; return false; }
            if (lByte <= cASCII.f) { rNibble = 10 + lByte - cASCII.a; return true; }

            rNibble = 0;
            return false;
        }
    }
}
