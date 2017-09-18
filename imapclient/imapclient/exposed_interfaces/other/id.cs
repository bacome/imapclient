using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public interface iId : IReadOnlyDictionary<string, string>
    {
        string Name { get; }
        string Version { get; }
        string OS { get; }
        string OSVersion { get; }
        string Vendor { get; }
        string SupportURL { get; }
        string Address { get; }
        string Date { get; }
        string Command { get; }
        string Arguments { get; }
        string Environment { get; }
    }

    public abstract class cIdBase
    {
        protected const string kName = "name";
        protected const string kVersion = "version";
        protected const string kOS = "os";
        protected const string kOSVersion = "os-version";
        protected const string kVendor = "vendor";
        protected const string kSupportURL = "support-url";
        protected const string kAddress = "address";
        protected const string kDate = "date";
        protected const string kCommand = "command";
        protected const string kArguments = "arguments";
        protected const string kEnvironment = "environment";
    }

    public class cId : cIdBase, iId
    {
        protected readonly ReadOnlyDictionary<string, string> mDictionary;

        public cId(IDictionary<string, string> pDictionary)
        {
            mDictionary = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(pDictionary, StringComparer.InvariantCultureIgnoreCase));
        }

        public int Count => mDictionary.Count;

        public IEnumerable<string> Values => mDictionary.Values;
        public IEnumerable<string> Keys => mDictionary.Keys;

        public bool ContainsKey(string pKey) => mDictionary.ContainsKey(pKey);
        public bool TryGetValue(string pKey, out string rValue) => mDictionary.TryGetValue(pKey, out rValue);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => mDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.GetEnumerator();

        public string this[string pKey]  => mDictionary[pKey];

        private string ZGetValue(string pIdFieldName)
        {
            if (mDictionary.TryGetValue(pIdFieldName, out string lValue)) return lValue;
            return null;
        }

        public string Name => ZGetValue(kName);
        public string Version => ZGetValue(kVersion);
        public string OS => ZGetValue(kOS);
        public string OSVersion => ZGetValue(kOSVersion);
        public string Vendor => ZGetValue(kVendor);
        public string SupportURL => ZGetValue(kSupportURL);
        public string Address => ZGetValue(kAddress);
        public string Date => ZGetValue(kDate);
        public string Command => ZGetValue(kCommand);
        public string Arguments => ZGetValue(kArguments);
        public string Environment => ZGetValue(kEnvironment);

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cId));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }

    public class cClientIdUTF8 : cId
    {
        public cClientIdUTF8(IDictionary<string, string> pDictionary) : base(pDictionary)
        {
            if (pDictionary.Count < 1 || pDictionary.Count > 30) throw new ArgumentOutOfRangeException(nameof(pDictionary));

            foreach (var lEntry in pDictionary)
            {
                if (Encoding.UTF8.GetByteCount(lEntry.Key) > 30) throw new ArgumentOutOfRangeException(nameof(pDictionary));
                if (!cCommandPartFactory.TryAsUTF8String(lEntry.Key, out _)) throw new ArgumentOutOfRangeException(nameof(pDictionary));

                if (lEntry.Value != null)
                {
                    if (Encoding.UTF8.GetByteCount(lEntry.Value) > 1024) throw new ArgumentOutOfRangeException(nameof(pDictionary));
                    if (!cCommandPartFactory.TryAsUTF8String(lEntry.Value, out _)) throw new ArgumentOutOfRangeException(nameof(pDictionary));
                }
            }
        }
    }

    public class cClientId : cClientIdUTF8
    {
        public cClientId(IDictionary<string, string> pDictionary) : base(pDictionary)
        {
            foreach (var lEntry in pDictionary)
            {
                if (lEntry.Key.Length > 30) throw new ArgumentOutOfRangeException(nameof(pDictionary));
                if (!cCommandPartFactory.TryAsASCIIString(lEntry.Key, out _)) throw new ArgumentOutOfRangeException(nameof(pDictionary));

                if (lEntry.Value != null)
                {
                    if (lEntry.Value.Length > 1024) throw new ArgumentOutOfRangeException(nameof(pDictionary));
                    if (!cCommandPartFactory.TryAsASCIIString(lEntry.Value, out _)) throw new ArgumentOutOfRangeException(nameof(pDictionary));
                }
            }
        }
    }

    public class cIdDictionary : cIdBase, iId, IDictionary<string, string>
    {
        private readonly Dictionary<string, string> mDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public cIdDictionary(bool pDefault = true)
        {
            if (pDefault)
            {
                mDictionary[kName] = "work.bacome.imapclient";
                mDictionary[kVersion] = cIMAPClient.Version.ToString();

                try
                {
                    OperatingSystem lOS = System.Environment.OSVersion;
                    mDictionary[kOS] = lOS.Platform.ToString();
                    mDictionary[kOSVersion] = lOS.Version.ToString();
                }
                catch { }

                mDictionary[kVendor] = "bacome";
                mDictionary[kSupportURL] = @"http:\\bacome.work";
                mDictionary[kDate] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(cIMAPClient.ReleaseDate).Bytes);

                /* not safe: the command line could contain anything
                try
                {
                    string[] lCommand = System.Environment.GetCommandLineArgs();
                    mDictionary[kCommand] = lCommand[0];
                    if (lCommand.Length > 1) mDictionary[kArguments] = string.Join(", ", lCommand, 1, lCommand.Length - 1);
                }
                catch { } */

                mDictionary[kEnvironment] = System.Environment.Version.ToString();
            }
        }

        public int Count => mDictionary.Count;
        public bool IsReadOnly => false;

        public ICollection<string> Values => mDictionary.Values;
        public ICollection<string> Keys => mDictionary.Keys;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values => mDictionary.Values;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => mDictionary.Keys;

        public bool ContainsKey(string pKey) => mDictionary.ContainsKey(pKey);
        public bool TryGetValue(string pKey, out string rValue) => mDictionary.TryGetValue(pKey, out rValue);
        public void Add(string pKey, string pValue) => mDictionary.Add(pKey, pValue);
        public bool Remove(string pKey) => mDictionary.Remove(pKey);

        public bool Contains(KeyValuePair<string, string> pEntry) => ((ICollection<KeyValuePair<string, string>>)mDictionary).Contains(pEntry);
        public void Add(KeyValuePair<string, string> pEntry) => mDictionary.Add(pEntry.Key, pEntry.Value);
        public bool Remove(KeyValuePair<string, string> pEntry) => mDictionary.Remove(pEntry.Key);
        public void Clear() => mDictionary.Clear();
        public void CopyTo(KeyValuePair<string, string>[] pArray, int pIndex) => ((ICollection<KeyValuePair<string, string>>)mDictionary).CopyTo(pArray, pIndex);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => mDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.GetEnumerator();

        public string this[string pKey]
        {
            get => mDictionary[pKey];
            set => mDictionary[pKey] = value;
        }

        private string ZGetValue(string pIdFieldName)
        {
            if (mDictionary.TryGetValue(pIdFieldName, out string lValue)) return lValue;
            return null;
        }

        public string Name
        {
            get => ZGetValue(kName);
            set => mDictionary[kName] = value;
        }

        public string Version
        {
            get => ZGetValue(kVersion);
            set => mDictionary[kVersion] = value;
        }

        public string OS
        {
            get => ZGetValue(kOS);
            set => mDictionary[kOS] = value;
        }

        public string OSVersion
        {
            get => ZGetValue(kOSVersion);
            set => mDictionary[kOSVersion] = value;
        }

        public string Vendor
        {
            get => ZGetValue(kVendor);
            set => mDictionary[kVendor] = value;
        }

        public string SupportURL
        {
            get => ZGetValue(kSupportURL);
            set => mDictionary[kSupportURL] = value;
        }

        public string Address
        {
            get => ZGetValue(kAddress);
            set => mDictionary[kAddress] = value;
        }

        public string Date => ZGetValue(kDate);
        public void SetDate(DateTime pDate) => mDictionary[kDate] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(pDate).Bytes);

        public string Command
        {
            get => ZGetValue(kCommand);
            set => mDictionary[kCommand] = value;
        }

        public string Arguments
        {
            get => ZGetValue(kArguments);
            set => mDictionary[kArguments] = value;
        }

        public string Environment
        {
            get => ZGetValue(kEnvironment);
            set => mDictionary[kEnvironment] = value;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIdDictionary));
            foreach (var lFieldValue in mDictionary) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }
}