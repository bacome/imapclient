using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP namespace name.
    /// </summary>
    public class cNamespaceName
    {
        // to extend with LANGUAGE translations

        /// <summary>
        /// The name prefix of the namespace. May be the empty string.
        /// </summary>
        public readonly string Prefix;

        /// <summary>
        /// The namespace hierarchy delimiter. May be null if there is no hierarchy.
        /// </summary>
        public readonly char? Delimiter;

        private cNamespaceName(string pPrefix, char? pDelimiter, bool pValid)
        {
            Prefix = pPrefix;
            Delimiter = pDelimiter;
        }

        public cNamespaceName(string pPrefix, char? pDelimiter)
        {
            if (pPrefix == null) throw new ArgumentNullException(nameof(pPrefix));
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) throw new ArgumentOutOfRangeException(nameof(pDelimiter));

            if (!cCommandPartFactory.Validation.TryAsListMailbox(pPrefix, pDelimiter, out _)) throw new ArgumentOutOfRangeException(nameof(pPrefix));

            Prefix = pPrefix;
            Delimiter = pDelimiter;
        }

        internal static bool TryConstruct(string pPrefix, char? pDelimiter, out cNamespaceName rResult)
        {
            if (pPrefix == null) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }

            if (!cCommandPartFactory.Validation.TryAsListMailbox(pPrefix, pDelimiter, out _)) { rResult = null; return false; }

            rResult = new cNamespaceName(pPrefix, pDelimiter, true);
            return true;
        }

        internal static bool TryConstruct(IList<byte> pEncodedPrefix, byte? pDelimiter, bool pUTF8Enabled, out cNamespaceName rResult)
        {
            if (pEncodedPrefix == null) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }

            if (!cTools.TryEncodedMailboxPathToString(pEncodedPrefix, pDelimiter, pUTF8Enabled, out var lPrefix)) { rResult = null; return false; }

            char? lDelimiter;
            if (pDelimiter == null) lDelimiter = null;
            else lDelimiter = (char)pDelimiter.Value;

            return TryConstruct(lPrefix, lDelimiter, out rResult);
        }

        public override string ToString() => $"{nameof(cNamespaceName)}({Prefix},{Delimiter})";
    }
}