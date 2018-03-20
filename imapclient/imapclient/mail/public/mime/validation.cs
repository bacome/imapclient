using System;
using System.Net.Mail;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public static class cValidation
    {
        // validates input from the user of the library (NOT input from a connected server: generally server input has a more flexible syntax)

        public static bool IsDotAtom(string pText)
        {
            if (pText == null) return false;
            var lAtoms = pText.Split('.');
            foreach (var lAtom in lAtoms) if (lAtom.Length == 0 || !cCharset.AText.ContainsAll(lAtom)) return false;
            return true;
        }

        public static bool IsDomainLiteral(string pText)
        {
            if (pText == null) return false;
            cBytesCursor lCursor = new cBytesCursor(pText);
            if (!lCursor.SkipByte(cASCII.LBRACKET)) return false;
            lCursor.GetToken(cCharset.WSPDText, null, null, out cByteList _);
            if (!lCursor.SkipByte(cASCII.RBRACKET)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsDomain(string pText) => IsDomainLiteral(pText) || IsDotAtom(pText);

        public static bool IsNoFoldLiteral(string pString)
        {
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.SkipByte(cASCII.LBRACKET)) return false;
            lCursor.GetToken(cCharset.DText, null, null, out cByteList _);
            if (!lCursor.SkipByte(cASCII.RBRACKET)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsMessageIds()

        public static bool IsValid(MailAddress p)
    }
}