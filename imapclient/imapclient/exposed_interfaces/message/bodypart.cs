﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public enum eBodyPartTypeCode
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

    public abstract class cBodyPart
    {
        protected const string kTypeMultipart = "MULTIPART";
        public const string TypeMessage = "MESSAGE";
        public const string TypeText = "TEXT";
        public const string SubTypeRFC822 = "rfc822";

        public readonly string Type;
        public readonly eBodyPartTypeCode TypeCode;
        public readonly string SubType;
        public readonly string Part; // will be null for the top level part in a multipart bodystructure 

        public cBodyPart(string pType, string pSubType, string pPart)
        {
            if (pType == null) throw new ArgumentNullException(nameof(pType));
            if (pSubType == null) throw new ArgumentNullException(nameof(pSubType));

            Type = pType.ToUpperInvariant();

            if (Type.Equals(TypeText)) TypeCode = eBodyPartTypeCode.text;
            else if (Type.Equals("IMAGE")) TypeCode = eBodyPartTypeCode.image;
            else if (Type.Equals("AUDIO")) TypeCode = eBodyPartTypeCode.audio;
            else if (Type.Equals("VIDEO")) TypeCode = eBodyPartTypeCode.video;
            else if (Type.Equals("APPLICATION")) TypeCode = eBodyPartTypeCode.application;
            else if (Type.Equals(kTypeMultipart)) TypeCode = eBodyPartTypeCode.multipart;
            else if (Type.Equals(TypeMessage)) TypeCode = eBodyPartTypeCode.message;
            else TypeCode = eBodyPartTypeCode.unknown;

            SubType = pSubType.ToUpperInvariant();
            Part = pPart;
        }

        public abstract cBodyPartDisposition Disposition { get; }
        public abstract cStrings Languages { get; }
        public abstract string Location { get; }
        public abstract cBodyPartExtensionValues ExtensionValues { get; }

        public override string ToString() => $"{nameof(cBodyPart)}({Type},{TypeCode},{SubType},{Part})";
    }

    public abstract class cBodyPartExtensionValue { }

    public class cBodyPartExtensionString : cBodyPartExtensionValue
    {
        public string String;
        public cBodyPartExtensionString(string pString) { String = pString; }
        public override string ToString() => $"{nameof(cBodyPartExtensionString)}({String})";
    }

    public class cBodyPartExtensionNumber : cBodyPartExtensionValue
    {
        public uint Number;
        public cBodyPartExtensionNumber(uint pNumber) { Number = pNumber; }
        public override string ToString() => $"{nameof(cBodyPartExtensionNumber)}({Number})";
    }

    public class cBodyPartExtensionValues : cBodyPartExtensionValue, IEnumerable<cBodyPartExtensionValue>
    {
        public ReadOnlyCollection<cBodyPartExtensionValue> Values;
        public cBodyPartExtensionValues(IList<cBodyPartExtensionValue> pValues) { Values = new ReadOnlyCollection<cBodyPartExtensionValue>(pValues); }
        public IEnumerator<cBodyPartExtensionValue> GetEnumerator() => Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cBodyPartExtensionValues));
            foreach (var lValue in Values) lBuilder.Append(lValue);
            return lBuilder.ToString();
        }
    }

    public class cBodyParts : ReadOnlyCollection<cBodyPart>
    {
        public cBodyParts(IList<cBodyPart> pParts) : base(pParts) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBodyParts));
            foreach (var lPart in this) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    public abstract class cBodyPartExtensionData
    {
        public readonly cBodyPartDisposition Disposition;
        public readonly cStrings Languages;
        public readonly string Location;
        public readonly cBodyPartExtensionValues ExtensionValues;

        public cBodyPartExtensionData(cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensionValues)
        {
            Disposition = pDisposition;
            Languages = pLanguages;
            Location = pLocation;
            ExtensionValues = pExtensionValues;
        }

        public override string ToString() => $"{nameof(cBodyPartExtensionData)}({Disposition},{Languages},{Location},{ExtensionValues})";
    }

    public class cMultiPartExtensionData : cBodyPartExtensionData
    {
        public readonly cBodyPartParameters Parameters;

        public cMultiPartExtensionData(cBodyPartParameters pParameters, cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensionValues) : base(pDisposition, pLanguages, pLocation, pExtensionValues)
        {
            Parameters = pParameters;
        }

        public override string ToString() => $"{nameof(cMultiPartExtensionData)}({base.ToString()},{Parameters})";
    }

    public class cSinglePartExtensionData : cBodyPartExtensionData
    {
        public readonly string MD5;

        public cSinglePartExtensionData(string pMD5, cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensions) : base(pDisposition, pLanguages, pLocation, pExtensions)
        {
            MD5 = pMD5;
        }

        public override string ToString() => $"{nameof(cSinglePartExtensionData)}({base.ToString()},{MD5})";
    }

    public class cMultiPart : cBodyPart
    {
        public readonly cBodyParts Parts;
        public readonly cMultiPartExtensionData ExtensionData;

        public cMultiPart(IList<cBodyPart> pParts, string pSubType, string pPart, cMultiPartExtensionData pExtensionData) : base(kTypeMultipart, pSubType, pPart)
        {
            Parts = new cBodyParts(pParts);
            ExtensionData = pExtensionData;
        }

        public cBodyPartParameters Parameters => ExtensionData.Parameters;
        public override cBodyPartDisposition Disposition => ExtensionData.Disposition;
        public override cStrings Languages => ExtensionData.Languages;
        public override string Location => ExtensionData.Location;
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData.ExtensionValues;

        public override string ToString() => $"{nameof(cMultiPart)}({base.ToString()},{Parts},{ExtensionData})";
    }

    public class cBodyPartDisposition
    {
        // rfc 2183

        public readonly string Type;
        public readonly eDispositionTypeCode TypeCode;
        public readonly cBodyPartParameters Parameters;

        public cBodyPartDisposition(string pType, cBodyPartParameters pParameters)
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

        public override string ToString() => $"{nameof(cBodyPartDisposition)}({Type},{TypeCode},{Parameters})";
    }

    public class cSinglePart : cBodyPart
    {
        public readonly cBodyPartParameters Parameters;
        public readonly string ContentId;
        public readonly cCulturedString Description; // decoded (the source may contain encoded words)
        public readonly string Encoding;
        public readonly eDecodingRequired DecodingRequired;
        public readonly uint SizeInBytes;
        public readonly cSinglePartExtensionData ExtensionData;

        public cSinglePart(string pType, string pSubType, string pPart, cBodyPartParameters pParameters, string pContentId, cCulturedString pDescription, string pEncoding, uint pSizeInBytes, cSinglePartExtensionData pExtensionData) : base(pType, pSubType, pPart)
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
        public override cBodyPartDisposition Disposition => ExtensionData.Disposition;
        public override cStrings Languages => ExtensionData.Languages;
        public override string Location => ExtensionData.Location;
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData.ExtensionValues;

        public override string ToString() => $"{nameof(cSinglePart)}({base.ToString()},{Parameters},{ContentId},{Description},{Encoding},{DecodingRequired},{SizeInBytes},{ExtensionData})";
    }

    public class cMessagePart : cSinglePart
    {
        public readonly cEnvelope Envelope;
        public readonly cBodyPart Body;
        public readonly cBodyPart BodyEx;
        public readonly uint SizeInLines;

        public cMessagePart(string pPart, cBodyPartParameters pParameters, string pContentId, cCulturedString pDescription, string pEncoding, uint pSizeInBytes, cEnvelope pEnvelope, cBodyPart pBody, cBodyPart pBodyEx, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(TypeMessage, SubTypeRFC822, pPart, pParameters, pContentId, pDescription, pEncoding, pSizeInBytes, pExtensionData)
        {
            Envelope = pEnvelope;
            Body = pBody;
            BodyEx = pBodyEx;
            SizeInLines = pSizeInLines;
        }

        public override string ToString() => $"{nameof(cMessagePart)}({base.ToString()},{Envelope},{Body},{BodyEx},{SizeInLines})";
    }

    public class cTextPart : cSinglePart
    {
        public readonly uint SizeInLines;

        public cTextPart(string pSubType, string pPart, cBodyPartParameters pParameters, string pContentId, cCulturedString pDescription, string pEncoding, uint pSizeInBytes, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(TypeText, pSubType, pPart, pParameters, pContentId, pDescription, pEncoding, pSizeInBytes, pExtensionData)
        {
            SizeInLines = pSizeInLines;
        }

        public override string ToString() => $"{nameof(cTextPart)}({base.ToString()},{SizeInLines})";
    }

    public class cBodyPartParameterValue
    {
        public readonly bool I18N; // true if the parameter was in rfc2231 format
        public readonly string Value;
        public readonly string LanguageTag; // not null if a language tag was present, uppercased

        public cBodyPartParameterValue(string pValue)
        {
            I18N = false;
            Value = pValue;
            LanguageTag = null;
        }

        public cBodyPartParameterValue(string pValue, string pLanguageTag)
        {
            I18N = true;
            Value = pValue;

            if (pLanguageTag == null) LanguageTag = null;
            else LanguageTag = pLanguageTag.ToUpperInvariant();
        }

        public override string ToString() => $"{nameof(cBodyPartParameterValue)}({I18N},{Value},{LanguageTag})";
    }

    public class cBodyPartParameters : ReadOnlyDictionary<string, cBodyPartParameterValue>
    {
        public cBodyPartParameters(Dictionary<string, cBodyPartParameterValue> pDictionary) : base(pDictionary) { }

        public string GetStringValue(string pParameterName, string pDefault = null)
        {
            if (!TryGetValue(pParameterName, out cBodyPartParameterValue lValue)) return pDefault;
            if (lValue.I18N) return null;
            return lValue.Value;
        }

        public uint? GetUIntValue(string pParameterName, uint? pDefault = null)
        {
            if (!TryGetValue(pParameterName, out cBodyPartParameterValue lValue)) return pDefault;
            if (lValue.I18N) return null;
            if (uint.TryParse(lValue.Value, out var lResult)) return lResult;
            return null;
        }

        public DateTime? GetDateTimeValue(string pParameterName, DateTime? pDefault = null)
        {
            if (!TryGetValue(pParameterName, out cBodyPartParameterValue lValue)) return pDefault;
            if (lValue.I18N) return null;
            if (!cBytesCursor.TryConstruct(lValue.Value, out var lCursor)) return null;
            if (lCursor.GetRFC822DateTime(out var lResult) && lCursor.Position.AtEnd) return lResult;
            return null;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBodyPartParameters));
            foreach (var lFieldValue in this) lBuilder.Append(lFieldValue.Key, lFieldValue.Value);
            return lBuilder.ToString();
        }
    }
}