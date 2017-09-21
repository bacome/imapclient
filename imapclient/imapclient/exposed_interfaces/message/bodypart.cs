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

    public enum eTextBodyPartSubTypeCode
    {
        unknown,
        plain,
        html
    }

    public enum eMultiPartBodySubTypeCode
    {
        unknown,
        mixed,
        digest,
        alternative,
        related
    }

    public abstract class cBodyPart
    {
        protected const string kTypeMultipart = "MuLtIpArT";
        protected const string kTypeMessage = "MeSsAgE";
        protected const string kTypeText = "TeXt";
        protected const string kSubTypeRFC822 = "RfC822";

        public readonly string Type;
        public readonly string SubType;
        public readonly cSection Section;
        public readonly eBodyPartTypeCode TypeCode;

        public cBodyPart(string pType, string pSubType, cSection pSection)
        {
            Type = pType ?? throw new ArgumentNullException(nameof(pType));
            SubType = pSubType ?? throw new ArgumentNullException(nameof(pSubType));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));

            if (Type.Equals(kTypeText, StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.text;
            else if (Type.Equals("IMAGE", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.image;
            else if (Type.Equals("AUDIO", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.audio;
            else if (Type.Equals("VIDEO", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.video;
            else if (Type.Equals("APPLICATION", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.application;
            else if (Type.Equals(kTypeMultipart, StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.multipart;
            else if (Type.Equals(kTypeMessage, StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.message;
            else TypeCode = eBodyPartTypeCode.unknown;
        }

        public abstract cBodyPartDisposition Disposition { get; }
        public abstract cStrings Languages { get; }
        public abstract string Location { get; }
        public abstract cBodyPartExtensionValues ExtensionValues { get; }

        public override string ToString() => $"{nameof(cBodyPart)}({Type},{SubType},{Section},{TypeCode})";
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

    public class cMultiPartBody : cBodyPart
    {
        public readonly cBodyParts Parts;
        public readonly eMultiPartBodySubTypeCode SubTypeCode;
        public readonly cMultiPartExtensionData ExtensionData;

        public cMultiPartBody(IList<cBodyPart> pParts, string pSubType, cSection pSection, cMultiPartExtensionData pExtensionData) : base(kTypeMultipart, pSubType, pSection)
        {
            Parts = new cBodyParts(pParts);

            if (SubType == "MIXED") SubTypeCode = eMultiPartBodySubTypeCode.mixed;
            else if (SubType == "DIGEST") SubTypeCode = eMultiPartBodySubTypeCode.digest;
            else if (SubType == "ALTERNATIVE") SubTypeCode = eMultiPartBodySubTypeCode.alternative;
            else if (SubType == "RELATED") SubTypeCode = eMultiPartBodySubTypeCode.related;
            else SubTypeCode = eMultiPartBodySubTypeCode.unknown;

            ExtensionData = pExtensionData;
        }

        public cBodyPartParameters Parameters => ExtensionData?.Parameters;
        public override cBodyPartDisposition Disposition => ExtensionData?.Disposition;
        public override cStrings Languages => ExtensionData?.Languages;
        public override string Location => ExtensionData?.Location;
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData?.ExtensionValues;

        public override string ToString() => $"{nameof(cMultiPartBody)}({base.ToString()},{Parts},{ExtensionData})";
    }

    public class cBodyPartDisposition
    {
        // rfc 2183

        public readonly string Type;
        public readonly eDispositionTypeCode TypeCode;
        public readonly cBodyPartParameters Parameters;

        public cBodyPartDisposition(string pType, cBodyPartParameters pParameters)
        {
            Type = pType ?? throw new ArgumentNullException(nameof(pType));
            Parameters = pParameters;

            if (Type.Equals("INLINE", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eDispositionTypeCode.inline;
            else if (Type.Equals("ATTACHMENT", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eDispositionTypeCode.attachment;
            else TypeCode = eDispositionTypeCode.unknown;

        }

        public string FileName => Parameters?.GetStringValue("filename");

        public DateTime? CreationDate => Parameters?.GetDateTimeValue("creation-date");
        public DateTime? ModificationDate => Parameters?.GetDateTimeValue("modification-date");
        public DateTime? ReadDate => Parameters?.GetDateTimeValue("read-date");

        public int? Size
        {
            get
            {
                uint? lSize = Parameters?.GetUIntValue("size");
                if (lSize == null) return null;
                return (int)lSize.Value;
            }
        }

        public override string ToString() => $"{nameof(cBodyPartDisposition)}({Type},{Parameters},{TypeCode})";
    }

    public class cSinglePartBody : cBodyPart
    {
        public readonly cBodyPartParameters Parameters;
        public readonly string ContentId;
        public readonly cCulturedString Description; // decoded (the source may contain encoded words)
        public readonly string ContentTransferEncoding;
        public readonly eDecodingRequired DecodingRequired;
        public readonly uint SizeInBytes;
        public readonly cSinglePartExtensionData ExtensionData;

        public cSinglePartBody(string pType, string pSubType, cSection pSection, cBodyPartParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, cSinglePartExtensionData pExtensionData) : base(pType, pSubType, pSection)
        {
            Parameters = pParameters;
            ContentId = pContentId;
            Description = pDescription;
            ContentTransferEncoding = pContentTransferEncoding ?? throw new ArgumentNullException(nameof(pContentTransferEncoding));

            if (ContentTransferEncoding.Equals("7BIT", StringComparison.InvariantCultureIgnoreCase)) DecodingRequired = eDecodingRequired.none;
            else if (ContentTransferEncoding.Equals("8BIT", StringComparison.InvariantCultureIgnoreCase)) DecodingRequired = eDecodingRequired.none;
            else if (ContentTransferEncoding.Equals("BINARY", StringComparison.InvariantCultureIgnoreCase)) DecodingRequired = eDecodingRequired.none;
            else if (ContentTransferEncoding.Equals("QUOTED-PRINTABLE", StringComparison.InvariantCultureIgnoreCase)) DecodingRequired = eDecodingRequired.quotedprintable;
            else if (ContentTransferEncoding.Equals("BASE64", StringComparison.InvariantCultureIgnoreCase)) DecodingRequired = eDecodingRequired.base64;
            else DecodingRequired = eDecodingRequired.unknown; // note that rfc 2045 section 6.4 specifies that if 'unknown' then the part has to be treated as application/octet-stream

            SizeInBytes = pSizeInBytes;
            ExtensionData = pExtensionData;
        }

        public string MD5 => ExtensionData?.MD5;
        public override cBodyPartDisposition Disposition => ExtensionData?.Disposition;
        public override cStrings Languages => ExtensionData?.Languages;
        public override string Location => ExtensionData?.Location;
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData?.ExtensionValues;

        public override string ToString() => $"{nameof(cSinglePartBody)}({base.ToString()},{Parameters},{ContentId},{Description},{ContentTransferEncoding},{DecodingRequired},{SizeInBytes},{ExtensionData})";
    }

    public class cMessageBodyPart : cSinglePartBody
    {
        public readonly cEnvelope Envelope;
        private readonly cBodyPart mBody;
        public readonly cBodyPart BodyStructure;
        public readonly uint SizeInLines;

        public cMessageBodyPart(cSection pSection, cBodyPartParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, cEnvelope pEnvelope, cBodyPart pBody, cBodyPart pBodyStructure, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(TypeMessage, SubTypeRFC822, pSection, pParameters, pContentId, pDescription, pContentTransferEncoding, pSizeInBytes, pExtensionData)
        {
            Envelope = pEnvelope;
            mBody = pBody;
            BodyStructure = pBodyStructure;
            SizeInLines = pSizeInLines;
        }

        public cBodyPart Body => mBody ?? BodyStructure;

        public override string ToString() => $"{nameof(cMessageBodyPart)}({base.ToString()},{Envelope},{BodyStructure ?? mBody},{SizeInLines})";
    }

    public class cTextBodyPart : cSinglePartBody
    {
        public readonly eTextBodyPartSubTypeCode SubTypeCode;
        public readonly uint SizeInLines;

        public cTextBodyPart(string pSubType, cSection pSection, cBodyPartParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(TypeText, pSubType, pSection, pParameters, pContentId, pDescription, pContentTransferEncoding, pSizeInBytes, pExtensionData)
        {
            if (SubType == "PLAIN") SubTypeCode = eTextBodyPartSubTypeCode.plain;
            else if (SubType == "HTML") SubTypeCode = eTextBodyPartSubTypeCode.html;
            else SubTypeCode = eTextBodyPartSubTypeCode.unknown;

            SizeInLines = pSizeInLines;
        }

        public string Charset => Parameters.GetStringValue("charset", "us-ascii");

        public override string ToString() => $"{nameof(cTextBodyPart)}({base.ToString()},{SizeInLines})";
    }

    public class cBodyPartParameterValue
    {
        public readonly bool I18N; // true if the parameter was in rfc2231 format
        public readonly string Value;
        public readonly string LanguageTag; // not null if a language tag was present

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
            LanguageTag = pLanguageTag;
        }

        public override string ToString() => $"{nameof(cBodyPartParameterValue)}({I18N},{Value},{LanguageTag})";
    }

    public class cBodyPartParameters : ReadOnlyDictionary<string, cBodyPartParameterValue>
    {
        ;?;
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