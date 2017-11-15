using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The RFC 2045 MIME type of a message part.
    /// </summary>
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

    /// <summary>
    /// The RFC 2183 disposition type of a message part.
    /// </summary>
    public enum eDispositionTypeCode
    {
        unknown,
        inline,
        attachment
    }

    /// <summary>
    /// The RFC 2045 MIME subtype of a text message part.
    /// </summary>
    public enum eTextBodyPartSubTypeCode
    {
        unknown,
        plain,
        html
    }

    /// <summary>
    /// The RFC 2045 MIME subtype of a multipart message part.
    /// </summary>
    public enum eMultiPartBodySubTypeCode
    {
        unknown,
        mixed,
        digest,
        alternative,
        related
    }

    /// <summary>
    /// Contains named MIME type constants.
    /// </summary>
    public static class kMimeType
    {
        public const string Multipart = "Multipart";
        public const string Message = "Message";
        public const string Text = "Text";
    }

    /// <summary>
    /// Contains named MIME subtype constants.
    /// </summary>
    public static class kMimeSubType
    {
        public const string RFC822 = "RFC822";
    }

    /// <summary>
    /// Represents a message part.
    /// </summary>
    /// <remarks>
    /// <para>Will be one of;
    /// <list type="bullet">
    /// <item><see cref="cMultiPartBody"/></item>
    /// <item><see cref="cSinglePartBody"/></item>
    /// <item><see cref="cMessageBodyPart"/></item>
    /// <item><see cref="cTextBodyPart"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract class cBodyPart
    {
        /// <summary>
        /// The MIME type of the part in text form.
        /// </summary>
        public readonly string Type;

        /// <summary>
        /// The MIME subtype of the part in text form.
        /// </summary>
        public readonly string SubType;

        /// <summary>
        /// The IMAP section identifier of the part.
        /// </summary>
        public readonly cSection Section;

        /// <summary>
        /// The MIME type of the part in code form.
        /// </summary>
        public readonly eBodyPartTypeCode TypeCode;

        internal cBodyPart(string pType, string pSubType, cSection pSection)
        {
            Type = pType ?? throw new ArgumentNullException(nameof(pType));
            SubType = pSubType ?? throw new ArgumentNullException(nameof(pSubType));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));

            if (Type.Equals(kMimeType.Text, StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.text;
            else if (Type.Equals("IMAGE", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.image;
            else if (Type.Equals("AUDIO", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.audio;
            else if (Type.Equals("VIDEO", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.video;
            else if (Type.Equals("APPLICATION", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.application;
            else if (Type.Equals(kMimeType.Multipart, StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.multipart;
            else if (Type.Equals(kMimeType.Message, StringComparison.InvariantCultureIgnoreCase)) TypeCode = eBodyPartTypeCode.message;
            else TypeCode = eBodyPartTypeCode.unknown;
        }

        /// <summary>
        /// The disposition of the part.
        /// </summary>
        public abstract cBodyPartDisposition Disposition { get; }

        /// <summary>
        /// The language(s) of the part.
        /// </summary>
        public abstract cStrings Languages { get; }

        /// <summary>
        /// The location URI of the part.
        /// </summary>
        public abstract string Location { get; }

        /// <summary>
        /// Any additional extension data for the part.
        /// </summary>
        public abstract cBodyPartExtensionValues ExtensionValues { get; }

        public override string ToString() => $"{nameof(cBodyPart)}({Type},{SubType},{Section},{TypeCode})";
    }

    /// <summary>
    /// <para>Represents an additional extension data element.</para>
    /// <para>Will be one of;
    /// <list type="bullet">
    /// <item><see cref="cBodyPartExtensionString"/></item>
    /// <item><see cref="cBodyPartExtensionNumber"/></item>
    /// <item><see cref="cBodyPartExtensionValues"/></item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract class cBodyPartExtensionValue { }

    /// <summary>
    /// A string extension data element.
    /// </summary>
    public class cBodyPartExtensionString : cBodyPartExtensionValue
    {
        public string String;
        public cBodyPartExtensionString(string pString) { String = pString; }
        public override string ToString() => $"{nameof(cBodyPartExtensionString)}({String})";
    }

    /// <summary>
    /// A numeric extension data element.
    /// </summary>
    public class cBodyPartExtensionNumber : cBodyPartExtensionValue
    {
        public uint Number;
        public cBodyPartExtensionNumber(uint pNumber) { Number = pNumber; }
        public override string ToString() => $"{nameof(cBodyPartExtensionNumber)}({Number})";
    }

    /// <summary>
    /// A collection of extension data elements
    /// </summary>
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

    /// <summary>
    /// A collection of message parts.
    /// </summary>
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

    // TODO ;?; // fill in the "see"




    /// <summary>
    /// IMAP bodystructure extension data.
    /// </summary>
    /// <remarks>
    /// <para>Will be one of;
    /// <list type="bullet">
    /// <item><see cref="cMultiPartExtensionData"/></item>
    /// <item><see cref="cSinglePartExtensionData"/></item>
    /// </list>
    /// </para>
    /// <para>See </para>
    /// </remarks>
    public abstract class cBodyPartExtensionData
    {
        /// <summary>
        /// The disposition of the part.
        /// </summary>
        public readonly cBodyPartDisposition Disposition;

        /// <summary>
        /// The language(s) of the part.
        /// </summary>
        public readonly cStrings Languages;

        /// <summary>
        /// The location URI of the part.
        /// </summary>
        public readonly string Location;

        /// <summary>
        /// Any additional extension data for the part.
        /// </summary>
        public readonly cBodyPartExtensionValues ExtensionValues;

        internal cBodyPartExtensionData(cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensionValues)
        {
            Disposition = pDisposition;
            Languages = pLanguages;
            Location = pLocation;
            ExtensionValues = pExtensionValues;
        }

        public override string ToString() => $"{nameof(cBodyPartExtensionData)}({Disposition},{Languages},{Location},{ExtensionValues})";
    }

    /// <summary>
    /// The IMAP bodystructure extension data of a multipart part message part.
    /// </summary>
    public class cMultiPartExtensionData : cBodyPartExtensionData
    {
        /// <summary>
        /// The MIME type parameters of the part.
        /// </summary>
        public readonly cBodyStructureParameters Parameters;

        public cMultiPartExtensionData(cBodyStructureParameters pParameters, cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensionValues) : base(pDisposition, pLanguages, pLocation, pExtensionValues)
        {
            Parameters = pParameters;
        }

        public override string ToString() => $"{nameof(cMultiPartExtensionData)}({base.ToString()},{Parameters})";
    }

    /// <summary>
    /// The IMAP bodystructure extension data of a single part message part.
    /// </summary>
    public class cSinglePartExtensionData : cBodyPartExtensionData
    {
        public readonly string MD5;

        public cSinglePartExtensionData(string pMD5, cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensions) : base(pDisposition, pLanguages, pLocation, pExtensions)
        {
            MD5 = pMD5;
        }

        public override string ToString() => $"{nameof(cSinglePartExtensionData)}({base.ToString()},{MD5})";
    }

    /// <summary>
    /// Represents a multipart message part.
    /// </summary>
    public class cMultiPartBody : cBodyPart
    {
        /// <summary>
        /// The contained parts.
        /// </summary>
        public readonly cBodyParts Parts;

        /// <summary>
        /// The MIME subtype of the part in code form.
        /// </summary>
        public readonly eMultiPartBodySubTypeCode SubTypeCode;

        /// <summary>
        /// The IMAP bodystructure extension data for the part.
        /// </summary>
        public readonly cMultiPartExtensionData ExtensionData;

        public cMultiPartBody(IList<cBodyPart> pParts, string pSubType, cSection pSection, cMultiPartExtensionData pExtensionData) : base(kMimeType.Multipart, pSubType, pSection)
        {
            Parts = new cBodyParts(pParts);

            if (SubType.Equals("MIXED", StringComparison.InvariantCultureIgnoreCase)) SubTypeCode = eMultiPartBodySubTypeCode.mixed;
            else if (SubType.Equals("DIGEST", StringComparison.InvariantCultureIgnoreCase)) SubTypeCode = eMultiPartBodySubTypeCode.digest;
            else if (SubType.Equals("ALTERNATIVE", StringComparison.InvariantCultureIgnoreCase)) SubTypeCode = eMultiPartBodySubTypeCode.alternative;
            else if (SubType.Equals("RELATED", StringComparison.InvariantCultureIgnoreCase)) SubTypeCode = eMultiPartBodySubTypeCode.related;
            else SubTypeCode = eMultiPartBodySubTypeCode.unknown;

            ExtensionData = pExtensionData;
        }

        /// <summary>
        /// The MIME type parameters of the part.
        /// </summary>
        public cBodyStructureParameters Parameters => ExtensionData?.Parameters;

        /// <summary>
        /// The disposition of the part.
        /// </summary>
        public override cBodyPartDisposition Disposition => ExtensionData?.Disposition;

        /// <summary>
        /// The language(s) of the part.
        /// </summary>
        public override cStrings Languages => ExtensionData?.Languages;

        /// <summary>
        /// The location URI of the part.
        /// </summary>
        public override string Location => ExtensionData?.Location;

        /// <summary>
        /// Any additional extension data for the part.
        /// </summary>
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData?.ExtensionValues;

        public override string ToString() => $"{nameof(cMultiPartBody)}({base.ToString()},{Parts},{ExtensionData})";
    }

    /// <summary>
    /// RFC 2183 disposition data.
    /// </summary>
    public class cBodyPartDisposition
    {
        /// <summary>
        /// The disposition type in text form. 
        /// </summary>
        public readonly string Type;

        /// <summary>
        /// The disposition type in code form. 
        /// </summary>
        public readonly eDispositionTypeCode TypeCode;

        /// <summary>
        /// The disposition parameters.
        /// </summary>
        public readonly cBodyStructureParameters Parameters;

        public cBodyPartDisposition(string pType, cBodyStructureParameters pParameters)
        {
            Type = pType ?? throw new ArgumentNullException(nameof(pType));
            Parameters = pParameters;

            if (Type.Equals("INLINE", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eDispositionTypeCode.inline;
            else if (Type.Equals("ATTACHMENT", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eDispositionTypeCode.attachment;
            else TypeCode = eDispositionTypeCode.unknown;
        }

        /// <summary>
        /// The suggested filename if provided. May be <see langword="null"/>.
        /// </summary>
        public string FileName => Parameters?.First("filename")?.StringValue;

        /// <summary>
        /// The creation date if provided. May be <see langword="null"/>.
        /// </summary>
        public DateTime? CreationDate => Parameters?.First("creation-date")?.DateTimeValue;

        /// <summary>
        /// The modification date if provided. May be <see langword="null"/>.
        /// </summary>
        public DateTime? ModificationDate => Parameters?.First("modification-date")?.DateTimeValue;

        /// <summary>
        /// The last read date if provided. May be <see langword="null"/>.
        /// </summary>
        public DateTime? ReadDate => Parameters?.First("read-date")?.DateTimeValue;

        /// <summary>
        /// The approximate size in bytes if provided. May be <see langword="null"/>.
        /// </summary>
        public int? Size
        {
            get
            {
                uint? lSize = Parameters?.First("size")?.UIntValue;
                if (lSize == null) return null;
                return (int)lSize.Value;
            }
        }

        public override string ToString() => $"{nameof(cBodyPartDisposition)}({Type},{Parameters},{TypeCode})";
    }

    /// <summary>
    /// Represents a single part message part.
    /// </summary>
    public class cSinglePartBody : cBodyPart
    {
        /// <summary>
        /// The MIME type parameters of the part.
        /// </summary>
        public readonly cBodyStructureParameters Parameters;

        /// <summary>
        /// The MIME content-id of the part.
        /// </summary>
        public readonly string ContentId;

        /// <summary>
        /// The MIME content description of the part.
        /// </summary>
        public readonly cCulturedString Description;

        /// <summary>
        /// The MIME content transfer encoding of the part in text form.
        /// </summary>
        public readonly string ContentTransferEncoding;

        /// <summary>
        /// The MIME content transfer encoding of the part in code form.
        /// </summary>
        public readonly eDecodingRequired DecodingRequired;

        /// <summary>
        /// The size in bytes of the encoded part.
        /// </summary>
        public readonly uint SizeInBytes;

        /// <summary>
        /// The IMAP bodystructure extension data for the part.
        /// </summary>
        public readonly cSinglePartExtensionData ExtensionData;

        public cSinglePartBody(string pType, string pSubType, cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, cSinglePartExtensionData pExtensionData) : base(pType, pSubType, pSection)
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

        /// <summary>
        /// The disposition of the part.
        /// </summary>
        public override cBodyPartDisposition Disposition => ExtensionData?.Disposition;

        /// <summary>
        /// The language(s) of the part.
        /// </summary>
        public override cStrings Languages => ExtensionData?.Languages;

        /// <summary>
        /// The location URI of the part.
        /// </summary>
        public override string Location => ExtensionData?.Location;

        /// <summary>
        /// Any additional extension data for the part.
        /// </summary>
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData?.ExtensionValues;

        public override string ToString() => $"{nameof(cSinglePartBody)}({base.ToString()},{Parameters},{ContentId},{Description},{ContentTransferEncoding},{DecodingRequired},{SizeInBytes},{ExtensionData})";
    }

    /// <summary>
    /// Represents a message part that contains a message.
    /// </summary>
    public class cMessageBodyPart : cSinglePartBody
    {
        /// <summary>
        /// The IMAP envelope of the encapsulated message.
        /// </summary>
        public readonly cEnvelope Envelope;

        private readonly cBodyPart mBody;

        /// <summary>
        /// The IMAP bodystructure information for the encapsulated message.
        /// </summary>
        public readonly cBodyPart BodyStructure;

        /// <summary>
        /// The size in text lines of the encapsulated message.
        /// </summary>
        public readonly uint SizeInLines;

        public cMessageBodyPart(cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, cEnvelope pEnvelope, cBodyPart pBody, cBodyPart pBodyStructure, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(kMimeType.Message, kMimeSubType.RFC822, pSection, pParameters, pContentId, pDescription, pContentTransferEncoding, pSizeInBytes, pExtensionData)
        {
            Envelope = pEnvelope;
            mBody = pBody;
            BodyStructure = pBodyStructure;
            SizeInLines = pSizeInLines;
        }

        /// <summary>
        /// The IMAP body or bodystructure information for the encapsulated message, whichever is available.
        /// </summary>
        public cBodyPart Body => mBody ?? BodyStructure;

        public override string ToString() => $"{nameof(cMessageBodyPart)}({base.ToString()},{Envelope},{BodyStructure ?? mBody},{SizeInLines})";
    }

    /// <summary>
    /// Represents a message part that contains text.
    /// </summary>
    public class cTextBodyPart : cSinglePartBody
    {
        /// <summary>
        /// The MIME subtype of the part in code form.
        /// </summary>
        public readonly eTextBodyPartSubTypeCode SubTypeCode;

        /// <summary>
        /// The size in text lines of the part.
        /// </summary>
        public readonly uint SizeInLines;

        public cTextBodyPart(string pSubType, cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(kMimeType.Text, pSubType, pSection, pParameters, pContentId, pDescription, pContentTransferEncoding, pSizeInBytes, pExtensionData)
        {
            if (SubType.Equals("PLAIN", StringComparison.InvariantCultureIgnoreCase)) SubTypeCode = eTextBodyPartSubTypeCode.plain;
            else if (SubType.Equals("HTML", StringComparison.InvariantCultureIgnoreCase)) SubTypeCode = eTextBodyPartSubTypeCode.html;
            else SubTypeCode = eTextBodyPartSubTypeCode.unknown;

            SizeInLines = pSizeInLines;
        }

        /// <summary>
        /// The character set of the text data.
        /// </summary>
        public string Charset => Parameters?.First("charset")?.StringValue ?? "us-ascii";

        public override string ToString() => $"{nameof(cTextBodyPart)}({base.ToString()},{SizeInLines})";
    }

    /// <summary>
    /// <para>A message part parameter.</para>
    /// <para>Parameters are attribute value pairs.</para>
    /// <para>The value may have a language associated with it.</para>
    /// <para>See RFC 2184.</para>
    /// </summary>
    public class cBodyStructureParameter
    {
        /// <summary>
        /// The name of the attribute.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The un-decoded value.
        /// </summary>
        public readonly cBytes RawValue;

        /// <summary>
        /// The decoded value.
        /// </summary>
        public readonly string StringValue;

        /// <summary>
        /// The language tag of the value (if any).
        /// </summary>
        public readonly string LanguageTag;

        public cBodyStructureParameter(IList<byte> pName, IList<byte> pValue)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pValue == null) throw new ArgumentNullException(nameof(pValue));
            Name = cTools.UTF8BytesToString(pName);
            RawValue = new cBytes(pValue);
            StringValue = cTools.UTF8BytesToString(pValue);
            LanguageTag = null;
        }

        public cBodyStructureParameter(IList<byte> pName, IList<byte> pValue, string pStringValue, string pLanguageTag)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pValue == null) throw new ArgumentNullException(nameof(pValue));
            Name = cTools.UTF8BytesToString(pName);
            RawValue = new cBytes(pValue);
            StringValue = pStringValue;
            LanguageTag = pLanguageTag;
        }

        /// <summary>
        /// Parse the un-decoded value as a UInt.
        /// If the value is not a valid UInt, returns <see langword="null"/>.
        /// </summary>
        public uint? UIntValue
        {
            get
            {
                cBytesCursor lCursor = new cBytesCursor(RawValue);
                if (lCursor.GetNumber(out var _, out var lResult) && lCursor.Position.AtEnd) return lResult;
                return null;
            }
        }

        /// <summary>
        /// Parse the un-decoded value as an RFC 822 date and time.
        /// If the value is not a valid RFC 822 date and time, returns <see langword="null"/>.
        /// </summary>
        public DateTime? DateTimeValue
        {
            get
            {
                cBytesCursor lCursor = new cBytesCursor(RawValue);
                if (lCursor.GetRFC822DateTime(out var lResult) && lCursor.Position.AtEnd) return lResult;
                return null;
            }
        }

        public override string ToString() => $"{nameof(cBodyStructureParameter)}({Name},{RawValue},{StringValue},{LanguageTag})";
    }

    /// <summary>
    /// Message part parameters.
    /// </summary>
    public class cBodyStructureParameters : ReadOnlyCollection<cBodyStructureParameter>
    {
        public cBodyStructureParameters(IList<cBodyStructureParameter> pParameters) : base(pParameters) { }

        /// <summary>
        /// Returns the first parameter with the specified attribute name.
        /// </summary>
        /// <param name="pName">The attribute name.</param>
        /// <returns>The parameter if there is at least one with a matching name, otherwise <see langword="null"/>.</returns>
        public cBodyStructureParameter First(string pName) => this.FirstOrDefault(p => p.Name.Equals(pName, StringComparison.InvariantCultureIgnoreCase));

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBodyStructureParameters));
            foreach (var lParameter in this) lBuilder.Append(lParameter);
            return lBuilder.ToString();
        }
    }
}