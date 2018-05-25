using System;
using System.Collections.Generic;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal class cQuotedPrintableEncoder : cEncoder
    {
        private static readonly cBytes kCRLF = new cBytes("\r\n");
        private static readonly cBytes kEQUALSCRLF = new cBytes("=\r\n");
        private static readonly cBytes kEBCDICNotSome = new cBytes("!\"#$@[\\]^`{|}~");

        private readonly eQuotedPrintableEncodeSourceType mSourceType;
        private readonly eQuotedPrintableEncodeQuotingRule mQuotingRule;

        private bool mPendingCR = false;
        private List<byte> mPendingBytes = new List<byte>();
        private int mPendingInputBytes = 0;
        private readonly List<byte> mPendingWSP = new List<byte>();
        private readonly List<byte> mPendingNonWSP = new List<byte>();

        public cQuotedPrintableEncoder(eQuotedPrintableEncodeSourceType pSourceType, eQuotedPrintableEncodeQuotingRule pQuotingRule)
        {
            mSourceType = pSourceType;
            mQuotingRule = pQuotingRule;
        }

        protected sealed override void YEncode(byte[] pInput, int pCount)
        {
            if (pCount == 0)
            {
                ZFlush();
                return;
            }

            for (int i = 0; i < pCount; i++)
            {
                var lByte = pInput[i];

                if (mSourceType == eQuotedPrintableEncodeSourceType.CRLFTerminatedLines)
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
                else if (mSourceType == eQuotedPrintableEncodeSourceType.LFTerminatedLines)
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

        protected sealed override int YGetBufferedInputBytes()
        {
            var lResult = mPendingInputBytes + mPendingWSP.Count;
            if (mPendingCR) lResult++;
            if (mPendingNonWSP.Count != 0) lResult++;
            return lResult;
        }

        private void ZAdd(byte pByte)
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

            if (mPendingBytes.Count + mPendingWSP.Count + mPendingNonWSP.Count + lCount > 76) ZSoftLineBreak();

            if (mPendingNonWSP.Count != 0) throw new cInternalErrorException(nameof(cQuotedPrintableEncoder), nameof(ZAdd));

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
            mPendingInputBytes += mPendingWSP.Count;
            mPendingWSP.Clear();

            if (lNeedsQuoting)
            {
                mPendingBytes.Add(cASCII.EQUALS);
                mPendingBytes.AddRange(cTools.ByteToHexBytes(pByte));
            }
            else mPendingBytes.Add(pByte);

            mPendingInputBytes++;
        }

        private void ZSoftLineBreak()
        {
            if (mPendingBytes.Count + mPendingWSP.Count + 1 > 76)
            {
                if (mPendingWSP.Count == 0) throw new cInternalErrorException(nameof(cQuotedPrintableEncoder), nameof(ZSoftLineBreak));

                mOutput.AddRange(mPendingBytes);
                mPendingBytes.Clear();

                if (mPendingWSP.Count > 1)
                {
                    byte lCarriedWSP = mPendingWSP[mPendingWSP.Count - 1];

                    for (int i = 0; i < mPendingWSP.Count - 1; i++) mOutput.Add(mPendingWSP[i]);
                    mPendingWSP.Clear();

                    mOutput.AddRange(kEQUALSCRLF);

                    if (mPendingNonWSP.Count == 0)
                    {
                        mPendingInputBytes = 0;
                        mPendingWSP.Add(lCarriedWSP);
                        return;
                    }

                    mPendingBytes.Add(lCarriedWSP);
                    mPendingBytes.AddRange(mPendingNonWSP);
                    mPendingNonWSP.Clear();
                    mPendingInputBytes = 2;

                    return;
                }

                mOutput.AddRange(kEQUALSCRLF);

                if (mPendingNonWSP.Count == 0)
                {
                    mPendingInputBytes = 0;
                    return;
                }

                mPendingBytes.AddRange(mPendingWSP);
                mPendingWSP.Clear();
                mPendingBytes.AddRange(mPendingNonWSP);
                mPendingNonWSP.Clear();
                mPendingInputBytes = 2;

                return;
            }

            mOutput.AddRange(mPendingBytes);
            mPendingBytes.Clear();
            mOutput.AddRange(mPendingWSP);
            mPendingWSP.Clear();
            mOutput.AddRange(kEQUALSCRLF);

            if (mPendingNonWSP.Count == 0) mPendingInputBytes = 0;
            else
            {
                mPendingBytes.AddRange(mPendingNonWSP);
                mPendingNonWSP.Clear();
                mPendingInputBytes = 1;
            }
        }

        private void ZAddHardLineBreak()
        {
            mOutput.AddRange(mPendingBytes);
            mPendingBytes.Clear();
            mOutput.AddRange(mPendingWSP);
            mPendingWSP.Clear();
            mOutput.AddRange(mPendingNonWSP);
            mPendingNonWSP.Clear();
            mOutput.AddRange(kCRLF);
            mPendingInputBytes = 0;
        }

        private void ZFlush()
        {
            if (mPendingCR) ZAdd(cASCII.CR);
            else if (mPendingBytes.Count == 0 && mPendingWSP.Count == 0) return; // can't have pending nonwsp unless there is something else pending

            mOutput.AddRange(mPendingBytes);

            if (mPendingWSP.Count == 0 || mPendingNonWSP.Count != 0)
            {
                mOutput.AddRange(mPendingWSP);
                mOutput.AddRange(mPendingNonWSP);
                return;
            }

            // this code handles the case where the data finishes with white space
            //  in that case I have to avoid leaving the output finishing with a soft line break
            //   consider the input line          "12345678901234567890123456789012345678901234567890123456789012345678901234 "
            //     which might go to              "12345678901234567890123456789012345678901234567890123456789012345678901234=20" , but this is too long [over the 76 char line length limit]
            //     BUT in soft line break form is "12345678901234567890123456789012345678901234567890123456789012345678901234 ="  , which is normally fine, but,
            //      as the last line in a quoted-printable encoding it is not fine - it is explicitly disallowed in rfc 2045 page 21 section (3)

            int lWSPThatWillFit = 74 - mPendingBytes.Count;

            if (lWSPThatWillFit < mPendingWSP.Count)
            {
                // add the white space that will fit
                for (int i = 0; i < lWSPThatWillFit; i++) mOutput.Add(mPendingWSP[i]);
                // add a soft line break
                mOutput.AddRange(kEQUALSCRLF);
                // add the remaining white space except for the last one
                for (int i = lWSPThatWillFit; i < mPendingWSP.Count - 1; i++) mOutput.Add(mPendingWSP[i]);
            }
            else for (int i = 0; i < mPendingWSP.Count - 1; i++) mOutput.Add(mPendingWSP[i]); // add the white space except for the last one

            // add the last white space, encoded
            mOutput.Add(cASCII.EQUALS);
            mOutput.AddRange(cTools.ByteToHexBytes(mPendingWSP[mPendingWSP.Count - 1]));

            // terminate the line
            mOutput.AddRange(kCRLF);
        }
    }
}