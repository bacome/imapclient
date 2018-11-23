using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a string that may include language information as per RFC 2231.
    /// </summary>
    [Serializable]
    public class cCulturedString: iCanComposeHeaderFieldValue, IEquatable<cCulturedString>, IComparable<cCulturedString>
    {
        /// <summary>
        /// The parts of the string. May be <see langword="null"/>.
        /// </summary>
        public readonly ReadOnlyCollection<cCulturedStringPart> Parts;

        /// <summary>
        /// Initialises a new instance from the specified bytes, decoding any encoded-words.
        /// </summary>
        /// <param name="pBytes"></param>
        public cCulturedString(IList<byte> pBytes)
        {
            // consider the problem of IMAP removing the RFC 2822 quoting (see the addr-name definition in RFC 3501)
            //  consider the display name >fr€d " " fr€d< (with the 'fr€d's as encoded words)
            //   after IMAP removing the quoting the string looks like this >fr€d   fr€d<, 
            //    so if I were to ignore the spaces between the encoded words
            //     (as I should when processing unstructured text (like the subject))
            //      then I'd end up with >fr€dfr€d<
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
                    else if (lCursor.GetRFC822FWS(lPendingWSP)) lFoundEncodedWord = true;
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

        /// <summary>
        /// Initialises a new instance with the specified string.
        /// </summary>
        public cCulturedString(string pString)
        {
            if (pString == null) throw new ArgumentNullException(nameof(pString));

            if (string.IsNullOrEmpty(pString))
            {
                Parts = null;
                return;
            }

            List<cCulturedStringPart> lParts = new List<cCulturedStringPart>();
            lParts.Add(new cCulturedStringPart(pString, null));
            Parts = new ReadOnlyCollection<cCulturedStringPart>(lParts);
        }

        /// <inheritdoc cref="iCanComposeHeaderFieldValue.CanComposeHeaderFieldValue(bool)"/>
        public bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed)
        {
            // only checks for invalid characters
            if (Parts is null) return true;
            foreach (var lPart in Parts) if (!cCharset.WSPVChar.ContainsAll(lPart.String)) return false;
            return true;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Parts != null) foreach (var lPart in Parts) if (lPart == null) throw new cDeserialiseException(nameof(cCulturedString), nameof(Parts), kDeserialiseExceptionMessage.ContainsNulls);
        }





        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cCulturedString pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo"/>
        public int CompareTo(cCulturedString pOther)
        {
            if (pOther == null) return 1;

            int lCompareTo = cTools.CompareToNullableFields(Parts, pOther.Parts);
            if (lCompareTo != 0) return lCompareTo;

            int lMinCount = Math.Min(Parts.Count, pOther.Parts.Count);

            for (int i = 0; i < lMinCount; i++)
            {
                lCompareTo = Parts[i].CompareTo(pOther.Parts[i]);
                if (lCompareTo != 0) return lCompareTo;
            }

            return Parts.Count.CompareTo(pOther.Parts.Count);
        }

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cCulturedString;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                if (Parts != null) foreach (var lPart in Parts) lHash = lHash * 23 + lPart.GetHashCode();
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

        /// <inheritdoc cref="ToString"/>
        public static implicit operator string(cCulturedString pString) => pString?.ToString();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cCulturedString pA, cCulturedString pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            lReferenceEquals = cTools.EqualsReferenceEquals(pA.Parts, pB.Parts);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            if (pA.Parts.Count != pB.Parts.Count) return false;
            for (int i = 0; i < pA.Parts.Count; i++) if (pA.Parts[i] != pB.Parts[i]) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cCulturedString pA, cCulturedString pB) => !(pA == pB);

    }

    /// <summary>
    /// Represents part of a string that may include language information as per RFC 2231.
    /// </summary>
    /// <seealso cref="cCulturedString"/>
    [Serializable]
    public class cCulturedStringPart : IEquatable<cCulturedStringPart>, IComparable<cCulturedStringPart>
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cCulturedStringPart pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo"/>
        public int CompareTo(cCulturedStringPart pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo = String.CompareTo(pOther.String);
            if (lCompareTo != 0) return lCompareTo;
            lCompareTo = cTools.CompareToNullableFields(LanguageTag, pOther.LanguageTag);
            if (lCompareTo != 0) return lCompareTo;
            return LanguageTag.CompareTo(pOther.LanguageTag);
        }

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
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.String == pB.String && pA.LanguageTag == pB.LanguageTag;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cCulturedStringPart pA, cCulturedStringPart pB) => !(pA == pB);
    }
}