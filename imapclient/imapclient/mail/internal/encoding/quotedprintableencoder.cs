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

        private readonly eQuotedPrintableEncodingType mType;
        private readonly eQuotedPrintableEncodingRule mRule;

        private bool mPendingCR = false;
        private List<byte> mPendingEncodedBytes = new List<byte>();
        private int mPendingInputByteCount = 0;
        private readonly List<byte> mPendingWSP = new List<byte>();
        private readonly List<byte> mPendingEncodedNonWSP = new List<byte>();

        public cQuotedPrintableEncoder(eQuotedPrintableEncodingType pType, eQuotedPrintableEncodingRule pRule)
        {
            mType = pType;
            mRule = pRule;
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

                if (mType == eQuotedPrintableEncodingType.CRLFTerminatedLines)
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
                else if (mType == eQuotedPrintableEncodingType.LFTerminatedLines)
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

        protected sealed override int YGetBufferedInputByteCount()
        {
            var lResult = mPendingInputByteCount + mPendingWSP.Count;
            if (mPendingCR) lResult++;
            if (mPendingEncodedNonWSP.Count != 0) lResult++;
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
                if (mRule == eQuotedPrintableEncodingRule.EBCDIC && kEBCDICNotSome.Contains(pByte)) lNeedsQuoting = true;
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
                    mPendingEncodedNonWSP.AddRange(cMailTools.ByteToHexBytes(pByte));
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
                mPendingEncodedBytes.AddRange(cMailTools.ByteToHexBytes(pByte));
            }
            else mPendingEncodedBytes.Add(pByte);

            mPendingInputByteCount++;
        }

        private void ZSoftLineBreak()
        {
            if (mPendingEncodedBytes.Count + mPendingWSP.Count + 1 > 76)
            {
                if (mPendingWSP.Count == 0) throw new cInternalErrorException(nameof(cQuotedPrintableEncoder), nameof(ZSoftLineBreak));

                mEncodedBytes.AddRange(mPendingEncodedBytes);
                mPendingEncodedBytes.Clear();

                if (mPendingWSP.Count > 1)
                {
                    byte lCarriedWSP = mPendingWSP[mPendingWSP.Count - 1];

                    for (int i = 0; i < mPendingWSP.Count - 1; i++) mPendingEncodedBytes.Add(mPendingWSP[i]);
                    mPendingWSP.Clear();

                    mPendingEncodedBytes.AddRange(kEQUALSCRLF);

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
            mEncodedBytes.AddRange(mPendingEncodedBytes);
            mPendingEncodedBytes.Clear();
            mEncodedBytes.AddRange(mPendingWSP);
            mPendingWSP.Clear();
            mEncodedBytes.AddRange(mPendingEncodedNonWSP);
            mPendingEncodedNonWSP.Clear();
            mEncodedBytes.AddRange(kCRLF);
            mPendingInputByteCount = 0;
        }

        private void ZFlush()
        {
            // this code handles the case where the data does not finish with a line terminator

            if (mPendingCR) ZAdd(cASCII.CR);
            else if (mPendingEncodedBytes.Count == 0 && mPendingWSP.Count == 0) return; // can't have pending nonwsp unless there is something else pending

            mEncodedBytes.AddRange(mPendingEncodedBytes);

            if (mPendingWSP.Count == 0 || mPendingEncodedNonWSP.Count != 0)
            {
                mEncodedBytes.AddRange(mPendingWSP);
                mEncodedBytes.AddRange(mPendingEncodedNonWSP);
                return;
            }

            // this code handles the case where the data finishes with white space
            //  in that case I have to avoid leaving the output finishing with a soft line break
            //   consider the input line          "12345678901234567890123456789012345678901234567890123456789012345678901234 "
            //     which might go to              "12345678901234567890123456789012345678901234567890123456789012345678901234=20" , but this is too long [over the 76 char line length limit]
            //     BUT in soft line break form is "12345678901234567890123456789012345678901234567890123456789012345678901234 ="  , which is normally fine, but,
            //      as the last line in a quoted-printable encoding it is not fine - it is explicitly disallowed in rfc 2045 page 21 section (3)

            int lWSPThatWillFit = 74 - mPendingEncodedBytes.Count;

            if (lWSPThatWillFit < mPendingWSP.Count)
            {
                // add the white space that will fit
                for (int i = 0; i < lWSPThatWillFit; i++) mEncodedBytes.Add(mPendingWSP[i]);
                // add a soft line break
                mEncodedBytes.AddRange(kEQUALSCRLF);
                // add the remaining white space except for the last one
                for (int i = lWSPThatWillFit; i < mPendingWSP.Count - 1; i++) mEncodedBytes.Add(mPendingWSP[i]);
            }
            else for (int i = 0; i < mPendingWSP.Count - 1; i++) mEncodedBytes.Add(mPendingWSP[i]); // add the white space except for the last one

            // add the last white space, encoded
            mEncodedBytes.Add(cASCII.EQUALS);
            mEncodedBytes.AddRange(cMailTools.ByteToHexBytes(mPendingWSP[mPendingWSP.Count - 1]));
        }
    }
}