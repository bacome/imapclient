using System;
using System.Collections.Generic;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP namespace name.
    /// </summary>
    /// <remarks>
    /// IMAP namespace names have few grammatical restrictions, but may not include the null character.
    /// IMAP hierarchy delimitiers have few grammatical restrictions, but must be ASCII, and not NUL, CR or LF.
    /// Be careful to correctly specify the hierarchy delimitier, it is used in preparing the namespace name for sending to the server.
    /// </remarks>
    /// <seealso cref="cNamespace"/>
    /// <seealso cref="cNamespaces"/>
    public class cNamespaceName
    {
        // to extend with LANGUAGE translations

        /// <summary>
        /// The name prefix of the namespace. May be the empty string.
        /// </summary>
        public readonly string Prefix;

        /// <summary>
        /// The namespace hierarchy delimiter. May be <see langword="null"/>. 
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

        /// <summary>
        /// Initialises a new instance. Will throw if the parameters provided are not valid.
        /// </summary>
        /// <param name="pPrefix">The name prefix of the namespace. May be the empty string, may not be <see langword="null"/></param>
        /// <param name="pDelimiter">The namespace hierarchy delimiter. <see langword="null"/> if the server has no hierarchy in its names.</param>
        /// <inheritdoc cref="cNamespaceName" select="remarks"/>
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cNamespaceName)}({Prefix},{Delimiter})";
    }
}