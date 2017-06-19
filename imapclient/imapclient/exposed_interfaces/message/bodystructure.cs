using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cBodyStructure
    {
        public enum ePartTypeCode
        {
            unknown,
            text,
            image,
            audio,
            video,
            application,
            multipart,
            message
        }

        public enum eDispositionTypeCode
        {
            unknown,
            inline,
            attachment
        }

        protected const string kTypeMultipart = "MULTIPART";
        public const string TypeMessage = "MESSAGE";
        public const string TypeText = "TEXT";
        public const string SubTypeRFC822 = "rfc822";

        public readonly string Type;
        public readonly ePartTypeCode TypeCode;
        public readonly string SubType;
        public readonly string Part; // will be null for the top level part in a multipart bodystructure 

        public cBodyStructure(string pType, string pSubType, string pPart)
        {
            if (pType == null) throw new ArgumentNullException(nameof(pType));
            if (pSubType == null) throw new ArgumentNullException(nameof(pSubType));

            Type = pType.ToUpperInvariant();

            if (Type.Equals(TypeText)) TypeCode = ePartTypeCode.text;
            else if (Type.Equals("IMAGE")) TypeCode = ePartTypeCode.image;
            else if (Type.Equals("AUDIO")) TypeCode = ePartTypeCode.audio;
            else if (Type.Equals("VIDEO")) TypeCode = ePartTypeCode.video;
            else if (Type.Equals("APPLICATION")) TypeCode = ePartTypeCode.application;
            else if (Type.Equals(kTypeMultipart)) TypeCode = ePartTypeCode.multipart;
            else if (Type.Equals(TypeMessage)) TypeCode = ePartTypeCode.message;
            else TypeCode = ePartTypeCode.unknown;

            SubType = pSubType.ToUpperInvariant();
            Part = pPart;
        }

        public abstract cDisposition Disposition { get; }
        public abstract cStrings Languages { get; }
        public abstract string Location { get; }
        public abstract cExtensionValue.cValues ExtensionValues { get; }

        public override string ToString() => $"{nameof(cBodyStructure)}({Type},{TypeCode},{SubType},{Part})";

        public class cParts : ReadOnlyCollection<cBodyStructure>
        {
            public cParts(IList<cBodyStructure> pParts) : base(pParts) { }

            public override string ToString()
            {
                var lBuilder = new cListBuilder(nameof(cParts));
                foreach (var lPart in this) lBuilder.Append(lPart);
                return lBuilder.ToString();
            }
        }

        public abstract class cExtensionData
        {
            public readonly cDisposition Disposition;
            public readonly cStrings Languages;
            public readonly string Location;
            public readonly cExtensionValue.cValues ExtensionValues;

            public cExtensionData(cDisposition pDisposition, cStrings pLanguages, string pLocation, cExtensionValue.cValues pExtensionValues)
            {
                Disposition = pDisposition;
                Languages = pLanguages;
                Location = pLocation;
                ExtensionValues = pExtensionValues;
            }

            public override string ToString() => $"{nameof(cExtensionData)}({Disposition},{Languages},{Location},{ExtensionValues})";

            public class cMulti : cExtensionData
            {
                public readonly cParameters Parameters;

                public cMulti(cParameters pParameters, cDisposition pDisposition, cStrings pLanguages, string pLocation, cExtensionValue.cValues pExtensionValues) : base(pDisposition, pLanguages, pLocation, pExtensionValues)
                {
                    Parameters = pParameters;
                }

                public override string ToString() => $"{nameof(cMulti)}({base.ToString()},{Parameters})";
            }

            public class cSingle : cExtensionData
            {
                public readonly string MD5;

                public cSingle(string pMD5, cDisposition pDisposition, cStrings pLanguages, string pLocation, cExtensionValue.cValues pExtensions) : base(pDisposition, pLanguages, pLocation, pExtensions)
                {
                    MD5 = pMD5;
                }

                public override string ToString() => $"{nameof(cSingle)}({base.ToString()},{MD5})";
            }
        }

        public class cMulti : cBodyStructure
        {
            public readonly cParts Parts;
            public readonly cExtensionData.cMulti ExtensionData;

            public cMulti(IList<cBodyStructure> pParts, string pSubType, string pPart, cExtensionData.cMulti pExtensionData) : base(kTypeMultipart, pSubType, pPart)
            {
                Parts = new cParts(pParts);
                ExtensionData = pExtensionData;
            }

            public cParameters Parameters => ExtensionData.Parameters;
            public override cDisposition Disposition => ExtensionData.Disposition;
            public override cStrings Languages => ExtensionData.Languages;
            public override string Location => ExtensionData.Location;
            public override cExtensionValue.cValues ExtensionValues => ExtensionData.ExtensionValues;

            public override string ToString() => $"{nameof(cMulti)}({base.ToString()},{Parts},{ExtensionData})";
        }

        public class cDisposition
        {
            // rfc 2183

            public readonly string Type;
            public readonly eDispositionTypeCode TypeCode;
            public readonly cParameters Parameters;

            public cDisposition(string pType, cParameters pParameters)
            {
                if (pType == null) throw new ArgumentNullException(nameof(pType));

                Type = pType.ToUpperInvariant();

                if (Type.Equals("INLINE")) TypeCode = eDispositionTypeCode.inline;
                else if (Type.Equals("ATTACHMENT")) TypeCode = eDispositionTypeCode.attachment;
                else TypeCode = eDispositionTypeCode.unknown;

                Parameters = pParameters;
            }

            public string FileName => Parameters.GetStringValue("filename");

            public DateTime? CreationDate => Parameters.GetDateTimeValue("creation-date");
            public DateTime? ModificationDate => Parameters.GetDateTimeValue("modification-date");
            public DateTime? ReadDate => Parameters.GetDateTimeValue("read-date");

            public uint? Size => Parameters.GetUIntValue("size");

            public override string ToString() => $"{nameof(cDisposition)}({Type},{TypeCode},{Parameters})";
        }

        public class cSingle : cBodyStructure
        {
            public readonly cParameters Parameters;
            public readonly string ContentId;
            public readonly cCulturedString Description; // decoded (the source may contain encoded words)
            public readonly string Encoding;
            public readonly eDecodingRequired DecodingRequired;
            public readonly uint SizeInBytes;
            public readonly cExtensionData.cSingle ExtensionData;

            public cSingle(string pType, string pSubType, string pPart, cParameters pParameters, string pContentId, cCulturedString pDescription, string pEncoding, uint pSizeInBytes, cExtensionData.cSingle pExtensionData) : base(pType, pSubType, pPart)
            {
                if (pEncoding == null) throw new ArgumentNullException(nameof(pEncoding));

                Parameters = pParameters;
                ContentId = pContentId;
                Description = pDescription;
                Encoding = pEncoding.ToUpperInvariant();

                if (Encoding.Equals("7BIT")) DecodingRequired = eDecodingRequired.none;
                else if (Encoding.Equals("8BIT")) DecodingRequired = eDecodingRequired.none;
                else if (Encoding.Equals("BINARY")) DecodingRequired = eDecodingRequired.none;
                else if (Encoding.Equals("QUOTED-PRINTABLE")) DecodingRequired = eDecodingRequired.quotedprintable;
                else if (Encoding.Equals("BASE64")) DecodingRequired = eDecodingRequired.base64;
                else DecodingRequired = eDecodingRequired.unknown; // note that rfc 2045 section 6.4 specifies that if 'unknown' then the part has to be treated as application/octet-stream

                SizeInBytes = pSizeInBytes;
                ExtensionData = pExtensionData;
            }

            public string MD5 => ExtensionData.MD5;
            public override cDisposition Disposition => ExtensionData.Disposition;
            public override cStrings Languages => ExtensionData.Languages;
            public override string Location => ExtensionData.Location;
            public override cExtensionValue.cValues ExtensionValues => ExtensionData.ExtensionValues;

            public override string ToString() => $"{nameof(cSingle)}({base.ToString()},{Parameters},{ContentId},{Description},{Encoding},{DecodingRequired},{SizeInBytes},{ExtensionData})";

            public class cMessage : cSingle
            {
                public readonly cEnvelope Envelope;
                public readonly cBodyStructure Body;
                public readonly cBodyStructure BodyEx;
                public readonly uint SizeInLines;

                public cMessage(string pPart, cParameters pParameters, string pContentId, cCulturedString pDescription, string pEncoding, uint pSizeInBytes, cEnvelope pEnvelope, cBodyStructure pBody, cBodyStructure pBodyEx, uint pSizeInLines, cExtensionData.cSingle pExtensionData) : base(TypeMessage, SubTypeRFC822, pPart, pParameters, pContentId, pDescription, pEncoding, pSizeInBytes, pExtensionData)
                {
                    Envelope = pEnvelope;
                    Body = pBody;
                    BodyEx = pBodyEx;
                    SizeInLines = pSizeInLines;
                }

                public override string ToString() => $"{nameof(cMessage)}({base.ToString()},{Envelope},{Body},{BodyEx},{SizeInLines})";
            }

            public class cText : cSingle
            {
                public readonly uint SizeInLines;

                public cText(string pSubType, string pPart, cParameters pParameters, string pContentId, cCulturedString pDescription, string pEncoding, uint pSizeInBytes, uint pSizeInLines, cExtensionData.cSingle pExtensionData) : base(TypeText, pSubType, pPart, pParameters, pContentId, pDescription, pEncoding, pSizeInBytes, pExtensionData)
                {
                    SizeInLines = pSizeInLines;
                }

                public override string ToString() => $"{nameof(cText)}({base.ToString()},{SizeInLines})";
            }
        }

        public class cParameterValue
        {
            public readonly bool I18N; // true if the parameter was in rfc2231 format
            public readonly string Value;
            public readonly string LanguageTag; // not null if a language tag was present, uppercased

            public cParameterValue(string pValue)
            {
                I18N = false;
                Value = pValue;
                LanguageTag = null;
            }

            public cParameterValue(string pValue, string pLanguageTag)
            {
                I18N = true;
                Value = pValue;

                if (pLanguageTag == null) LanguageTag = null;
                else LanguageTag = pLanguageTag.ToUpperInvariant();
            }

            public override string ToString() => $"{nameof(cParameterValue)}({I18N},{Value},{LanguageTag})";
        }

        public class cParameters : ReadOnlyDictionary<string, cParameterValue>
        {
            public cParameters(Dictionary<string, cParameterValue> pDictionary) : base(pDictionary) { }

            public string GetStringValue(string pParameterName, string pDefault = null)
            {
                if (!TryGetValue(pParameterName, out cParameterValue lValue)) return pDefault;
                if (lValue.I18N) return null;
                return lValue.Value;
            }

            public uint? GetUIntValue(string pParameterName, uint? pDefault = null)
            {
                if (!TryGetValue(pParameterName, out cParameterValue lValue)) return pDefault;
                if (lValue.I18N) return null;
                if (uint.TryParse(lValue.Value, out var lResult)) return lResult;
                return null;
            }

            public DateTime? GetDateTimeValue(string pParameterName, DateTime? pDefault = null)
            {
                if (!TryGetValue(pParameterName, out cParameterValue lValue)) return pDefault;
                if (lValue.I18N) return null;
                if (!cBytesCursor.TryConstruct(lValue.Value, out var lCursor)) return null;
                if (lCursor.GetRFC822DateTime(out var lResult) && lCursor.Position.AtEnd) return lResult;
                return null;
            }

            public override string ToString()
            {
                var lBuilder = new cListBuilder(nameof(cParameters));
                foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
                return lBuilder.ToString();
            }
        }

        public abstract class cExtensionValue
        {
            public class cString : cExtensionValue
            {
                public string String;
                public cString(string pString) { String = pString; }
                public override string ToString() => $"{nameof(cString)}({String})";
            }

            public class cNumber : cExtensionValue
            {
                public uint Number;
                public cNumber(uint pNumber) { Number = pNumber; }
                public override string ToString() => $"{nameof(cNumber)}({Number})";
            }

            public class cValues : cExtensionValue, IEnumerable<cExtensionValue>
            {
                public ReadOnlyCollection<cExtensionValue> Values;
                public cValues(IList<cExtensionValue> pValues) { Values = new ReadOnlyCollection<cExtensionValue>(pValues); }
                public IEnumerator<cExtensionValue> GetEnumerator() => Values.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cValues));
                    foreach (var lValue in Values) lBuilder.Append(lValue);
                    return lBuilder.ToString();
                }
            }
        }
    }
}