using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    internal static class cParsing
    {
        // parses message data
        //  this code accepts and outputs obsolete rfc 5322 syntax (as opposed to the cValidation routines)
        //  this should only be used on existing message data, not to construct a new message

        public static bool TryParseMsgId(IList<byte> pValue, out string rMessageId)
        {
            cBytesCursor lCursor = new cBytesCursor(pValue);

            if (lCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight) && lCursor.Position.AtEnd)
            {
                rMessageId = cTools.MessageId(lIdLeft, lIdRight);
                return true;
            }

            rMessageId = null;
            return false;
        }

        public static bool TryParseMsgIds(IList<byte> pValue, out cStrings rMessageIds)
        {
            List<string> lMessageIds = new List<string>();

            cBytesCursor lCursor = new cBytesCursor(pValue);

            while (true)
            {
                if (!lCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight)) break;
                lMessageIds.Add(cTools.MessageId(lIdLeft, lIdRight));
            }

            if (lCursor.Position.AtEnd)
            {
                rMessageIds = new cStrings(lMessageIds);
                return true;
            }

            rMessageIds = null;
            return false;
        }

        public static string CalculateBaseSubject(string pSubject)
        {
            if (pSubject == null) return null;
            return cBaseSubjectCalculator.Calculate(pSubject);
        }





        private class cBaseSubjectCalculator
        {
            // from rfc5256 section 2.1

            private string mSubject;
            private int mFront;
            private int mRear;

            private cBaseSubjectCalculator(string pChars)
            {
                mSubject = pChars;
                mFront = 0;
                mRear = mSubject.Length - 1;
            }

            private bool ZSkipCharAtFront(char pChar)
            {
                if (mFront > mRear) return false;
                if (ZCompare(mSubject[mFront], pChar)) { mFront++; return true; }
                return false;
            }

            private bool ZSkipCharsAtFront(string pChars)
            {
                if (mFront > mRear) return false;

                var lBookmark = mFront;

                int lChar = 0;

                while (true)
                {
                    if (!ZCompare(mSubject[mFront], pChars[lChar]))
                    {
                        mFront = lBookmark;
                        return false;
                    }

                    mFront++;
                    lChar++;

                    if (lChar == pChars.Length) return true;

                    if (mFront > mRear)
                    {
                        mFront = lBookmark;
                        return false;
                    }
                }
            }

            private bool ZGetCharAtFront(out char rChar)
            {
                if (mFront > mRear) { rChar = '\0'; return false; }
                rChar = mSubject[mFront++];
                return true;
            }

            private bool ZSkipCharAtRear(char pChar)
            {
                if (mFront > mRear) return false;
                if (ZCompare(mSubject[mRear], pChar)) { mRear--; return true; }
                return false;
            }

            private bool ZSkipCharsAtRear(string pChars)
            {
                if (mFront > mRear) return false;

                var lBookmark = mRear;

                int lChar = pChars.Length - 1;

                while (true)
                {
                    if (!ZCompare(mSubject[mRear], pChars[lChar]))
                    {
                        mRear = lBookmark;
                        return false;
                    }

                    mRear--;
                    lChar--;

                    if (lChar == -1) return true;

                    if (mFront > mRear)
                    {
                        mRear = lBookmark;
                        return false;
                    }
                }
            }

            private bool ZSkipWSP()
            {
                bool lResult = false;

                while (true)
                {
                    if (!ZSkipCharAtFront(' ') && !ZSkipCharAtFront('\t')) return lResult;
                    lResult = true;
                }
            }

            private bool ZSkipBlobReFwd()
            {
                int lBookmark = mFront;

                while (true)
                {
                    if (!ZSkipBlob()) break;
                }

                if (!ZSkipCharsAtFront("re") &&
                    !ZSkipCharsAtFront("fwd") &&
                    !ZSkipCharsAtFront("fw"))
                {
                    mFront = lBookmark;
                    return false;
                }

                ZSkipWSP();
                ZSkipBlob();

                if (!ZSkipCharAtFront(':'))
                {
                    mFront = lBookmark;
                    return false;
                }

                return true;
            }

            private bool ZSkipBlob()
            {
                int lBookmark = mFront;

                if (!ZSkipCharAtFront('[')) return false;

                while (true)
                {
                    if (!ZGetCharAtFront(out var lChar) || lChar == '[') { mFront = lBookmark; return false; }
                    if (lChar == ']') break;
                }

                ZSkipWSP();

                return true;
            }

            private bool ZCompare(char pChar1, char pChar2)
            {
                char lChar1;
                if (pChar1 < 'a') lChar1 = pChar1;
                else if (pChar1 > 'z') lChar1 = pChar1;
                else lChar1 = (char)(pChar1 - 'a' + 'A');

                char lChar2;
                if (pChar2 < 'a') lChar2 = pChar2;
                else if (pChar2 > 'z') lChar2 = pChar2;
                else lChar2 = (char)(pChar2 - 'a' + 'A');

                return lChar1 == lChar2;
            }

            public static string Calculate(string pSubject)
            {
                cBaseSubjectCalculator lCalculator = new cBaseSubjectCalculator(pSubject);
                int lBookmark;

                while (true)
                {
                    // 2.1.2
                    while (true)
                    {
                        if (!lCalculator.ZSkipCharsAtRear("(fwd)") &&
                            !lCalculator.ZSkipCharAtRear(' ') &&
                            !lCalculator.ZSkipCharAtRear('\t')) break;
                    }

                    while (true) // 2.1.5
                    {
                        // 2.1.3
                        if (lCalculator.ZSkipWSP()) continue;
                        if (lCalculator.ZSkipBlobReFwd()) continue;

                        // 2.1.4

                        lBookmark = lCalculator.mFront;

                        if (lCalculator.ZSkipBlob())
                        {
                            if (lCalculator.mFront > lCalculator.mRear)
                            {
                                lCalculator.mFront = lBookmark;
                                break;
                            }

                            continue;
                        }

                        break;
                    }

                    // 2.1.6

                    lBookmark = lCalculator.mFront;

                    if (!lCalculator.ZSkipCharsAtFront("[fwd:")) break;

                    if (!lCalculator.ZSkipCharAtRear(']'))
                    {
                        lCalculator.mFront = lBookmark;
                        break;
                    }
                }

                if (lCalculator.mFront > lCalculator.mRear) return null;

                return lCalculator.mSubject.Substring(lCalculator.mFront, lCalculator.mRear - lCalculator.mFront + 1);
            }
        }
    }
}