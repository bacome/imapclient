using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
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
            get => ZGetValue(kOS);
            set => this[kOS] = value;
        }

        public string OSVersion
        {
            get => ZGetValue(kOSVersion);
            set => this[kOSVersion] = value;
        }

        public string Vendor
        {
            get => ZGetValue(kVendor);
            set => this[kVendor] = value;
        }

        public string SupportURL
        {
            get => ZGetValue(kSupportURL);
            set => this[kSupportURL] = value;
        }

        public string Address
        {
            get => ZGetValue(kAddress);
            set => this[kAddress] = value;
        }

        public string Date => ZGetValue(kDate);
        public void SetDate(DateTime pDate) => this[kDate] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(pDate).Bytes);

        public string Command
        {
            get => ZGetValue(kCommand);
            set => this[kCommand] = value;
        }

        public string Arguments
        {
            get => ZGetValue(kArguments);
            set => this[kArguments] = value;
        }

        public string Environment
        {
            get => ZGetValue(kEnvironment);
            set => this[kEnvironment] = value;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIdDictionary));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }
}