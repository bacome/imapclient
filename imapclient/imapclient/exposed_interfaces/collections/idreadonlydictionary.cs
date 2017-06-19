using System;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cIdReadOnlyDictionary : ReadOnlyDictionary<string, string>
    {
        public cIdReadOnlyDictionary(cIdDictionary pDictionary) : base(pDictionary) { }

        private string ZGetValue(string pIdFieldName)
        {
            if (TryGetValue(pIdFieldName, out string lValue)) return lValue;
            return null;
        }

        public string Name => ZGetValue(cIdFieldNames.Name);
        public string Version => ZGetValue(cIdFieldNames.Version);
        public string OS => ZGetValue(cIdFieldNames.OS);
        public string OSVersion => ZGetValue(cIdFieldNames.OSVersion);
        public string Vendor => ZGetValue(cIdFieldNames.Vendor);
        public string SupportURL => ZGetValue(cIdFieldNames.SupportURL);
        public string Address => ZGetValue(cIdFieldNames.Address);
        public string Date => ZGetValue(cIdFieldNames.Date);
        public string Command => ZGetValue(cIdFieldNames.Command);
        public string Arguments => ZGetValue(cIdFieldNames.Arguments);
        public string Environment => ZGetValue(cIdFieldNames.Environment);

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIdReadOnlyDictionary));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }
}
