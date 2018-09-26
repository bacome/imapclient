using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents a string that may include language information as per RFC 2231.
    /// </summary>
    [Serializable]
    public class cCulturedString
    {
        /// <summary>
        /// The parts of the string. May be <see langword="null"/>.
        /// </summary>
        public readonly ReadOnlyCollection<cCulturedStringPart> Parts;

        internal cCulturedString(IList<byte> pBytes, bool pPhraseSemantics)
        {
            // "phrasesemantics" is to deal with the problem of IMAP removing the RFC 2822 quoting (see the addr-mailbox and addr-name definitions in RFC 3501)
            //  consider the display name >fr€d " " fr€d< (with the 'fr€d's as encoded words)
            //   after IMAP removing the quoting the string looks like this >fr€d   fr€d<, 
            //    so if I were to ignore the spaces between the encoded words
            //     (as I should when processing unstructured text (like the subject))
            //      then I'd end up with >fr€dfr€d<
            //
            //  for this code to work the IMAP server has to guarantee that only one space will be inserted between phrase parts when sending the display name
            //   (which is what I would guess it would do)
            //
            // Also consider the removing of quotes around text that contains something that looks like an encoded word
            //  quite how that is meant to be handled I have no idea
            //   e.g. the name >fred "fr€d" fred< (with the 'fr€d' as an encoded word) would end up with the middle fred decoded
            //   the only defence against this is the one that this library's own encoder uses, which is to encode something that looks like an encoded word (even if UTF8 is on) rather than quoting it

            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));

            if (pBytes.Count == 0)
            {
                Parts = null;
                return;
            }

            cBytesCursor lCursor = new cBytesCursor(pBytes);

            List<cCulturedStringPart> lParts = new List<cCulturedStringPart>();
            cByteList lBytes = new cByteList();
            cByteList lPendingWSP = new cByteList();

            while (!lCursor.Position.AtEnd)
            {
                var lBookmark = lCursor.Position;

                bool lFoundEncodedWord;

                if (lCursor.ProcessEncodedWord(out var lString, out var lLanguageTag))
                {
                    lPendingWSP.Clear();
                    if (lCursor.Position.AtEnd) lFoundEncodedWord = true;
                    else if (lCursor.ZGetRFC822FWS(lPendingWSP)) lFoundEncodedWord = true;
                    else lFoundEncodedWord = false;
                }
                else lFoundEncodedWord = false;

                if (lFoundEncodedWord)
                {
                    if (lBytes.Count > 0)
                    {
                        lParts.Add(new cCulturedStringPart(cTools.UTF8BytesToString(lBytes), null));
                        lBytes.Clear();
                    }

                    lParts.Add(new cCulturedStringPart(lString, lLanguageTag));

                    if (pPhraseSemantics && lPendingWSP.Count > 1)
                    {
                        lBytes.AddRange(lPendingWSP);
                        lPendingWSP.Clear();
                    }
                }
                else
                {
                    lCursor.Position = lBookmark;

                    if (lPendingWSP != null)
                    {
                        lBytes.AddRange(lPendingWSP);
                        lPendingWSP.Clear();
                    }

                    if (!lCursor.GetByte(out var lByte)) break;

                    lBytes.Add(lByte);
                }
            }

            if (lBytes.Count > 0) lParts.Add(new cCulturedStringPart(cTools.UTF8BytesToString(lBytes), null));
            Parts = lParts.AsReadOnly();
        }

        internal cCulturedString(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            List<cCulturedStringPart> lParts = new List<cCulturedStringPart>();
            lParts.Add(new cCulturedStringPart(pString, null));
            Parts = new ReadOnlyCollection<cCulturedStringPart>(lParts);
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            foreach (var lPart in Parts) if (lPart == null) throw new cDeserialiseException(nameof(cCulturedString), nameof(Parts), kDeserialiseExceptionMessage.ContainsNulls);
        }

        /**<summary>Returns the string data sans the language information.</summary>*/
        public override string ToString()
        {
            if (Parts == null) return string.Empty;

            var lBuilder = new StringBuilder();
            foreach (var lPart in Parts) lBuilder.Append(lPart.String);
            return lBuilder.ToString();
        }

        /// <inheritdoc cref="ToString"/>
        public static implicit operator string(cCulturedString pString) => pString?.ToString();

        [Conditional("DEBUG")]
        internal static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cCulturedString), nameof(_Tests));

            cCulturedString lCString;
            string lString;

            lCString = new cCulturedString(new cBytes("=?iso-8859-1?q?this=20is=20some=20text?="), false);
            lString = lCString;
            if (lString != "this is some text") throw new cTestsException($"{nameof(cCulturedString)}.1");

            if (new cCulturedString(new cBytes("=?US-ASCII?Q?Keith_Moore?= <moore@cs.utk.edu>"), false) != "Keith Moore <moore@cs.utk.edu>") throw new cTestsException($"{nameof(cCulturedString)}.2");

            lString = new cCulturedString(new cBytes("=?ISO-8859-1?Q?Keld_J=F8rn_Simonsen?= <keld@dkuug.dk>"), false);
            if (lString != "Keld Jørn Simonsen <keld@dkuug.dk>") throw new cTestsException($"{nameof(cCulturedString)}.3");

            lString = new cCulturedString(new cBytes("=?ISO-8859-1?Q?Andr=E9?= Pirard <PIRARD@vm1.ulg.ac.be>"), false);
            if (lString != "André Pirard <PIRARD@vm1.ulg.ac.be>") throw new cTestsException($"{nameof(cCulturedString)}.4");

            lString = new cCulturedString(new cBytes("=?ISO-8859-1?B?SWYgeW91IGNhbiByZWFkIHRoaXMgeW8=?= =?ISO-8859-2?B?dSB1bmRlcnN0YW5kIHRoZSBleGFtcGxlLg==?="), false);
            if (lString != "If you can read this you understand the example.") throw new cTestsException($"{nameof(cCulturedString)}.5");

            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?= b"), false) != "a b") throw new cTestsException($"{nameof(cCulturedString)}.6.1");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?= b"), true) != "a b") throw new cTestsException($"{nameof(cCulturedString)}.6.2");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?= =?ISO-8859-1?Q?b?="), false) != "ab") throw new cTestsException($"{nameof(cCulturedString)}.7.1");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?= =?ISO-8859-1?Q?b?="), true) != "ab") throw new cTestsException($"{nameof(cCulturedString)}.7.2");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?=  =?ISO-8859-1?Q?b?="), false) != "ab") throw new cTestsException($"{nameof(cCulturedString)}.8.1");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?=  =?ISO-8859-1?Q?b?="), true) != "a  b") throw new cTestsException($"{nameof(cCulturedString)}.8.2");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a_b?="), false) != "a b") throw new cTestsException($"{nameof(cCulturedString)}.9");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?= =?ISO-8859-2?Q?_b?="), false) != "a b") throw new cTestsException($"{nameof(cCulturedString)}.10");

            lCString = new cCulturedString(new cBytes("=?US-ASCII*EN?Q?Keith_Moore?= <moore@cs.utk.edu>"), false);

            if (lCString != "Keith Moore <moore@cs.utk.edu>") throw new cTestsException($"{nameof(cCulturedString)}.11");
            if (lCString.Parts[0].LanguageTag != "EN") throw new cTestsException($"{nameof(cCulturedString)}.12");
        }
    }

    /// <summary>
    /// Represents part of a string that may include language information as per RFC 2231.
    /// </summary>
    /// <seealso cref="cCulturedString"/>
    [Serializable]
    public class cCulturedStringPart
    {
        /// <summary>
        /// The text of the part (after RFC 2231 decoding). 
        /// </summary>
        public readonly string String;

        /// <summary>
        /// The language of the part. May be <see langword="null"/>.
        /// </summary>
        public readonly string LanguageTag;

        internal cCulturedStringPart(string pString, string pLanguageTag)
        {
            String = pString ?? throw new ArgumentNullException(nameof(pString));
            LanguageTag = pLanguageTag;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (String == null) throw new cDeserialiseException(nameof(cCulturedStringPart), nameof(String), kDeserialiseExceptionMessage.IsNull);
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cCulturedStringPart)}({String},{LanguageTag})";
    }
}