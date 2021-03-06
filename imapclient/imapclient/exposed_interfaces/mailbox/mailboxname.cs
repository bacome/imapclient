﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using work.bacome.imapclient.apidocumentation;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP mailbox name.
    /// </summary>
    /// <remarks>
    /// IMAP mailbox names have few grammatical restrictions, but may not include the NUL character.
    /// IMAP hierarchy delimitiers have few grammatical restrictions, but must be ASCII, and not NUL, CR or LF.
    /// Be careful to correctly specify the hierarchy delimiter, it is used in preparing the mailbox name for sending to the server.
    /// </remarks>
    /// <seealso cref="cIMAPClient.Mailbox(cMailboxName)"/>
    /// <seealso cref="cIMAPClient.Create(cMailboxName, bool)"/>
    /// <seealso cref="iMailboxHandle.MailboxName"/>
    public class cMailboxName : IComparable<cMailboxName>, IEquatable<cMailboxName>
    {
        internal const string InboxString = "INBOX";
        internal static readonly ReadOnlyCollection<byte> InboxBytes = new cBytes(InboxString);

        /// <summary>
        /// The mailbox path including the full hierarchy.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// The hierarchy delimiter used in this mailbox name. May be <see langword="null"/>. 
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> if the server has no hierarchy in its names.
        /// </remarks>
        public readonly char? Delimiter;

        private cMailboxName(string pPath, char? pDelimiter, bool pValid)
        {
            Path = pPath;
            Delimiter = pDelimiter;
        }

        /// <summary>
        /// Initialises a new instance with the specified path and hierarchy delimiter. Will throw if the arguments provided are not valid.
        /// </summary>
        /// <param name="pPath"></param>
        /// <param name="pDelimiter"></param>
        /// <inheritdoc cref="cMailboxName" select="remarks"/>
        public cMailboxName(string pPath, char? pDelimiter)
        {
            if (string.IsNullOrEmpty(pPath)) throw new ArgumentNullException(nameof(pPath));
            if (pDelimiter != null &&  !cTools.IsValidDelimiter(pDelimiter.Value)) throw new ArgumentOutOfRangeException(nameof(pDelimiter));

            if (pPath.Equals(InboxString, StringComparison.InvariantCultureIgnoreCase))
            {
                Path = InboxString;
                Delimiter = pDelimiter;
                return;
            }

            if (pPath[pPath.Length - 1] == pDelimiter) throw new ArgumentOutOfRangeException(nameof(pPath));
                
            if (!cCommandPartFactory.Validation.TryAsListMailbox(pPath, pDelimiter, out _)) throw new ArgumentOutOfRangeException(nameof(pPath));

            Path = pPath;
            Delimiter = pDelimiter;
        }

        /// <summary>
        /// Gets the path of the parent mailbox. Will be <see langword="null"/> if there is no parent mailbox.
        /// </summary>
        public string ParentPath
        {
            get
            {
                if (Delimiter == null) return null;
                int lParentPathEnd = Path.LastIndexOf(Delimiter.Value);
                if (lParentPathEnd == -1) return null;
                return Path.Substring(0, lParentPathEnd);
            }
        }

        /// <summary>
        /// Gets the name of the mailbox. As compared to <see cref="Path"/> this does not include the hierarchy.
        /// </summary>
        public string Name
        {
            get
            {
                if (Delimiter == null) return Path;
                int lParentPathEnd = Path.LastIndexOf(Delimiter.Value);
                if (lParentPathEnd == -1) return Path;
                return Path.Substring(lParentPathEnd + 1);
            }
        }

        /// <summary>
        /// Indicates whether this is 'INBOX'.
        /// </summary>
        public bool IsInbox => ReferenceEquals(Path, InboxString);

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo"/>
        public int CompareTo(cMailboxName pOther)
        {
            if (pOther == null) return 1;

            var lCompareTo = Path.CompareTo(pOther.Path);

            if (lCompareTo != 0) return lCompareTo;

            // should never get here

            if (Delimiter == null)
            {
                if (pOther.Delimiter == null) return 0;
                return -1;
            }

            if (pOther.Delimiter == null) return 1;

            return Delimiter.Value.CompareTo(pOther.Delimiter.Value);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals"/>
        public bool Equals(cMailboxName pOther) => this == pOther;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cMailboxName;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + Path.GetHashCode();
                if (Delimiter != null) lHash = lHash * 23 + Delimiter.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMailboxName)}({Path},{Delimiter})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMailboxName pA, cMailboxName pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Path == pB.Path && pA.Delimiter == pB.Delimiter;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMailboxName pA, cMailboxName pB) => !(pA == pB);

        internal static bool TryConstruct(string pPath, char? pDelimiter, out cMailboxName rResult)
        {
            if (string.IsNullOrEmpty(pPath)) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }

            if (pPath.Equals(InboxString, StringComparison.InvariantCultureIgnoreCase))
            {
                rResult = new cMailboxName(InboxString, pDelimiter, true);
                return true;
            }

            if (pPath[pPath.Length - 1] == pDelimiter) { rResult = null; return false; }

            if (!cCommandPartFactory.Validation.TryAsListMailbox(pPath, pDelimiter, out _)) { rResult = null; return false; }

            rResult = new cMailboxName(pPath, pDelimiter, true);
            return true;
        }

        internal static bool TryConstruct(IList<byte> pEncodedMailboxPath, byte? pDelimiter, bool pUTF8Enabled, out cMailboxName rResult)
        {
            if (pEncodedMailboxPath == null || pEncodedMailboxPath.Count == 0) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }

            string lPath;

            if (cASCII.Compare(pEncodedMailboxPath, InboxBytes, false)) lPath = InboxString;
            else if (!cTools.TryEncodedMailboxPathToString(pEncodedMailboxPath, pDelimiter, pUTF8Enabled, out lPath)) { rResult = null; return false; }

            char? lDelimiter;
            if (pDelimiter == null) lDelimiter = null;
            else lDelimiter = (char)pDelimiter.Value;

            return TryConstruct(lPath, lDelimiter, out rResult);
        }

        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailboxName), nameof(_Tests));

            _Tests_MailboxName("", true, lContext);

            _Tests_MailboxName("/", true, lContext);
            _Tests_MailboxName("fred", false, lContext);
            _Tests_MailboxName("fred/", true, lContext);
            _Tests_MailboxName("/fred", false, lContext);
            _Tests_MailboxName("/fred/", true, lContext);
            _Tests_MailboxName("fred/fr€d", false, lContext);
            _Tests_MailboxName("fred/fr€d/", true, lContext);
            _Tests_MailboxName("/fred/fr€d", false, lContext);
            _Tests_MailboxName("/fred/fr€d/", true, lContext);
        }

        [Conditional("DEBUG")]
        private static void _Tests_MailboxName(string pMailboxPath, bool pExpectFail, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailboxName), nameof(_Tests_MailboxName), pMailboxPath);

            cCommandPartFactory lFactory;
            cCommandPart lCommandPart;
            cBytesCursor lCursor;
            IList<byte> lEncodedMailboxPath;
            cMailboxName lMailboxName;

            lFactory = new cCommandPartFactory(false, null);

            if (!lFactory.TryAsMailbox(pMailboxPath, '/', out lCommandPart, out _)) throw new cTestsException($"mailboxname conversion failed on '{pMailboxPath}'");
            cTextCommandPart lTCP = lCommandPart as cTextCommandPart;

            lCursor = new cBytesCursor(lTCP.Bytes);
            lCursor.GetAString(out lEncodedMailboxPath);

            if (cMailboxName.TryConstruct(lEncodedMailboxPath, cASCII.SLASH, false, out lMailboxName))
            {
                if (pExpectFail) throw new cTestsException($"mailboxname construction succeeded on '{pMailboxPath}' and it shouldn't have");
            }
            else
            {
                if (!pExpectFail) throw new cTestsException($"mailboxname construction failed on '{pMailboxPath}' and it shouldn't have");
                return;
            }

            if (lMailboxName.Path != pMailboxPath) throw new cTestsException($"mailboxname conversion failed on '{pMailboxPath}' -> {lTCP.Bytes} -> '{lMailboxName}'", lContext);

            lFactory = new cCommandPartFactory(true, null);
            lFactory.TryAsMailbox(pMailboxPath, '/', out lCommandPart, out _);
            lTCP = lCommandPart as cTextCommandPart;
            lCursor = new cBytesCursor(lTCP.Bytes);
            lCursor.GetAString(out lEncodedMailboxPath);
            cMailboxName.TryConstruct(lEncodedMailboxPath, cASCII.SLASH, true, out lMailboxName);
            if (lMailboxName.Path != pMailboxPath) throw new cTestsException($"mailboxname conversion failed on '{pMailboxPath}' -> {lTCP.Bytes} -> '{lMailboxName}'", lContext);
        }
    }
}