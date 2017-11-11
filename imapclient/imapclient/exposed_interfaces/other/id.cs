﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace work.bacome.imapclient
{
    /// <summary>
    /// An ID (RFC 2971) field/ value collection.
    /// </summary>
    public interface iId : IReadOnlyDictionary<string, string>
    {
        /**<summary>The name of the program.</summary>*/
        string Name { get; }
        /**<summary>The version number of the program.</summary>*/
        string Version { get; }
        /**<summary>The name of the operating system.</summary>*/
        string OS { get; }
        /**<summary>The version of the operating system.</summary>*/
        string OSVersion { get; }
        /**<summary>The vendor of the client/server.</summary>*/
        string Vendor { get; }
        /**<summary>The URL to contact for support.</summary>*/
        string SupportURL { get; }
        /**<summary>The postal address of contact/vendor.</summary>*/
        string Address { get; }
        /**<summary>The date program was released.</summary>*/
        string Date { get; }
        /**<summary>The command used to start the program.</summary>*/
        string Command { get; }
        /**<summary>The arguments supplied on the command line, if any.</summary>*/
        string Arguments { get; }
        /**<summary>A description of environment.</summary>*/
        string Environment { get; }
    }

    /// <summary>
    /// Contains ID (RFC 2971) field name named constants.
    /// </summary>
    public static class kIdFieldName
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

    /// <summary>
    /// An ID (RFC 2971) field/ value collection. See <see cref="cIMAPClient.ServerId"/>.
    /// </summary>
    public class cId : iId
    {
        // immutable (for passing in and out)

        protected readonly ReadOnlyDictionary<string, string> mDictionary;

        /// <summary>
        /// Construct from a field/ value dictionary. Field names are case insensitive.
        /// </summary>
        /// <param name="pDictionary">A field/ value dictionary.</param>
        public cId(IDictionary<string, string> pDictionary)
        {
            mDictionary = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(pDictionary, StringComparer.InvariantCultureIgnoreCase));
        }

        /**<summary>Gets the number of field/ value pairs in the collection.</summary>*/
        public int Count => mDictionary.Count;

        /**<summary>Gets the values that are in the collection.</summary>*/
        public IEnumerable<string> Values => mDictionary.Values;
        /**<summary>Gets the fields that are in the collection.</summary>*/
        public IEnumerable<string> Keys => mDictionary.Keys;

        /// <summary>
        /// Determines whether the collection contains a field.
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public bool ContainsKey(string pKey) => mDictionary.ContainsKey(pKey);

        /// <summary>
        /// Retrieves the field value.
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public bool TryGetValue(string pKey, out string rValue) => mDictionary.TryGetValue(pKey, out rValue);

        /**<summary>Returns an enumerator that iterates through the field/ values.</summary>*/
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => mDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.GetEnumerator();

        /**<summary>Retrieves the field value.</summary>*/
        public string this[string pKey] => mDictionary[pKey];

        private string ZGetValue(string pIdFieldName)
        {
            if (mDictionary.TryGetValue(pIdFieldName, out string lValue)) return lValue;
            return null;
        }

        /**<summary>The name of the program.</summary>*/
        public string Name => ZGetValue(kIdFieldName.Name);
        /**<summary>The version number of the program.</summary>*/
        public string Version => ZGetValue(kIdFieldName.Version);
        /**<summary>The name of the operating system.</summary>*/
        public string OS => ZGetValue(kIdFieldName.OS);
        /**<summary>The version of the operating system.</summary>*/
        public string OSVersion => ZGetValue(kIdFieldName.OSVersion);
        /**<summary>The vendor of the client/server.</summary>*/
        public string Vendor => ZGetValue(kIdFieldName.Vendor);
        /**<summary>The URL to contact for support.</summary>*/
        public string SupportURL => ZGetValue(kIdFieldName.SupportURL);
        /**<summary>The postal address of contact/vendor.</summary>*/
        public string Address => ZGetValue(kIdFieldName.Address);
        /**<summary>The date program was released.</summary>*/
        public string Date => ZGetValue(kIdFieldName.Date);
        /**<summary>The command used to start the program.</summary>*/
        public string Command => ZGetValue(kIdFieldName.Command);
        /**<summary>The arguments supplied on the command line, if any.</summary>*/
        public string Arguments => ZGetValue(kIdFieldName.Arguments);
        /**<summary>A description of environment.</summary>*/
        public string Environment => ZGetValue(kIdFieldName.Environment);

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cId));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// An ID (RFC 2971) field/ value collection. See <see cref="cIMAPClient.ClientIdUTF8"/>. This class defines an implicit conversion from <see cref="cIdDictionary"/> and enforces the limits of RFC 2971.
    /// </summary>
    /// <remarks>
    /// <para>The limits of RFC 2971 are;</para>
    /// <list type="bullet">
    /// <item><description>Field names no longer than 30 bytes.</description></item>
    /// <item><description>Values no longer than 1024 bytes.</description></item>
    /// <item><description>No more than 30 field/ value pairs.</description></item>
    /// </list>
    /// </remarks>
    public class cClientIdUTF8 : cId
    {
        /// <summary>
        /// Construct from a field/ value dictionary. Field names are case insensitive. RFC 2971 limits are enforced by the constructor: it will throw if there are violations.
        /// </summary>
        /// <param name="pDictionary">A field/ value dictionary.</param>
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

        /// <summary>
        /// Implicit conversion.
        /// </summary>
        /// <param name="pDictionary">The dictionary to get the field/ value data from.</param>
        /// <returns>The copied field/ values.</returns>
        public static implicit operator cClientIdUTF8(cIdDictionary pDictionary) => new cClientIdUTF8(pDictionary);
    }

    /// <summary>
    /// An ID (RFC 2971) field/ value collection. See <see cref="cIMAPClient.ClientId"/>. This class defines an implicit conversion from <see cref="cIdDictionary"/> and enforces the limits of RFC 2971.
    /// </summary>
    /// <remarks>
    /// <para>The limits of RFC 2971 are;</para>
    /// <list type="bullet">
    /// <item><description>Field names no longer than 30 bytes.</description></item>
    /// <item><description>Values no longer than 1024 bytes.</description></item>
    /// <item><description>No more than 30 field/ value pairs.</description></item>
    /// </list>
    /// </remarks>
    public class cClientId : cClientIdUTF8
    {
        /// <summary>
        /// Construct from a field/ value dictionary. Field names are case insensitive. RFC 2971 limits are enforced by the constructor: it will throw if there are violations.
        /// </summary>
        /// <param name="pDictionary">A field/ value dictionary.</param>
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

        /// <summary>
        /// Implicit conversion.
        /// </summary>
        /// <param name="pDictionary">The dictionary to get the field/ value data from.</param>
        /// <returns>The copied field/ values.</returns>
        public static implicit operator cClientId(cIdDictionary pDictionary) => new cClientId(pDictionary);
    }

    /// <summary>
    /// An ID (RFC 2971) field/ value dictionary. See <see cref="cClientId"/> and <see cref="cClientIdUTF8"/>. Note that field names are case insensitive.
    /// </summary>
    public class cIdDictionary : iId, IDictionary<string, string>
    {
        private readonly Dictionary<string, string> mDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Construct an empty or a default dictionary. A default dictionary contains details about the library.
        /// </summary>
        /// <param name="pDefault">Indicates if a default or an empty dictionary should be constructed.</param>
        public cIdDictionary(bool pDefault = true)
        {
            if (pDefault)
            {
                mDictionary[kIdFieldName.Name] = "work.bacome.imapclient";
                mDictionary[kIdFieldName.Version] = cIMAPClient.Version.ToString();

                try
                {
                    OperatingSystem lOS = System.Environment.OSVersion;
                    mDictionary[kIdFieldName.OS] = lOS.Platform.ToString();
                    mDictionary[kIdFieldName.OSVersion] = lOS.Version.ToString();
                }
                catch { }

                mDictionary[kIdFieldName.Vendor] = "bacome";
                mDictionary[kIdFieldName.SupportURL] = @"http:\\bacome.work";
                mDictionary[kIdFieldName.Date] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(cIMAPClient.ReleaseDate).Bytes);

                /* not safe: the command line could contain anything
                try
                {
                    string[] lCommand = System.Environment.GetCommandLineArgs();
                    mDictionary[kCommand] = lCommand[0];
                    if (lCommand.Length > 1) mDictionary[kArguments] = string.Join(", ", lCommand, 1, lCommand.Length - 1);
                }
                catch { } */

                mDictionary[kIdFieldName.Environment] = System.Environment.Version.ToString();
            }
        }

        /**<summary>Gets the number of field/ value pairs in the dictionary.</summary>*/
        public int Count => mDictionary.Count;
        /**<summary>false</summary>*/
        public bool IsReadOnly => false;

        /**<summary>Gets the values that are in the dictionary.</summary>*/
        public ICollection<string> Values => mDictionary.Values;
        /**<summary>Gets the fields that are in the dictionary.</summary>*/
        public ICollection<string> Keys => mDictionary.Keys;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Values => mDictionary.Values;
        IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => mDictionary.Keys;

        /// <summary>
        /// Determines whether the dictionary contains a field.
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public bool ContainsKey(string pKey) => mDictionary.ContainsKey(pKey);

        /// <summary>
        /// Retrieves the field value.
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public bool TryGetValue(string pKey, out string rValue) => mDictionary.TryGetValue(pKey, out rValue);

        /// <summary>
        /// Adds the specified field/ value to the dictionary.
        /// </summary>
        /// <param name="pKey"></param>
        /// <param name="pValue"></param>
        public void Add(string pKey, string pValue) => mDictionary.Add(pKey, pValue);

        /// <summary>
        /// Removes the field from the dictionary.
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public bool Remove(string pKey) => mDictionary.Remove(pKey);

        /// <summary>
        /// Determines whether the dictionary contains a field/ value pair.
        /// </summary>
        /// <param name="pEntry"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<string, string> pEntry) => ((ICollection<KeyValuePair<string, string>>)mDictionary).Contains(pEntry);

        /// <summary>
        /// Adds the specified field/ value to the dictionary.
        /// </summary>
        /// <param name="pEntry"></param>
        public void Add(KeyValuePair<string, string> pEntry) => mDictionary.Add(pEntry.Key, pEntry.Value);

        /// <summary>
        /// Removes the field/ value pair from the dictionary.
        /// </summary>
        /// <param name="pEntry"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<string, string> pEntry) => mDictionary.Remove(pEntry.Key);

        /// <summary>
        /// Removes all field/ value pairs from the dictionary.
        /// </summary>
        public void Clear() => mDictionary.Clear();

        /// <summary>
        /// Copies the field/ value pairs to an array.
        /// </summary>
        /// <param name="pArray"></param>
        /// <param name="pIndex"></param>
        public void CopyTo(KeyValuePair<string, string>[] pArray, int pIndex) => ((ICollection<KeyValuePair<string, string>>)mDictionary).CopyTo(pArray, pIndex);

        /**<summary>Returns an enumerator that iterates through the field/ values.</summary>*/
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => mDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.GetEnumerator();

        /**<summary>Retrieves the field value.</summary>*/
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

        /**<summary>The name of the program.</summary>*/
        public string Name
        {
            get => ZGetValue(kIdFieldName.Name);
            set => mDictionary[kIdFieldName.Name] = value;
        }

        /**<summary>The version number of the program.</summary>*/
        public string Version
        {
            get => ZGetValue(kIdFieldName.Version);
            set => mDictionary[kIdFieldName.Version] = value;
        }

        /**<summary>The name of the operating system.</summary>*/
        public string OS
        {
            get => ZGetValue(kIdFieldName.OS);
            set => mDictionary[kIdFieldName.OS] = value;
        }

        /**<summary>The version of the operating system.</summary>*/
        public string OSVersion
        {
            get => ZGetValue(kIdFieldName.OSVersion);
            set => mDictionary[kIdFieldName.OSVersion] = value;
        }

        /**<summary>The vendor of the client/server.</summary>*/
        public string Vendor
        {
            get => ZGetValue(kIdFieldName.Vendor);
            set => mDictionary[kIdFieldName.Vendor] = value;
        }

        /**<summary>The URL to contact for support.</summary>*/
        public string SupportURL
        {
            get => ZGetValue(kIdFieldName.SupportURL);
            set => mDictionary[kIdFieldName.SupportURL] = value;
        }

        /**<summary>The postal address of contact/vendor.</summary>*/
        public string Address
        {
            get => ZGetValue(kIdFieldName.Address);
            set => mDictionary[kIdFieldName.Address] = value;
        }

        /**<summary>The date program was released.</summary>*/
        public string Date => ZGetValue(kIdFieldName.Date);

        /// <summary>
        /// Set the <see cref="Date"/>. This method converts the supplied date to RFC 3501 date format.
        /// </summary>
        /// <param name="pDate">The value to set the <see cref="Date"/> to.</param>
        public void SetDate(DateTime pDate) => mDictionary[kIdFieldName.Date] = cTools.UTF8BytesToString(cCommandPartFactory.AsDate(pDate).Bytes);

        /**<summary>The command used to start the program.</summary>*/
        public string Command
        {
            get => ZGetValue(kIdFieldName.Command);
            set => mDictionary[kIdFieldName.Command] = value;
        }

        /**<summary>The arguments supplied on the command line, if any.</summary>*/
        public string Arguments
        {
            get => ZGetValue(kIdFieldName.Arguments);
            set => mDictionary[kIdFieldName.Arguments] = value;
        }

        /**<summary>A description of environment.</summary>*/
        public string Environment
        {
            get => ZGetValue(kIdFieldName.Environment);
            set => mDictionary[kIdFieldName.Environment] = value;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cIdDictionary));
            foreach (var lFieldValue in mDictionary) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }
}