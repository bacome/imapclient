using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    internal partial class cBytesCursor
    {
        public bool ProcessExtendedValue(out cExtendedValue rValue)
        {
            //  NOTE: this routine does not return the cursor to its original position if it fails

            if (GetSequenceSet(out var lSequenceSet))
            {
                rValue = new cExtendedValue.cSequenceSetEV(lSequenceSet);
                return true;
            }

            if (GetNumber(out _, out var lNumber))
            {
                rValue = new cExtendedValue.cNumber(lNumber);
                return true;
            }

            if (SkipByte(cASCII.LPAREN) &&
                ZProcessComplexValue(out rValue) &&
                SkipByte(cASCII.RPAREN)) return true;

            rValue = null;
            return false;
        }

        private bool ZProcessComplexValue(out cExtendedValue rValue)
        {
            string lString;
            cExtendedValue lValue;

            if (SkipByte(cASCII.LPAREN))
            {
                if (!ZProcessComplexValue(out lValue) || !SkipByte(cASCII.RPAREN))
                {
                    rValue = null;
                    return false;
                }
            }
            else if (GetANString(out lString)) lValue = new cExtendedValue.cAString(lString);
            else
            {
                rValue = null;
                return false;
            }

            if (!SkipByte(cASCII.SPACE))
            {
                rValue = lValue;
                return true;
            }

            List<cExtendedValue> lValues = new List<cExtendedValue>();

            lValues.Add(lValue);

            while (true)
            {
                if (!ZProcessComplexValue(out lValue)) { rValue = null; return false; }
                lValues.Add(lValue);
                if (!SkipByte(cASCII.SPACE)) break;
            }

            rValue = new cExtendedValue.cValues(lValues);
            return true;
        }
    }
}