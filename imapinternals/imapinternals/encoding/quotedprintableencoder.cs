using System;
using System.Collections.Generic;
using work.bacome.imapclient;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    public enum eQuotedPrintableEncoderInputType
    {
        environmentterminated,
        binary,
        crlfterminated,
        lfterminated,
    }

    public enum eQuotedPrintableEncodingRule
    {
        minimal,
        ebcdic
    }

    public class cQuotedPrintableEncoder : cEncoder
    {
        private static readonly cBytes kCRLF = new cBytes("\r\n");
        private static readonly cBytes kEQUALSCRLF = new cBytes("=\r\n");
        private static readonly cBytes kEBCDICNotSome = new cBytes("!\"#$@[\\]^`{|}~");

        private readonly eQuotedPrintableEncoderInputType mInputType;
        private readonly eQuotedPrintableEncodingRule mEncodingRule;

        private bool mPendingCR = false;
        private List<byte> mPendingEncodedBytes = new List<byte>();
        private int mPendingInputByteCount = 0;
        private readonly List<byte> mPendingWSP = new List<byte>();
        private readonly List<byte> mPendingEncodedNonWSP = new List<byte>();

        public cQuotedPrintableEncoder(eQuotedPrintableEncoderInputType pInputType, eQuotedPrintableEncodingRule pEncodingRule)
        {
            if (pInputType == eQuotedPrintableEncoderInputType.environmentterminated)
            {
                if (Environment.NewLine == "\r\n") mInputType = eQuotedPrintableEncoderInputType.crlfterminated;
                else if (Environment.NewLine == "\n") mInputType = eQuotedPrintableEncoderInputType.lfterminated;
                else mInputType = eQuotedPrintableEncoderInputType.binary;
            }
            else mInputType = pInputType;

            mEncodingRule = pEncodingRule;
        }

        public sealed override int BufferedInputByteCount
        {
            get
            {
                var lResult = mPendingInputByteCount + mPendingWSP.Count;
                if (mPendingCR) lResult++;
                if (mPendingEncodedNonWSP.Count != 0) lResult++;
                return lResult;
            }
        }

        protected sealed override void YEncode(IList<byte> pInputBytes, int pOffset, int pCount)
        {
            if (pCount == 0)
            {
                ZFlush();
                return;
            }

            for (int i = 0; i < pCount; i++)
            {
                var lByte = pInputBytes[pOffset + i];

                if (mInputType == eQuotedPrintableEncoderInputType.crlfterminated)
                {
                    if (mPendingCR)
                    {
                        mPendingCR = false;

                        if (lByte == cASCII.LF)
                        {
                            ZAddHardLineBreak();
                            continue;
                        }

                        ZAdd(cASCII.CR);
                    }

                    if (lByte == cASCII.CR)
                    {
                        mPendingCR = true;
                        continue;
                    }
                }
                else if (mInputType == eQuotedPrintableEncoderInputType.lfterminated)
                {
                    if (lByte == cASCII.LF)
                    {
                        ZAddHardLineBreak();
                        continue;
                    }
                }

                ZAdd(lByte);
            }
        }

        private void ZAdd(byte pByte)
        {
            bool lNeedsQuoting;

            if (pByte == cASCII.TAB) lNeedsQuoting = false;
            else if (pByte < cASCII.SPACE) lNeedsQuoting = true;
            else if (pByte == cASCII.EQUALS) lNeedsQuoting = true;
            else if (pByte < cASCII.DEL)
            {
                if (mEncodingRule == eQuotedPrintableEncodingRule.ebcdic && kEBCDICNotSome.Contains(pByte)) lNeedsQuoting = true;
                else lNeedsQuoting = false;
            }
            else lNeedsQuoting = true;

            int lCount;
            if (lNeedsQuoting) lCount = 3;
            else lCount = 1;

            if (mPendingEncodedBytes.Count + mPendingWSP.Count + mPendingEncodedNonWSP.Count + lCount > 76) ZSoftLineBreak();

            if (mPendingEncodedNonWSP.Count != 0) throw new cInternalErrorException(nameof(cQuotedPrintableEncoder), nameof(ZAdd));

            if (pByte == cASCII.TAB || pByte == cASCII.SPACE)
            {
                mPendingWSP.Add(pByte);
                return;
            }

            if (mPendingEncodedBytes.Count + mPendingWSP.Count + lCount == 76)
            {
                if (lNeedsQuoting)
                {
                    mPendingEncodedNonWSP.Add(cASCII.EQUALS);
                    mPendingEncodedNonWSP.AddRange(cTools.ByteToHexBytes(pByte));
                }
                else mPendingEncodedNonWSP.Add(pByte);

                return;
            }

            mPendingEncodedBytes.AddRange(mPendingWSP);
            mPendingInputByteCount += mPendingWSP.Count;
            mPendingWSP.Clear();

            if (lNeedsQuoting)
            {
                mPendingEncodedBytes.Add(cASCII.EQUALS);
                mPendingEncodedBytes.AddRange(cTools.ByteToHexBytes(pByte));
            }
            else mPendingEncodedBytes.Add(pByte);

            mPendingInputByteCount++;
        }

        private void ZSoftLineBreak()
        {
            if (mPendingEncodedBytes.Count + mPendingWSP.Count + 1 > 76)
            {
                // this is the case where adding the '=' at the end of the spaces would take us over the 76 char line length limit

                if (mPendingWSP.Count == 0) throw new cInternalErrorException(nameof(cQuotedPrintableEncoder), nameof(ZSoftLineBreak));

                mEncodedBytes.AddRange(mPendingEncodedBytes);
                mPendingEncodedBytes.Clear();

                if (mPendingWSP.Count > 1)
                {
                    byte lCarriedWSP = mPendingWSP[mPendingWSP.Count - 1];

                    for (int i = 0; i < mPendingWSP.Count - 1; i++) mEncodedBytes.Add(mPendingWSP[i]);
                    mPendingWSP.Clear();

                    mEncodedBytes.AddRange(kEQUALSCRLF);

                    if (mPendingEncodedNonWSP.Count == 0)
                    {
                        mPendingInputByteCount = 0;
                        mPendingWSP.Add(lCarriedWSP);
                        return;
                    }

                    mPendingEncodedBytes.Add(lCarriedWSP);
                    mPendingEncodedBytes.AddRange(mPendingEncodedNonWSP);
                    mPendingEncodedNonWSP.Clear();
                    mPendingInputByteCount = 2;

                    return;
                }

                mEncodedBytes.AddRange(kEQUALSCRLF);

                if (mPendingEncodedNonWSP.Count == 0)
                {
                    mPendingInputByteCount = 0;
                    return;
                }

                mPendingEncodedBytes.AddRange(mPendingWSP);
                mPendingWSP.Clear();
                mPendingEncodedBytes.AddRange(mPendingEncodedNonWSP);
                mPendingEncodedNonWSP.Clear();
                mPendingInputByteCount = 2;

                return;
            }

            mEncodedBytes.AddRange(mPendingEncodedBytes);
            mPendingEncodedBytes.Clear();
            mEncodedBytes.AddRange(mPendingWSP);
            mPendingWSP.Clear();
            mEncodedBytes.AddRange(kEQUALSCRLF);

            if (mPendingEncodedNonWSP.Count == 0) mPendingInputByteCount = 0;
            else
            {
                mPendingEncodedBytes.AddRange(mPendingEncodedNonWSP);
                mPendingEncodedNonWSP.Clear();
                mPendingInputByteCount = 1;
            }
        }

        private void ZAddHardLineBreak()
        {
            if (mPendingWSP.Count == 0 || mPendingEncodedNonWSP.Count > 0)
            {
                // this is the case where the line does not end with white space
                mEncodedBytes.AddRange(mPendingEncodedBytes);
                mPendingEncodedBytes.Clear();
                mEncodedBytes.AddRange(mPendingWSP);
                mPendingWSP.Clear();
                mEncodedBytes.AddRange(mPendingEncodedNonWSP);
                mPendingEncodedNonWSP.Clear();
                mEncodedBytes.AddRange(kCRLF);
                mPendingInputByteCount = 0;
                return;
            }

            // this is the case where the line does end with white space
            //  the last white space char on the line has to be encoded BUT doing so may make the line longer than 76 chars, meaning a soft line break has to be inserted just before the last space

            var lInsertSoftLineBreak = mPendingEncodedBytes.Count + mPendingWSP.Count + 2 > 76;

            mEncodedBytes.AddRange(mPendingEncodedBytes);
            mPendingEncodedBytes.Clear();

            for (int i = 0; i < mPendingWSP.Count - 1; i++) mEncodedBytes.Add(mPendingWSP[i]);

            if (lInsertSoftLineBreak) mEncodedBytes.AddRange(kEQUALSCRLF);

            mEncodedBytes.Add(cASCII.EQUALS);
            mEncodedBytes.AddRange(cTools.ByteToHexBytes(mPendingWSP[mPendingWSP.Count - 1]));

            mPendingWSP.Clear();

            mEncodedBytes.AddRange(kCRLF);
            mPendingInputByteCount = 0;
        }

        private void ZFlush()
        {
            // this code handles the case where the data does not finish with a line terminator

            if (mPendingCR)
            {
                mPendingCR = false;
                ZAdd(cASCII.CR);
            }
            else if (mPendingEncodedBytes.Count == 0 && mPendingWSP.Count == 0) return; // nothing pending

            // output a line with a soft line break
            ZSoftLineBreak();

            if (mPendingEncodedBytes.Count == 0 && mPendingWSP.Count == 0) return;

            // this code handles the cases where the data ends with a line that is exactly 76 chars long

            // output another line with a soft line break
            ZSoftLineBreak();

            if (mPendingEncodedBytes.Count != 0 || mPendingWSP.Count != 0) throw new cInternalErrorException(nameof(cQuotedPrintableEncoder), nameof(ZFlush));
        }
    }
}