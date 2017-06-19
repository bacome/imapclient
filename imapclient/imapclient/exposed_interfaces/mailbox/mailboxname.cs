using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public class cMailboxName
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
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pDelimiter != null &&  !cTools.IsValidDelimiter(pDelimiter.Value)) throw new ArgumentOutOfRangeException(nameof(pDelimiter));

            if (pName.Equals(InboxString, StringComparison.InvariantCultureIgnoreCase))
            {
                Name = InboxString;
                Delimiter = pDelimiter;
                return;
            }
                
            cCommandPart.cFactory lFactory = new cCommandPart.cFactory();
            if (!lFactory.TryAsListMailbox(pName, pDelimiter, out _)) throw new ArgumentOutOfRangeException(nameof(pName));

            Name = pName;
            Delimiter = pDelimiter;
        }

        public static bool TryConstruct(string pName, char? pDelimiter, out cMailboxName rResult)
        {
            if (pName == null) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }

            if (pName.Equals(InboxString, StringComparison.InvariantCultureIgnoreCase))
            {
                rResult = new cMailboxName(pName, pDelimiter, true);
                return true;
            }

            cCommandPart.cFactory lFactory = new cCommandPart.cFactory();
            if (!lFactory.TryAsListMailbox(pName, pDelimiter, out _)) { rResult = null; return false; }

            rResult = new cMailboxName(pName, pDelimiter, true);
            return true;
        }

        public static bool TryConstruct(IList<byte> pBytes, byte? pDelimiter, fEnableableExtensions pEnabledExtensions, out cMailboxName rResult)
        {
            if (pBytes == null) { rResult = null; return false; }
            if (pDelimiter != null && !cTools.IsValidDelimiter(pDelimiter.Value)) { rResult = null; return false; }

            string lName;

            if (cASCII.Compare(pBytes, InboxBytes, false)) lName = InboxString;
            else if (!cTools.TryMailboxNameBytesToString(pBytes, pDelimiter, pEnabledExtensions, out lName)) { rResult = null; return false; }

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
            return (pA.Name == pB.Name && pA.Delimiter == pB.Delimiter);
        }

        public static bool operator !=(cMailboxName pA, cMailboxName pB) => !(pA == pB);

        public static class cTests
        {
            [Conditional("DEBUG")]
            public static void Tests(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cMailboxName), nameof(Tests));

                ZTestsMailboxName("", lContext);

                ZTestsMailboxName("/", lContext);
                ZTestsMailboxName("fred", lContext);
                ZTestsMailboxName("fred/", lContext);
                ZTestsMailboxName("/fred", lContext);
                ZTestsMailboxName("/fred/", lContext);
                ZTestsMailboxName("fred/fr€d", lContext);
                ZTestsMailboxName("fred/fr€d/", lContext);
                ZTestsMailboxName("/fred/fr€d", lContext);
                ZTestsMailboxName("/fred/fr€d/", lContext);
            }

            [Conditional("DEBUG")]
            private static void ZTestsMailboxName(string pMailboxName, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cMailboxName), nameof(ZTestsMailboxName), pMailboxName);

                cCommandPart.cFactory lFactory;
                cCommandPart lCommandPart;
                cBytesCursor lCursor;
                IList<byte> lEncodedMailboxName;
                cMailboxName lMailboxName;

                lFactory = new cCommandPart.cFactory(false);
                lFactory.TryAsMailbox(new cMailboxName(pMailboxName, '/'), out lCommandPart, out _);
                lCursor = new cBytesCursor(lCommandPart.Bytes);
                lCursor.GetAString(out lEncodedMailboxName);
                cMailboxName.TryConstruct(lEncodedMailboxName, cASCII.SLASH, fEnableableExtensions.none, out lMailboxName);
                if (lMailboxName.Name != pMailboxName) throw new cTestsException($"mailboxname conversion failed on '{pMailboxName}' -> {lCommandPart.Bytes} -> '{lMailboxName}'", lContext);

                lFactory = new cCommandPart.cFactory(true);
                lFactory.TryAsMailbox(new cMailboxName(pMailboxName, '/'), out lCommandPart, out _);
                lCursor = new cBytesCursor(lCommandPart.Bytes);
                lCursor.GetAString(out lEncodedMailboxName);
                cMailboxName.TryConstruct(lEncodedMailboxName, cASCII.SLASH, fEnableableExtensions.utf8, out lMailboxName);
                if (lMailboxName.Name != pMailboxName) throw new cTestsException($"mailboxname conversion failed on '{pMailboxName}' -> {lCommandPart.Bytes} -> '{lMailboxName}'", lContext);
            }
        }
    }
}