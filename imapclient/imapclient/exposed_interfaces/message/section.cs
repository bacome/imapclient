using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public enum eSectionPart { all, header, headerfields, headerfieldsnot, text, mime }

    public class cSection
    {
        public static readonly cSection All = new cSection(null);
        public static readonly cSection Header = new cSection(null, eSectionPart.header);
        public static readonly cSection Text = new cSection(null, eSectionPart.text);

        public readonly string Part; // may be null if the section refers to the top-most part
        public readonly eSectionPart TextPart;
        public readonly cStrings HeaderFields; // sorted, uppercased list of header fields or null

        public cSection(string pPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            TextPart = eSectionPart.all;
            HeaderFields = null;
        }

        public cSection(string pPart, eSectionPart pTextPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            if (pTextPart != eSectionPart.header && pTextPart != eSectionPart.text && (pPart == null || pTextPart != eSectionPart.mime)) throw new ArgumentOutOfRangeException(nameof(pTextPart));
            TextPart = pTextPart;
            HeaderFields = null;
        }

        public cSection(string pPart, IList<string> pHeaderFields, bool pNot = false)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;

            if (pNot) TextPart = eSectionPart.headerfieldsnot;
            else TextPart = eSectionPart.headerfields;

            if (pHeaderFields == null) throw new ArgumentNullException(nameof(pHeaderFields));
            if (pHeaderFields.Count == 0) throw new ArgumentOutOfRangeException(nameof(pHeaderFields));

            cCommandPart.cFactory lFactory = new cCommandPart.cFactory();

            List<string> lHeaderFields = new List<string>();

            foreach (string lHeaderField in pHeaderFields)
            {
                if (lHeaderField == null) throw new ArgumentOutOfRangeException(nameof(pHeaderFields));
                if (!lFactory.TryAsAString(lHeaderField, false, out _)) throw new ArgumentOutOfRangeException(nameof(pHeaderFields));
                lHeaderFields.Add(lHeaderField.ToUpperInvariant());
            }

            lHeaderFields.Sort();

            HeaderFields = new cStrings(lHeaderFields);
        }

        private bool ZValidPart(string pPart)
        {
            if (!cBytesCursor.TryConstruct(pPart, out var lCursor)) return false;

            while (true)
            {
                if (!lCursor.GetNZNumber(out _, out _)) return false;
                if (!lCursor.SkipByte(cASCII.DOT)) break;
            }

            return lCursor.Position.AtEnd;
        }

        public override bool Equals(object pObject) => this == pObject as cSection;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;

                if (Part != null) lHash = lHash * 23 + Part.GetHashCode();
                lHash = lHash * 23 + TextPart.GetHashCode();
                if (HeaderFields != null) lHash = lHash * 23 + HeaderFields.GetHashCode();
                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cSection)}({Part},{TextPart},{HeaderFields})";

        public static bool operator ==(cSection pA, cSection pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.Part == pB.Part && pA.TextPart == pB.TextPart && pA.HeaderFields == pB.HeaderFields);
        }

        public static bool operator !=(cSection pA, cSection pB) => !(pA == pB);
    }
}