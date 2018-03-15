using System;
using System.Collections.Generic;
using work.bacome.imapclient.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP namespace name.
    /// </summary>
    /// <seealso cref="cNamespace"/>
    /// <seealso cref="cNamespaces"/>
    public class cNamespaceName : IEquatable<cNamespaceName>
    {
        // to extend with LANGUAGE translations

        /// <summary>
        /// The name prefix of the namespace. May be the empty string.
        /// </summary>
        public readonly string Prefix;

        /// <summary>
        /// The hierarchy delimiter used in the namespace. May be <see langword="null"/>. 
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> if the server has no hierarchy in its names.
        /// </remarks>
        public readonly char? Delimiter;

        private cNamespaceName(string pPrefix, char? pDelimiter, bool pValid)
        {
            Prefix = pPrefix;
            Delimiter = pDelimiter;
        }

        internal cNamespaceName(string pPrefix, char? pDelimiter)
        {
            if (pPrefix == null) throw new ArgumentNullException(nameof(pPrefix));
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) throw new ArgumentOutOfRangeException(nameof(pDelimiter));

            if (!cCommandPartFactory.Validation.TryAsListMailbox(pPrefix, pDelimiter, out _)) throw new ArgumentOutOfRangeException(nameof(pPrefix));

            Prefix = pPrefix;
            Delimiter = pDelimiter;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cNamespaceName pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cNamespaceName;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + Prefix.GetHashCode();
                if (Delimiter != null) lHash = lHash * 23 + Delimiter.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cNamespaceName)}({Prefix},{Delimiter})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cNamespaceName pA, cNamespaceName pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Prefix == pB.Prefix && pA.Delimiter == pB.Delimiter;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cNamespaceName pA, cNamespaceName pB) => !(pA == pB);

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
    }
}