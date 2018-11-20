using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace work.bacome.imapinternals
{
    public abstract class cExtendedValue
    {
        public abstract bool Contains(string pAString, StringComparison pComparisonType);

        public class cNumber : cExtendedValue
        {
            public readonly uint Number;
            public cNumber(uint pNumber) { Number = pNumber; }
            public override bool Contains(string pAString, StringComparison pComparisonType) => false;
            public override string ToString() => $"{nameof(cNumber)}({Number})";
        }

        public class cSequenceSetEV : cExtendedValue
        {
            public readonly cSequenceSet SequenceSet;
            public cSequenceSetEV(cSequenceSet pSequenceSet) { SequenceSet = pSequenceSet; }
            public override bool Contains(string pAString, StringComparison pComparisonType) => false;
            public override string ToString() => $"{nameof(cSequenceSetEV)}({SequenceSet})";
        }

        public class cAString : cExtendedValue
        {
            public readonly string AString;

            public cAString(string pAString) { AString = pAString; }

            public override bool Contains(string pAString, StringComparison pComparisonType)
            {
                if (ReferenceEquals(AString, pAString)) return true; // to handle null == null
                if (AString == null || pAString == null) return false;
                return AString.Equals(pAString, pComparisonType);
            }

            public override string ToString() => $"{nameof(cAString)}({AString})";
        }

        public class cValues : cExtendedValue
        {
            public readonly ReadOnlyCollection<cExtendedValue> Values;

            public cValues(IList<cExtendedValue> pValues) { Values = new ReadOnlyCollection<cExtendedValue>(pValues); }

            public override bool Contains(string pAString, StringComparison pComparisonType) => Values.Any(lValue => lValue.Contains(pAString, pComparisonType));

            public override string ToString()
            {
                cListBuilder lBuilder = new cListBuilder(nameof(cValues));
                foreach (var lValue in Values) lBuilder.Append(lValue);
                return lBuilder.ToString();
            }
        }

        public class cValue : cExtendedValue
        {
            public readonly cExtendedValue Value;
            public cValue(cExtendedValue pValue) { Value = pValue; }
            public override bool Contains(string pAString, StringComparison pComparisonType) => false;
            public override string ToString() => $"{nameof(cValue)}({Value})";
        }
    }
}