﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents a string that may include language information as per RFC 2231.
    /// </summary>
    public class cCulturedString : IEquatable<cCulturedString>
    {
        private static readonly cBytes kSpace = new cBytes(" ");
        private static readonly cBytes kCRLFSPACE = new cBytes("\r\n ");
        private static readonly cBytes kTab = new cBytes("\t");
        private static readonly cBytes kCRLFTAB = new cBytes("\r\n\t");

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
            cBytes lPendingWSP = null;

            while (!lCursor.Position.AtEnd)
            {
                var lBookmark = lCursor.Position;

                bool lFoundEncodedWord;

                if (lCursor.ProcessEncodedWord(out var lString, out var lLanguageTag))
                {
                    if (lCursor.Position.AtEnd) lFoundEncodedWord = true;
                    else if (lCursor.SkipByte(cASCII.SPACE))
                    {
                        lFoundEncodedWord = true;
                        lPendingWSP = kSpace;
                    }
                    else if (lCursor.SkipBytes(kCRLFSPACE))
                    {
                        lFoundEncodedWord = true;
                        lPendingWSP = kCRLFSPACE;
                    }
                    else if (lCursor.SkipByte(cASCII.TAB))
                    {
                        lFoundEncodedWord = true;
                        lPendingWSP = kTab;
                    }
                    else if (lCursor.SkipBytes(kCRLFTAB))
                    {
                        lFoundEncodedWord = true;
                        lPendingWSP = kCRLFTAB;
                    }
                    else lFoundEncodedWord = false;
                }
                else lFoundEncodedWord = false;

                if (lFoundEncodedWord)
                {
                    if (lBytes.Count > 0)
                    {
                        lParts.Add(new cCulturedStringPart(cTools.UTF8BytesToString(lBytes), null));
                        lBytes = new cByteList();
                    }

                    lParts.Add(new cCulturedStringPart(lString, lLanguageTag));
                }
                else
                {
                    lCursor.Position = lBookmark;

                    if (lPendingWSP != null)
                    {
                        lBytes.AddRange(lPendingWSP);
                        lPendingWSP = null;
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cCulturedString pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cCulturedString;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (var lPart in Parts) lHash = lHash * 23 + lPart.GetHashCode();
                return lHash;
            }
        }

        /**<summary>Returns the string data sans the language information.</summary>*/
        public override string ToString()
        {
            if (Parts == null) return string.Empty;

            var lBuilder = new StringBuilder();
            foreach (var lPart in Parts) lBuilder.Append(lPart.String);
            return lBuilder.ToString();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cCulturedString pA, cCulturedString pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            if (pA.Parts.Count != pB.Parts.Count) return false;
            for (int i = 0; i < pA.Parts.Count; i++) if (pA.Parts[i] != pB.Parts[i]) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cCulturedString pA, cCulturedString pB) => !(pA == pB);

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
    public class cCulturedStringPart : IEquatable<cCulturedStringPart>
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cCulturedStringPart pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cCulturedStringPart;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + String.GetHashCode();
                if (LanguageTag != null) lHash = lHash * 23 + LanguageTag.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cCulturedStringPart)}({String},{LanguageTag})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cCulturedStringPart pA, cCulturedStringPart pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.String == pB.String && pA.LanguageTag == pB.LanguageTag;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cCulturedStringPart pA, cCulturedStringPart pB) => !(pA == pB);
    }
}