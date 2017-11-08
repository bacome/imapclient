using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

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

    public static class cIdKey
    {
        public const string Name = "name";
        public const string Version = "version";
        public const string OS = "os";
        public const string OSVersion = "os-version";
        public const string Vendor = "vendor";
        public const string SupportURL = "support-url";
        public const string Address = "address";
        public const string Date = "date";
        public const string Command = "command";
        public const string Arguments = "arguments";
        public const string Environment = "environment";
    }

    public class cId : iId
    {
        // immutable (for passing in and out)

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

        public string Name => ZGetValue(cIdKey.Name);
        public string Version => ZGetValue(cIdKey.Version);
        public string OS => ZGetValue(cIdKey.OS);
        public string OSVersion => ZGetValue(cIdKey.OSVersion);
        public string Vendor => ZGetValue(cIdKey.Vendor);
        public string SupportURL => ZGetValue(cIdKey.SupportURL);
        public string Address => ZGetValue(cIdKey.Address);
        public string Date => ZGetValue(cIdKey.Date);
        public string Command => ZGetValue(cIdKey.Command);
        public string Arguments => ZGetValue(cIdKey.Arguments);
        public string Environment => ZGetValue(cIdKey.Environment);

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
            if (pDictionary.Count > 30) throw new ArgumentOutOfRangeException(nameof(pDictionary));

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

        public static implicit operator cClientIdUTF8(cIdDictionary pDictionary) => new cClientIdUTF8(pDictionary);
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

        public static implicit operator cClientId(cIdDictionary pDictionary) => new cClientId(pDictionary);
    }

    public class cIdDictionary : iId, IDictionary<string, string>
    {
        private readonly Dictionary<string, string> mDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public cIdDictionary(bool pDefault = true)
        {
            if (pDefault)
            {
                mDictionary[cIdKey.Name] = "work.bacome.imapclient";
                mDictionary[cIdKey.Version] = cIMAPClient.Version.ToString();

                try
                {
                    OperatingSystem lOS = System.Environment.OSVersion;
                    mDictionary[cIdKey.OS] = lOS.Platform.ToString();
                    mDictionary[cIdKey.OSVersion] = lOS.Version.ToString();
                }
                catch { }

                mDictionary[cIdKey.Vendor] = "bacome";
                mDictionary[cIdKey.SupportURL] = @"http:\\bacome.work";
                mDictionary[cIdKey.Date] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(cIMAPClient.ReleaseDate).Bytes);

                /* not safe: the command line could contain anything
                try
                {
                    string[] lCommand = System.Environment.GetCommandLineArgs();
                    mDictionary[kCommand] = lCommand[0];
                    if (lCommand.Length > 1) mDictionary[kArguments] = string.Join(", ", lCommand, 1, lCommand.Length - 1);
                }
                catch { } */

                mDictionary[cIdKey.Environment] = System.Environment.Version.ToString();
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
            get => ZGetValue(cIdKey.Name);
            set => mDictionary[cIdKey.Name] = value;
        }

        public string Version
        {
            get => ZGetValue(cIdKey.Version);
            set => mDictionary[cIdKey.Version] = value;
        }

        public string OS
        {
            get => ZGetValue(cIdKey.OS);
            set => mDictionary[cIdKey.OS] = value;
        }

        public string OSVersion
        {
            get => ZGetValue(cIdKey.OSVersion);
            set => mDictionary[cIdKey.OSVersion] = value;
        }

        public string Vendor
        {
            get => ZGetValue(cIdKey.Vendor);
            set => mDictionary[cIdKey.Vendor] = value;
        }

        public string SupportURL
        {
            get => ZGetValue(cIdKey.SupportURL);
            set => mDictionary[cIdKey.SupportURL] = value;
        }

        public string Address
        {
            get => ZGetValue(cIdKey.Address);
            set => mDictionary[cIdKey.Address] = value;
        }

        public string Date => ZGetValue(cIdKey.Date);
        public void SetDate(DateTime pDate) => mDictionary[cIdKey.Date] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(pDate).Bytes);

        public string Command
        {
            get => ZGetValue(cIdKey.Command);
            set => mDictionary[cIdKey.Command] = value;
        }

        public string Arguments
        {
            get => ZGetValue(cIdKey.Arguments);
            set => mDictionary[cIdKey.Arguments] = value;
        }

        public string Environment
        {
            get => ZGetValue(cIdKey.Environment);
            set => mDictionary[cIdKey.Environment] = value;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIdDictionary));
            foreach (var lFieldValue in mDictionary) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }
}