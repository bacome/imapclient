using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cClientId
    {
        public readonly cIdReadOnlyDictionary Dictionary;
        public readonly cIdReadOnlyDictionary ASCIIDictionary;

        ;?;

        public cClientId(ReadOnlyDictionary<string, string> pDictionary, ReadOnlyDictionary<string, string> pASCIIDictionary = null)
        {
            if (pDictionary == null) throw new ArgumentNullException(nameof(pDictionary));

            if (pDictionary.Count > 30) throw new ArgumentOutOfRangeException(nameof(pDictionary), "too many fields");
            foreach (string lField in pDictionary.Keys) if (Encoding.UTF8.GetByteCount(lField) > 30) throw new ArgumentOutOfRangeException(nameof(pDictionary), $"field name too long: {lField}");
            foreach (string lValue in pDictionary.Values) if (Encoding.UTF8.GetByteCount(lValue) > 1024) throw new ArgumentOutOfRangeException(nameof(pDictionary), $"value too long: {lValue}");
            Dictionary = new cIdReadOnlyDictionary(new cIdDictionary(pDictionary));

            ReadOnlyDictionary<string, string> lASCIIDictionary;

            if (pASCIIDictionary == null) lASCIIDictionary = pDictionary;
            else
            {
                if (pASCIIDictionary.Count > 30) throw new ArgumentOutOfRangeException(nameof(pASCIIDictionary), "too many fields");
                lASCIIDictionary = pASCIIDictionary;
            }

            cIdDictionary lDictionary = new cIdDictionary();

            foreach (var lFieldValuePair in lASCIIDictionary)
            {
                string lField = cTools.UTF8BytesToString(Encoding.ASCII.GetBytes(lFieldValuePair.Key));
                if (lField.Length > 30) throw new ArgumentOutOfRangeException(nameof(pASCIIDictionary), $"field name too long: {lField}");
                if (lDictionary.ContainsKey(lField)) throw new ArgumentOutOfRangeException(nameof(pASCIIDictionary), $"duplicate field name: {lField}");

                string lValue = cTools.UTF8BytesToString(Encoding.ASCII.GetBytes(lFieldValuePair.Value));
                if (lField.Length > 1024) throw new ArgumentOutOfRangeException(nameof(pASCIIDictionary), $"value too long: {lValue}");

                lDictionary.Add(lField, lValue);
            }

            ASCIIDictionary = new cIdReadOnlyDictionary(lDictionary);
        }

        public override string ToString()
        {
            if (ReferenceEquals(Dictionary, ASCIIDictionary)) return $"{nameof(cClientId)}({Dictionary})";
            return $"{nameof(cClientId)}({Dictionary},{ASCIIDictionary})";
        }
    }

    public class cIdDictionary : Dictionary<string, string>
    {
        private const string kName = "name";
        private const string kVersion = "version";
        private const string kOS = "os";
        private const string kOSVersion = "os-version";
        private const string kVendor = "vendor";
        private const string kSupportURL = "support-url";
        private const string kAddress = "address";
        private const string kDate = "date";
        private const string kCommand = "command";
        private const string kArguments = "arguments";
        private const string kEnvironment = "environment";

        ;?; // hide dictionary and validate as adding

        public cIdDictionary() : base(StringComparer.InvariantCultureIgnoreCase) { }
        public cIdDictionary(IDictionary<string, string> pDictionary) : base(pDictionary, StringComparer.InvariantCultureIgnoreCase) { }

        public static cIdDictionary CreateDefaultClientIdDictionary()
        {
            cIdDictionary lDictionary = new cIdDictionary();

            lDictionary.Name = "work.bacome.imapclient";
            lDictionary.Version = cIMAPClient.Version.ToString();

            try
            {
                OperatingSystem lOS = System.Environment.OSVersion;
                lDictionary.OS = lOS.Platform.ToString();
                lDictionary.OSVersion = lOS.Version.ToString();
            }
            catch { }

            lDictionary.Vendor = "bacome";
            lDictionary.SupportURL = @"http:\\bacome.work";
            lDictionary.SetDate(cIMAPClient.ReleaseDate);

            try
            {
                string[] lCommand = System.Environment.GetCommandLineArgs();
                lDictionary.Command = lCommand[0];
                if (lCommand.Length > 1) lDictionary.Arguments = string.Join(", ", lCommand, 1, lCommand.Length - 1);
            }
            catch { }

            lDictionary.Environment = System.Environment.Version.ToString();

            return lDictionary;
        }

        private string ZGetValue(string pIdFieldName)
        {
            if (TryGetValue(pIdFieldName, out string lValue)) return lValue;
            return null;
        }

        public string Name
        {
            get => ZGetValue(kName);
            set => this[kName] = value;
        }

        public string Version
        {
            get => ZGetValue(kVersion);
            set => this[kVersion] = value;
        }

        public string OS
        {
            get => ZGetValue(cIdFieldNames.OS);
            set => this[cIdFieldNames.OS] = value;
        }

        public string OSVersion
        {
            get => ZGetValue(cIdFieldNames.OSVersion);
            set => this[cIdFieldNames.OSVersion] = value;
        }

        public string Vendor
        {
            get => ZGetValue(cIdFieldNames.Vendor);
            set => this[cIdFieldNames.Vendor] = value;
        }

        public string SupportURL
        {
            get => ZGetValue(cIdFieldNames.SupportURL);
            set => this[cIdFieldNames.SupportURL] = value;
        }

        public string Address
        {
            get => ZGetValue(cIdFieldNames.Address);
            set => this[cIdFieldNames.Address] = value;
        }

        public string Date => ZGetValue(cIdFieldNames.Date);
        public void SetDate(DateTime pDate) => this[cIdFieldNames.Date] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(pDate).Bytes);

        public string Command
        {
            get => ZGetValue(cIdFieldNames.Command);
            set => this[cIdFieldNames.Command] = value;
        }

        public string Arguments
        {
            get => ZGetValue(cIdFieldNames.Arguments);
            set => this[cIdFieldNames.Arguments] = value;
        }

        public string Environment
        {
            get => ZGetValue(cIdFieldNames.Environment);
            set => this[cIdFieldNames.Environment] = value;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIdDictionary));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }

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