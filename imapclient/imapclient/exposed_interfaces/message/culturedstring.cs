using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a string that may include language information as per RFC 2231.
    /// </summary>
    /// <seealso cref="cMessage.Subject"/>
    /// <seealso cref="cAddress.DisplayName"/>
    /// <seealso cref="cAttachment.Description"/>
    /// <seealso cref="cEnvelope.Subject"/>
    /// <seealso cref="cSinglePartBody.Description"/>
    public class cCulturedString
    {
        /// <summary>
        /// The parts of the string. May be <see langword="null"/>.
        /// </summary>
        public readonly ReadOnlyCollection<cCulturedStringPart> Parts;

        internal cCulturedString(IList<byte> pBytes)
        {
            if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));

            if (pBytes.Count == 0)
            {
                Parts = null;
                return;
            }

            cBytesCursor lCursor = new cBytesCursor(pBytes);

            List<cCulturedStringPart> lParts = new List<cCulturedStringPart>();
            cByteList lBytes = new cByteList();
            bool lPendingSpace = false;

            while (!lCursor.Position.AtEnd)
            {
                var lBookmark = lCursor.Position;

                if (lCursor.ProcessEncodedWord(out var lString, out var lLanguageTag) && (lCursor.Position.AtEnd || lCursor.SkipByte(cASCII.SPACE)))
                {
                    if (lBytes.Count > 0)
                    {
                        lParts.Add(new cCulturedStringPart(cTools.UTF8BytesToString(lBytes), null));
                        lBytes = new cByteList();
                    }

                    lParts.Add(new cCulturedStringPart(lString, lLanguageTag));

                    lPendingSpace = true;
                }
                else
                {
                    lCursor.Position = lBookmark;

                    if (lPendingSpace)
                    {
                        lBytes.Add(cASCII.SPACE);
                        lPendingSpace = false;
                    }

                    if (!lCursor.GetByte(out var lByte)) break;

                    lBytes.Add(lByte);
                }
            }

            if (lBytes.Count > 0) lParts.Add(new cCulturedStringPart(cTools.UTF8BytesToString(lBytes), null));
            Parts = new ReadOnlyCollection<cCulturedStringPart>(lParts);
        }

        internal cCulturedString(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));
            List<cCulturedStringPart> lParts = new List<cCulturedStringPart>();
            lParts.Add(new cCulturedStringPart(pString, null));
            Parts = new ReadOnlyCollection<cCulturedStringPart>(lParts);
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

            lCString = new cCulturedString(new cBytes("=?iso-8859-1?q?this=20is=20some=20text?="));
            lString = lCString;
            if (lString != "this is some text") throw new cTestsException($"{nameof(cCulturedString)}.1");

            if (new cCulturedString(new cBytes("=?US-ASCII?Q?Keith_Moore?= <moore@cs.utk.edu>")) != "Keith Moore <moore@cs.utk.edu>") throw new cTestsException($"{nameof(cCulturedString)}.2");

            lString = new cCulturedString(new cBytes("=?ISO-8859-1?Q?Keld_J=F8rn_Simonsen?= <keld@dkuug.dk>"));
            if (lString != "Keld Jørn Simonsen <keld@dkuug.dk>") throw new cTestsException($"{nameof(cCulturedString)}.3");

            lString = new cCulturedString(new cBytes("=?ISO-8859-1?Q?Andr=E9?= Pirard <PIRARD@vm1.ulg.ac.be>"));
            if (lString != "André Pirard <PIRARD@vm1.ulg.ac.be>") throw new cTestsException($"{nameof(cCulturedString)}.4");

            lString = new cCulturedString(new cBytes("=?ISO-8859-1?B?SWYgeW91IGNhbiByZWFkIHRoaXMgeW8=?= =?ISO-8859-2?B?dSB1bmRlcnN0YW5kIHRoZSBleGFtcGxlLg==?="));
            if (lString != "If you can read this you understand the example.") throw new cTestsException($"{nameof(cCulturedString)}.5");

            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?= b")) != "a b") throw new cTestsException($"{nameof(cCulturedString)}.6");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?= =?ISO-8859-1?Q?b?=")) != "ab") throw new cTestsException($"{nameof(cCulturedString)}.7");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a_b?=")) != "a b") throw new cTestsException($"{nameof(cCulturedString)}.8");
            if (new cCulturedString(new cBytes("=?ISO-8859-1?Q?a?= =?ISO-8859-2?Q?_b?=")) != "a b") throw new cTestsException($"{nameof(cCulturedString)}.9");

            lCString = new cCulturedString(new cBytes("=?US-ASCII*EN?Q?Keith_Moore?= <moore@cs.utk.edu>"));

            if (lCString != "Keith Moore <moore@cs.utk.edu>") throw new cTestsException($"{nameof(cCulturedString)}.10");
            if (lCString.Parts[0].LanguageTag != "EN") throw new cTestsException($"{nameof(cCulturedString)}.11");
        }
    }

    /// <summary>
    /// Represents part of a string that may include language information as per RFC 2231.
    /// </summary>
    /// <seealso cref="cCulturedString"/>
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

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cCulturedStringPart)}({String},{LanguageTag})";
    }
}