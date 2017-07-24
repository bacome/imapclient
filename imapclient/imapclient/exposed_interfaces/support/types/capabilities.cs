using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    public class cCapabilities
    {
        private readonly Dictionary<string, bool> mDictionary = new Dictionary<string, bool>(StringComparer.InvariantCultureIgnoreCase);

        public cCapabilities() { }

        public bool Has(string pCapability) => mDictionary.ContainsKey(pCapability);

        public void Set(string pCapability)
        {
            if (pCapability == null) throw new ArgumentNullException(nameof(pCapability));
            if (pCapability.Length == 0) throw new ArgumentOutOfRangeException(nameof(pCapability));

            if (mDictionary.ContainsKey(pCapability)) return;

            if (!cCommandPartFactory.TryAsAtom(pCapability, out _)) throw new ArgumentOutOfRangeException(nameof(pCapability));

            mDictionary.Add(pCapability, true);
        }

        public int Count => mDictionary.Count;

        public List<string> AsUpperList()
        {
            List<string> lCapabilities = new List<string>();
            foreach (string lCapability in mDictionary.Keys) lCapabilities.Add(lCapability.ToUpperInvariant());
            return lCapabilities;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCapabilities));
            foreach (var lCapability in mDictionary.Keys) lBuilder.Append(lCapability);
            return lBuilder.ToString();
        }
    }
}
