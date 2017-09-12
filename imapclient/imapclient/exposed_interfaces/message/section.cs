using System;
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
        public readonly cHeaderNames Names;

        public cSection(string pPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            TextPart = eSectionPart.all;
            Names = null;
        }

        public cSection(string pPart, eSectionPart pTextPart)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;
            if (pTextPart != eSectionPart.header && pTextPart != eSectionPart.text && (pPart == null || pTextPart != eSectionPart.mime)) throw new ArgumentOutOfRangeException(nameof(pTextPart));
            TextPart = pTextPart;
            Names = null;
        }

        public cSection(string pPart, cHeaderNames pNames, bool pNot = false)
        {
            if (pPart != null && !ZValidPart(pPart)) throw new ArgumentOutOfRangeException(nameof(pPart));
            Part = pPart;

            if (pNot) TextPart = eSectionPart.headerfieldsnot;
            else TextPart = eSectionPart.headerfields;

            if (pNames == null) throw new ArgumentNullException(nameof(pNames));
            if (pNames.Count == 0) throw new ArgumentOutOfRangeException(nameof(pNames));
            Names = pNames;
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
                if (Names != null) lHash = lHash * 23 + Names.GetHashCode();
                return lHash;
            }
        }

        public override string ToString() => $"{nameof(cSection)}({Part},{TextPart},{Names})";

        public static bool operator ==(cSection pA, cSection pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return (pA.Part == pB.Part && pA.TextPart == pB.TextPart && pA.Names == pB.Names);
        }

        public static bool operator !=(cSection pA, cSection pB) => !(pA == pB);
    }
}