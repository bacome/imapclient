using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an ID (RFC 2971) field/ value dictionary.
    /// </summary>
    public interface iIMAPId : IReadOnlyDictionary<string, string>
    {
        /**<summary>Gets the name of the program or <see langword="null"/> if it isn't specified.</summary>*/
        string Name { get; }
        /**<summary>Gets the version number of the program or <see langword="null"/> if it isn't specified.</summary>*/
        string Version { get; }
        /**<summary>Gets the name of the operating system or <see langword="null"/> if it isn't specified.</summary>*/
        string OS { get; }
        /**<summary>Gets the version of the operating system or <see langword="null"/> if it isn't specified.</summary>*/
        string OSVersion { get; }
        /**<summary>Gets the vendor of the client/server or <see langword="null"/> if it isn't specified.</summary>*/
        string Vendor { get; }
        /**<summary>Gets the URL to contact for support or <see langword="null"/> if it isn't specified.</summary>*/
        string SupportURL { get; }
        /**<summary>Gets the postal address of contact/vendor or <see langword="null"/> if it isn't specified.</summary>*/
        string Address { get; }
        /**<summary>Gets the date program was released or <see langword="null"/> if it isn't specified.</summary>*/
        string Date { get; }
        /**<summary>Gets the command used to start the program or <see langword="null"/> if it isn't specified.</summary>*/
        string Command { get; }
        /**<summary>Gets the arguments supplied on the command line or <see langword="null"/> if it isn't specified.</summary>*/
        string Arguments { get; }
        /**<summary>Gets the description of the environment or <see langword="null"/> if it isn't specified.</summary>*/
        string Environment { get; }
    }

    /// <summary>
    /// Contains ID (RFC 2971) field name constants.
    /// </summary>
    public static class kIMAPIdFieldName
    {
        /**<summary>name</summary>*/
        public const string Name = "name";
        /**<summary>version</summary>*/
        public const string Version = "version";
        /**<summary>os</summary>*/
        public const string OS = "os";
        /**<summary>os-version</summary>*/
        public const string OSVersion = "os-version";
        /**<summary>vendor</summary>*/
        public const string Vendor = "vendor";
        /**<summary>support-url</summary>*/
        public const string SupportURL = "support-url";
        /**<summary>address</summary>*/
        public const string Address = "address";
        /**<summary>date</summary>*/
        public const string Date = "date";
        /**<summary>command</summary>*/
        public const string Command = "command";
        /**<summary>arguments</summary>*/
        public const string Arguments = "arguments";
        /**<summary>environment</summary>*/
        public const string Environment = "environment";
    }

    /// <summary>
    /// An immutable ID (RFC 2971) field/ value dictionary.
    /// </summary>
    /// <remarks>
    /// ID field names are case insensitive.
    /// </remarks>
    public class cIMAPId : iIMAPId
    {
        // immutable (for passing in and out)

        /**<summary></summary>*/
        protected readonly ReadOnlyDictionary<string, string> mDictionary;

        /// <summary>
        /// Initialises a new instance, copying the specified dictionary.
        /// </summary>
        /// <param name="pDictionary"></param>
        public cIMAPId(IDictionary<string, string> pDictionary)
        {
            mDictionary = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(pDictionary, StringComparer.InvariantCultureIgnoreCase));
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mDictionary.Count;

        /**<summary>Gets the values that are in the dictionary.</summary>*/
        public IEnumerable<string> Values => mDictionary.Values;
        /**<summary>Gets the fields that are in the dictionary.</summary>*/
        public IEnumerable<string> Keys => mDictionary.Keys;

        /// <summary>
        /// Determines whether the dictionary contains the specified field (case insensitive).
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public bool ContainsKey(string pKey) => mDictionary.ContainsKey(pKey);

        /// <summary>
        /// Gets the specified field's value (case insensitive).
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public bool TryGetValue(string pKey, out string rValue) => mDictionary.TryGetValue(pKey, out rValue);

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => mDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.GetEnumerator();

        /// <summary>
        /// Gets the specified field's value (case insensitive).
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public string this[string pKey] => mDictionary[pKey];

        private string ZGetValue(string pIdFieldName)
        {
            if (mDictionary.TryGetValue(pIdFieldName, out string lValue)) return lValue;
            return null;
        }

        /**<summary>Gets the name of the program or <see langword="null"/> if it isn't specified.</summary>*/
        public string Name => ZGetValue(kIMAPIdFieldName.Name);
        /**<summary>Gets the version number of the program or <see langword="null"/> if it isn't specified.</summary>*/
        public string Version => ZGetValue(kIMAPIdFieldName.Version);
        /**<summary>Gets the name of the operating system or <see langword="null"/> if it isn't specified.</summary>*/
        public string OS => ZGetValue(kIMAPIdFieldName.OS);
        /**<summary>Gets the version of the operating system or <see langword="null"/> if it isn't specified.</summary>*/
        public string OSVersion => ZGetValue(kIMAPIdFieldName.OSVersion);
        /**<summary>Gets the vendor of the client/server or <see langword="null"/> if it isn't specified.</summary>*/
        public string Vendor => ZGetValue(kIMAPIdFieldName.Vendor);
        /**<summary>Gets the URL to contact for support or <see langword="null"/> if it isn't specified.</summary>*/
        public string SupportURL => ZGetValue(kIMAPIdFieldName.SupportURL);
        /**<summary>Gets the postal address of contact/vendor or <see langword="null"/> if it isn't specified.</summary>*/
        public string Address => ZGetValue(kIMAPIdFieldName.Address);
        /**<summary>Gets the date program was released or <see langword="null"/> if it isn't specified.</summary>*/
        public string Date => ZGetValue(kIMAPIdFieldName.Date);
        /**<summary>Gets the command used to start the program or <see langword="null"/> if it isn't specified.</summary>*/
        public string Command => ZGetValue(kIMAPIdFieldName.Command);
        /**<summary>Gets the arguments supplied on the command line or <see langword="null"/> if it isn't specified.</summary>*/
        public string Arguments => ZGetValue(kIMAPIdFieldName.Arguments);
        /**<summary>Gets the description of environment or <see langword="null"/> if it isn't specified.</summary>*/
        public string Environment => ZGetValue(kIMAPIdFieldName.Environment);

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIMAPId));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// An immutable ID (RFC 2971) field/ value dictionary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ID field names are case insensitive.
    /// </para>
    /// <para>
    /// This class enforces the limits of RFC 2971;
    /// <list type="bullet">
    /// <item>Field names no longer than 30 bytes.</item>
    /// <item>Values no longer than 1024 bytes.</item>
    /// <item>No more than 30 field/ value pairs.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class cIMAPClientIdUTF8 : cIMAPId
    {
        /// <summary>
        /// Initialises a new instance, copying the specified dictionary. 
        /// </summary>
        /// <param name="pDictionary"></param>
        /// <inheritdoc cref="cIMAPClientIdUTF8" select="remarks"/>
        public cIMAPClientIdUTF8(IDictionary<string, string> pDictionary) : base(pDictionary)
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

        /// <summary>
        /// Returns a new instance, copying the specified dictionary.
        /// </summary>
        /// <param name="pDictionary"></param>
        /// <returns></returns>
        /// <inheritdoc cref="cIMAPClientIdUTF8" select="remarks"/>
        public static implicit operator cIMAPClientIdUTF8(cIMAPIdDictionary pDictionary) => new cIMAPClientIdUTF8(pDictionary);
    }

    /// <inheritdoc cref="cIMAPClientIdUTF8" select="summary|remarks"/>
    public class cIMAPClientId : cIMAPClientIdUTF8
    {
        /// <inheritdoc cref="cIMAPClientIdUTF8(IDictionary{string, string})"/>
        public cIMAPClientId(IDictionary<string, string> pDictionary) : base(pDictionary)
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

        /// <summary>
        /// Returns a new instance, copying the specified dictionary.
        /// </summary>
        /// <param name="pDictionary"></param>
        /// <returns></returns>
        /// <inheritdoc cref="cIMAPClientIdUTF8" select="remarks"/>
        public static implicit operator cIMAPClientId(cIMAPIdDictionary pDictionary) => new cIMAPClientId(pDictionary);
    }

    /// <summary>
    /// An ID (RFC 2971) field/ value dictionary.
    /// </summary>
    /// <remarks>
    /// ID field names are case insensitive.
    /// </remarks>
    public class cIMAPIdDictionary : iIMAPId, IDictionary<string, string>
    {
        private readonly Dictionary<string, string> mDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Initialises a new instance, empty or with default values.
        /// </summary>
        /// <param name="pDefault">Indicates whether the instance should be initialised with default values.</param>
        /// <remarks>
        /// A default dictionary contains details about the library.
        /// </remarks>
        public cIMAPIdDictionary(bool pDefault = true)
        {
            if (pDefault)
            {
                mDictionary[kIMAPIdFieldName.Name] = "work.bacome.imapclient";
                mDictionary[kIMAPIdFieldName.Version] = cIMAPClient.Version.ToString();

                try
                {
                    OperatingSystem lOS = System.Environment.OSVersion;
                    mDictionary[kIMAPIdFieldName.OS] = lOS.Platform.ToString();
                    mDictionary[kIMAPIdFieldName.OSVersion] = lOS.Version.ToString();
                }
                catch { }

                mDictionary[kIMAPIdFieldName.Vendor] = "bacome";
                mDictionary[kIMAPIdFieldName.SupportURL] = @"http:\\bacome.work";
                mDictionary[kIMAPIdFieldName.Date] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(cIMAPClient.ReleaseDate).Bytes);

                /* not safe: the command line could contain anything
                try
                {
                    string[] lCommand = System.Environment.GetCommandLineArgs();
                    mDictionary[kCommand] = lCommand[0];
                    if (lCommand.Length > 1) mDictionary[kArguments] = string.Join(", ", lCommand, 1, lCommand.Length - 1);
                }
                catch { } */

                mDictionary[kIMAPIdFieldName.Environment] = System.Environment.Version.ToString();
            }
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mDictionary.Count;
        /**<summary>Gets the value <see langword="false"/>.</summary>*/
        public bool IsReadOnly => false;

        /**<summary>Gets the values that are in the dictionary.</summary>*/
        public ICollection<string> Values => mDictionary.Values;
        /**<summary>Gets the fields that are in the dictionary.</summary>*/
        public ICollection<string> Keys => mDictionary.Keys;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values => mDictionary.Values;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => mDictionary.Keys;

        /// <summary>
        /// Determines whether the dictionary contains the specified field (case insensitive).
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public bool ContainsKey(string pKey) => mDictionary.ContainsKey(pKey);

        /// <summary>
        /// Gets the specifed field's value (case insensitive).
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public bool TryGetValue(string pKey, out string rValue) => mDictionary.TryGetValue(pKey, out rValue);

        /// <summary>
        /// Adds the specified field (case insensitive) and value to the dictionary.
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="pValue"></param>
        public void Add(string pKey, string pValue) => mDictionary.Add(pKey, pValue);

        /// <summary>
        /// Removes the specified field from the dictionary (case insensitive).
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public bool Remove(string pKey) => mDictionary.Remove(pKey);

        /// <summary>
        /// Determines whether the dictionary contains the specified field/ value pair.
        /// </summary>
        /// <param name="pEntry"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<string, string> pEntry) => ((ICollection<KeyValuePair<string, string>>)mDictionary).Contains(pEntry);

        /// <summary>
        /// Adds the specified field (case insensitive) and value to the dictionary.
        /// </summary>
        /// <param name="pEntry"></param>
        public void Add(KeyValuePair<string, string> pEntry) => mDictionary.Add(pEntry.Key, pEntry.Value);

        /// <summary>
        /// Removes the specified field/ value pair from the dictionary.
        /// </summary>
        /// <param name="pEntry"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<string, string> pEntry) => mDictionary.Remove(pEntry.Key);

        /// <summary>
        /// Removes all field/ value pairs from the dictionary.
        /// </summary>
        public void Clear() => mDictionary.Clear();

        /// <summary>
        /// Copies the field/ value pairs in the dictionary to an array.
        /// </summary>
        /// <param name="pArray"></param>
        /// <param name="pIndex"></param>
        public void CopyTo(KeyValuePair<string, string>[] pArray, int pIndex) => ((ICollection<KeyValuePair<string, string>>)mDictionary).CopyTo(pArray, pIndex);

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => mDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.GetEnumerator();

        /// <summary>
        /// Gets the specifed field's value (case insensitive).
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
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

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the name of the program.</summary>*/
        public string Name
        {
            get => ZGetValue(kIMAPIdFieldName.Name);
            set => mDictionary[kIMAPIdFieldName.Name] = value;
        }

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the version number of the program.</summary>*/
        public string Version
        {
            get => ZGetValue(kIMAPIdFieldName.Version);
            set => mDictionary[kIMAPIdFieldName.Version] = value;
        }

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the name of the operating system.</summary>*/
        public string OS
        {
            get => ZGetValue(kIMAPIdFieldName.OS);
            set => mDictionary[kIMAPIdFieldName.OS] = value;
        }

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the version of the operating system.</summary>*/
        public string OSVersion
        {
            get => ZGetValue(kIMAPIdFieldName.OSVersion);
            set => mDictionary[kIMAPIdFieldName.OSVersion] = value;
        }

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the vendor of the client/server.</summary>*/
        public string Vendor
        {
            get => ZGetValue(kIMAPIdFieldName.Vendor);
            set => mDictionary[kIMAPIdFieldName.Vendor] = value;
        }

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the URL to contact for support.</summary>*/
        public string SupportURL
        {
            get => ZGetValue(kIMAPIdFieldName.SupportURL);
            set => mDictionary[kIMAPIdFieldName.SupportURL] = value;
        }

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the postal address of contact/vendor.</summary>*/
        public string Address
        {
            get => ZGetValue(kIMAPIdFieldName.Address);
            set => mDictionary[kIMAPIdFieldName.Address] = value;
        }

        /**<summary>Gets the date program was released or <see langword="null"/> if it isn't specified.</summary>*/
        public string Date => ZGetValue(kIMAPIdFieldName.Date);

        /// <summary>
        /// Sets <see cref="Date"/>. This method converts the supplied date to RFC 3501 date format.
        /// </summary>
        /// <param name="pDate"></param>
        public void SetDate(DateTime pDate) => mDictionary[kIMAPIdFieldName.Date] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(pDate).Bytes);

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the command used to start the program.</summary>*/
        public string Command
        {
            get => ZGetValue(kIMAPIdFieldName.Command);
            set => mDictionary[kIMAPIdFieldName.Command] = value;
        }

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the arguments supplied on the command line, if any.</summary>*/
        public string Arguments
        {
            get => ZGetValue(kIMAPIdFieldName.Arguments);
            set => mDictionary[kIMAPIdFieldName.Arguments] = value;
        }

        /**<summary>Gets (<see langword="null"/> if it isn't specified) and sets the description of the environment.</summary>*/
        public string Environment
        {
            get => ZGetValue(kIMAPIdFieldName.Environment);
            set => mDictionary[kIMAPIdFieldName.Environment] = value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIMAPIdDictionary));
            foreach (var lFieldValue in mDictionary) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }
}