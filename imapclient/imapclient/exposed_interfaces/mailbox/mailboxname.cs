using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public class cMailboxName : IComparable<cMailboxName>
    {
        public const string InboxString = "INBOX";
        public static readonly ReadOnlyCollection<byte> InboxBytes = new cBytes(InboxString);

        public readonly string Name;
        public readonly char? Delimiter;

        private cMailboxName(string pName, char? pDelimiter, bool pValid)
        {
            Name = pName;
            Delimiter = pDelimiter;
        }

        public cMailboxName(string pName, char? pDelimiter)
        {
            if (string.IsNullOrEmpty(pName)) throw new ArgumentNullException(nameof(pName));
            if (pDelimiter != null &&  !cTools.IsValidDelimiter(pDelimiter.Value)) throw new ArgumentOutOfRangeException(nameof(pDelimiter));

            if (pName.Equals(InboxString, StringComparison.InvariantCultureIgnoreCase))
            {
                Name = InboxString;
                Delimiter = pDelimiter;
                return;
            }

            if (pName[pName.Length - 1] == pDelimiter) throw new ArgumentOutOfRangeException(nameof(pName));
                
            if (!cCommandPartFactory.Validation.TryAsListMailbox(pName, pDelimiter, out _)) throw new ArgumentOutOfRangeException(nameof(pName));

            Name = pName;
            Delimiter = pDelimiter;
        }

        public static bool TryConstruct(string pName, char? pDelimiter, out cMailboxName rResult)
        {
            if (string.IsNullOrEmpty(pName)) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }

            if (pName.Equals(InboxString, StringComparison.InvariantCultureIgnoreCase))
            {
                rResult = new cMailboxName(pName, pDelimiter, true);
                return true;
            }

            if (pName[pName.Length - 1] == pDelimiter) { rResult = null; return false; }

            if (!cCommandPartFactory.Validation.TryAsListMailbox(pName, pDelimiter, out _)) { rResult = null; return false; }

            rResult = new cMailboxName(pName, pDelimiter, true);
            return true;
        }

        public static bool TryConstruct(IList<byte> pBytes, byte? pDelimiter, bool pUTF8Enabled, out cMailboxName rResult)
        {
            if (pBytes == null || pBytes.Count == 0) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }

            string lName;

            if (cASCII.Compare(pBytes, InboxBytes, false)) lName = InboxString;
            else if (!cTools.TryMailboxNameBytesToString(pBytes, pDelimiter, pUTF8Enabled, out lName)) { rResult = null; return false; }

            char? lDelimiter;
            if (pDelimiter == null) lDelimiter = null;
            else lDelimiter = (char)pDelimiter.Value;

            return TryConstruct(lName, lDelimiter, out rResult);
        }

        public override bool Equals(object pObject) => this == pObject as cMailboxName;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + Name.GetHashCode();
                if (Delimiter != null) lHash = lHash * 23 + Delimiter.GetHashCode();
                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cMailboxName)}({Name},{Delimiter})";

        public static bool operator ==(cMailboxName pA, cMailboxName pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Name == pB.Name && pA.Delimiter == pB.Delimiter;
        }

        public static bool operator !=(cMailboxName pA, cMailboxName pB) => !(pA == pB);

        public int CompareTo(cMailboxName pOther)
        {
            if (pOther == null) return 1;

            var lCompareTo = Name.CompareTo(pOther.Name);

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

        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
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
        private static void _Tests_MailboxName(string pMailboxName, bool pExpectFail, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cMailboxName), nameof(_Tests_MailboxName), pMailboxName);

            cCommandPartFactory lFactory;
            cCommandPart lCommandPart;
            cBytesCursor lCursor;
            IList<byte> lEncodedMailboxName;
            cMailboxName lMailboxName;

            lFactory = new cCommandPartFactory(false, null);

            if (lFactory.TryAsMailbox(pMailboxName, '/', out lCommandPart, out _))
            {
                if (pExpectFail) throw new cTestsException($"mailboxname conversion succeeded on '{pMailboxName}'");
            }
            else
            {
                if (pExpectFail) return;
                throw new cTestsException($"mailboxname conversion failed on '{pMailboxName}'");
            }

            lCursor = new cBytesCursor(lCommandPart.Bytes);
            lCursor.GetAString(out lEncodedMailboxName);
            cMailboxName.TryConstruct(lEncodedMailboxName, cASCII.SLASH, false, out lMailboxName);
            if (lMailboxName.Name != pMailboxName) throw new cTestsException($"mailboxname conversion failed on '{pMailboxName}' -> {lCommandPart.Bytes} -> '{lMailboxName}'", lContext);

            lFactory = new cCommandPartFactory(true, null);
            lFactory.TryAsMailbox(pMailboxName, '/', out lCommandPart, out _);
            lCursor = new cBytesCursor(lCommandPart.Bytes);
            lCursor.GetAString(out lEncodedMailboxName);
            cMailboxName.TryConstruct(lEncodedMailboxName, cASCII.SLASH, true, out lMailboxName);
            if (lMailboxName.Name != pMailboxName) throw new cTestsException($"mailboxname conversion failed on '{pMailboxName}' -> {lCommandPart.Bytes} -> '{lMailboxName}'", lContext);
        }
    }
}