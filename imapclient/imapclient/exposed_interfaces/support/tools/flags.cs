using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    // NOTE: this class does NO validation that the flags are mutually consistent
    //  it is designed to collect flags from server responses
    //  it is NOT designed as a class for use in the API as a way for the user to list a set of flags that they want set or unset
    //
    public class cFlags
    {
        private readonly Dictionary<string, bool> mDictionary = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        public cFlags() { }

        public bool Has(string pFlag) => mDictionary.ContainsKey(pFlag);

        public void Set(string pFlag)
        {
            if (pFlag == null) throw new ArgumentNullException(nameof(pFlag));
            if (pFlag.Length == 0) throw new ArgumentOutOfRangeException(nameof(pFlag));

            if (mDictionary.ContainsKey(pFlag)) return;

            if (pFlag == "\\*")
            {
                mDictionary.Add(pFlag, true);
                return;
            }

            if (pFlag[0] == '\\')
            {
                if (!cCommandPart.TryAsAtom(pFlag.Remove(0, 1), out _)) throw new ArgumentOutOfRangeException(nameof(pFlag));
            }
            else if (!cCommandPart.TryAsAtom(pFlag, out _)) throw new ArgumentOutOfRangeException(nameof(pFlag));

            mDictionary.Add(pFlag, true);
        }

        public int Count => mDictionary.Count;

        public List<string> ToSortedUpperList()
        {
            List<string> lFlags = new List<string>();
            foreach (string lFlag in mDictionary.Keys) lFlags.Add(lFlag.ToUpperInvariant());
            lFlags.Sort();
            return lFlags;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cFlags));
            foreach (var lCapability in mDictionary.Keys) lBuilder.Append(lCapability);
            return lBuilder.ToString();
        }
    }
}
