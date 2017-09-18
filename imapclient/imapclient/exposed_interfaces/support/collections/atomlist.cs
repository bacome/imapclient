using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public class cUniqueIgnoreCaseStringList
    {
        private readonly Dictionary<string, bool> mDictionary = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        public cUniqueIgnoreCaseStringList() { }

        public bool Contains(string pString) => mDictionary.ContainsKey(pString);

        public void Add(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            if (mDictionary.ContainsKey(pString)) return;
            mDictionary.Add(pString, true);
        }

        public int Count => mDictionary.Count;

        public ICollection<string> AsReadOnly() => mDictionary.Keys;

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cUniqueIgnoreCaseStringList));
            foreach (var lAtom in mDictionary.Keys) lBuilder.Append(lAtom);
            return lBuilder.ToString();
        }
    }
}
