using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;
using work.bacome.imapinternals;

namespace work.bacome.imapclient
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

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Parts != null) foreach (var lPart in Parts) if (lPart == null) throw new cDeserialiseException(nameof(cCulturedString), nameof(Parts), kDeserialiseExceptionMessage.ContainsNulls);
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