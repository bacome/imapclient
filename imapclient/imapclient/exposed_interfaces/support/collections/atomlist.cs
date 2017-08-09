using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public class cUniqueIgnoreCaseStringList
    {
        private readonly Dictionary<string, bool> mDictionary = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        public cUniqueIgnoreCaseStringList() { }

        public bool Contains(string pAtom) => mDictionary.ContainsKey(pAtom);

        public void Add(string pAtom)
        {
            if (pAtom == null) throw new ArgumentNullException(nameof(pAtom));
            if (mDictionary.ContainsKey(pAtom)) return;
            mDictionary.Add(pAtom, true);
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
