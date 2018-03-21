using System;
using System.Collections.Generic;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public static class cValidation
    {
        // validates input from the user of the library
        //  this code only outputs non-obsolete rfc 5322 syntax

        public static bool IsDotAtom(string pString, out string rDotAtom)
        {
            ;?;

            if (pString == null) return false;




            var lAtoms = pString.Split('.');
            foreach (var lAtom in lAtoms) if (lAtom.Length == 0 || !cCharset.AText.ContainsAll(lAtom)) return false;
            return true;
        }

        public static bool IsDomainLiteral(string pString, out string rDomainLiteral)
        {
            if (pString == null) return false;
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.SkipByte(cASCII.LBRACKET)) return false;
            lCursor.GetToken(cCharset.WSPDText, null, null, out cByteList _);
            if (!lCursor.SkipByte(cASCII.RBRACKET)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsDomain(string pString, out string rDomain) => IsDomainLiteral(pString, out rDomain) || IsDotAtom(pString, out rDomain);

        public static bool IsNoFoldLiteral(string pString, out string rNoFoldLiteral)
        {
            if (pString == null) return false;
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC5322NoFoldLiteral(out _)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsMessageId(string pString, out string rMessageId)
        {
            if (pString == null) return false;
            cBytesCursor lCursor = new cBytesCursor(pString);
            if (!lCursor.GetRFC5322MsgId(out _)) return false;
            return lCursor.Position.AtEnd;
        }

        public static bool IsMessageIds(string pString, out List<string> rMessageIds)
        {
            if (pString == null) { rMessageIds = null; return false; }

            cBytesCursor lCursor = new cBytesCursor(pString);

            var lMessageIds = new List<string>();

            while (true)
            {
                lCursor.SkipRFC822CFWS();
                if (!lCursor.GetRFC5322MsgId(out var lMessageId)) break;
                lMessageIds.Add(lMessageId);
            }

            if (!lCursor.Position.AtEnd)
            {
                rMessageIds = null;
                return false;
            }

            if (lMessageIds.Count == 0)
            {
                rMessageIds = null;
                return false;
            }

            rMessageIds = lMessageIds;
            return true;
        }

        public static bool IsPhrases(string pString, out List<cHeaderPhraseValue> rPhrases)
        {
            if (pString == null) { rPhrases = null; return false; }

            cBytesCursor lCursor = new cBytesCursor(pString);

            var lMessageIds = new List<string>();

            while (true)
            {
                lCursor.SkipRFC822CFWS();
                

                if (!lCursor.GetRFC5322MsgId(out var lMessageId)) break;
                lMessageIds.Add(lMessageId);
            }

            if (!lCursor.Position.AtEnd)
            {
                rMessageIds = null;
                return false;
            }

            if (lMessageIds.Count == 0)
            {
                rMessageIds = null;
                return false;
            }

            rMessageIds = lMessageIds;
            return true;
        }

        public static bool IsPhrase(string pString, out cHeaderPhraseValue rPhrase)
        {

        }

    }
}