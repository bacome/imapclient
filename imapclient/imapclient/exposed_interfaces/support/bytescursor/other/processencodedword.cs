using System;

namespace work.bacome.imapclient.support
{
    public partial class cBytesCursor
    {
        private static readonly cBytes kEqualsQuestionMark = new cBytes("=?");
        private static readonly cBytes kQuestionMarkBQuestionMark = new cBytes("?B?");
        private static readonly cBytes kQuestionMarkQQuestionMark = new cBytes("?Q?");
        private static readonly cBytes kQuestionMarkEquals = new cBytes("?=");

        public bool ProcessEncodedWord(out string rString, out string rLanguageTag)
        {
            // leading =?
            if (!SkipBytes(kEqualsQuestionMark)) { rString = null; rLanguageTag = null; return false; }

            // charset
            if (!GetToken(cCharset.CharsetName, null, null, out var lCharset)) { rString = null; rLanguageTag = null; return false; }

            // optional language tag
            if (SkipByte(cASCII.ASTERISK))
            {
                if (!GetLanguageTag(out rLanguageTag)) { rString = null; return false; }
            }
            else rLanguageTag = null;

            // encoding q or b

            cByteList lBytes;

            if (SkipBytes(kQuestionMarkBQuestionMark))
            {
                if (!GetToken(cCharset.Base64, null, null, out cByteList lBase64)) { rString = null; return false; }
                if (!cBase64.TryDecode(lBase64, out var lDecoded, out _)) { rString = null; return false; }
                lBytes = lDecoded;
            }
            else if (SkipBytes(kQuestionMarkQQuestionMark))
            {
                if (!GetToken(cCharset.QEncoding, cASCII.EQUALS, cASCII.UNDERSCORE, out lBytes)) { rString = null; return false; }
            }
            else { rString = null; return false; }

            // trailing ?=
            if (!SkipBytes(kQuestionMarkEquals)) { rString = null; return false; }

            // convert bytes using charset
            return cTools.TryCharsetBytesToString(lCharset, lBytes, out rString);
        }
    }
}