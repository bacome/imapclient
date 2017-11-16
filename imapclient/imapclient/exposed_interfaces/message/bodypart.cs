using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The RFC 2045 MIME type of a message body-part.
    /// </summary>
    /// <seealso cref="cBodyPart.TypeCode"/>
    /// <seealso cref="cAttachment.TypeCode"/>"/>
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
    /// The RFC 2183 disposition type of a message body-part.
    /// </summary>
    /// <seealso cref="cBodyPartDisposition.TypeCode"/>
    public enum eDispositionTypeCode
    {
        unknown,
        inline,
        attachment
    }

    /// <summary>
    /// The RFC 2045 MIME subtype of a text message body-part.
    /// </summary>
    /// <seealso cref="cTextBodyPart.SubTypeCode"/>
    public enum eTextBodyPartSubTypeCode
    {
        unknown,
        plain,
        html
    }

    /// <summary>
    /// The RFC 2045 MIME subtype of a multipart message body-part.
    /// </summary>
    /// <seealso cref="cMultiPartBody.SubTypeCode"/>
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
        /**<summary>Multipart</summary>*/
        public const string Multipart = "Multipart";
        /**<summary>Message</summary>*/
        public const string Message = "Message";
        /**<summary>Text</summary>*/
        public const string Text = "Text";
    }

    /// <summary>
    /// Contains named MIME subtype constants.
    /// </summary>
    public static class kMimeSubType
    {
        /**<summary>RFC822</summary>*/
        public const string RFC822 = "RFC822";
    }

    /// <summary>
    /// Represents a message body-part.
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
    /// <seealso cref="cMessage.BodyStructure"/>
    /// <seealso cref="iMessageHandle.Body"/>
    /// <seealso cref="iMessageHandle.BodyStructure"/>
    /// <seealso cref="cMessageBodyPart.Body"/>
    /// <seealso cref="cMessageBodyPart.BodyStructure"/>
    /// <seealso cref="cMultiPartBody.Parts"/>
    public abstract class cBodyPart
    {
        /// <summary>
        /// The MIME type of the body-part in text form.
        /// </summary>
        public readonly string Type;

        /// <summary>
        /// The MIME subtype of the body-part in text form.
        /// </summary>
        public readonly string SubType;

        /// <summary>
        /// The IMAP section identifier of the body-part.
        /// </summary>
        public readonly cSection Section;

        /// <summary>
        /// The MIME type of the body-part in code form.
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
        /// Gets the disposition of the body-part. May be <see langword="null"/>. 
        /// </summary>
        public abstract cBodyPartDisposition Disposition { get; }

        /// <summary>
        /// Gets the language(s) of the body-part. May be <see langword="null"/>. 
        /// </summary>
        public abstract cStrings Languages { get; }

        /// <summary>
        /// Gets the location URI of the body-part. May be <see langword="null"/>.
        /// </summary>
        public abstract string Location { get; }

        /// <summary>
        /// Gets any additional extension data for the body-part. May be <see langword="null"/>.
        /// </summary>
        public abstract cBodyPartExtensionValues ExtensionValues { get; }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cBodyPart)}({Type},{SubType},{Section},{TypeCode})";
    }

    /// <summary>
    /// Represents an element of additional extension-data.
    /// </summary>
    public abstract class cBodyPartExtensionValue { }

    /// <summary>
    /// Contains an element of additional extension-data that is a string.
    /// </summary>
    public class cBodyPartExtensionString : cBodyPartExtensionValue
    {
        /**<summary>The additional extension-data.</summary>*/
        public string String;
        internal cBodyPartExtensionString(string pString) { String = pString; }
        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cBodyPartExtensionString)}({String})";
    }

    /// <summary>
    /// Contains an element of additional extension-data that is a number.
    /// </summary>
    public class cBodyPartExtensionNumber : cBodyPartExtensionValue
    {
        /**<summary>The additional extension-data.</summary>*/
        public uint Number;
        internal cBodyPartExtensionNumber(uint pNumber) { Number = pNumber; }
        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cBodyPartExtensionNumber)}({Number})";
    }

    /// <summary>
    /// Contains an element of additional extension-data that is a collection of values.
    /// </summary>
    /// <seealso cref="cBodyPart.ExtensionValues"/>
    /// <seealso cref="cBodyPartExtensionData.ExtensionValues"/>
    public class cBodyPartExtensionValues : cBodyPartExtensionValue, IEnumerable<cBodyPartExtensionValue>
    {
        /**<summary>The additional extension-data.</summary>*/
        public ReadOnlyCollection<cBodyPartExtensionValue> Values;
        internal cBodyPartExtensionValues(IList<cBodyPartExtensionValue> pValues) { Values = new ReadOnlyCollection<cBodyPartExtensionValue>(pValues); }
        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<cBodyPartExtensionValue> GetEnumerator() => Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cBodyPartExtensionValues));
            foreach (var lValue in Values) lBuilder.Append(lValue);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// A read-only collection of message body-parts.
    /// </summary>
    /// <seealso cref="cMultiPartBody.Parts"/>
    public class cBodyParts : ReadOnlyCollection<cBodyPart>
    {
        internal cBodyParts(IList<cBodyPart> pParts) : base(pParts) { }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBodyParts));
            foreach (var lPart in this) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Represents IMAP BODYSTRUCTURE extension-data.
    /// </summary>
    public abstract class cBodyPartExtensionData
    {
        /// <summary>
        /// The disposition of the body-part. May be <see langword="null"/>. 
        /// </summary>
        public readonly cBodyPartDisposition Disposition;

        /// <summary>
        /// The language(s) of the body-part. May be <see langword="null"/>. 
        /// </summary>
        public readonly cStrings Languages;

        /// <summary>
        /// The location URI of the body-part. May be <see langword="null"/>. 
        /// </summary>
        public readonly string Location;

        /// <summary>
        /// Any additional extension-data for the body-part. May be <see langword="null"/>. 
        /// </summary>
        public readonly cBodyPartExtensionValues ExtensionValues;

        internal cBodyPartExtensionData(cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensionValues)
        {
            Disposition = pDisposition;
            Languages = pLanguages;
            Location = pLocation;
            ExtensionValues = pExtensionValues;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cBodyPartExtensionData)}({Disposition},{Languages},{Location},{ExtensionValues})";
    }

    /// <summary>
    /// Contains the IMAP BODYSTRUCTURE extension-data of a multipart-part message body-part.
    /// </summary>
    /// <seealso cref="cMultiPartBody.ExtensionData"/>
    public class cMultiPartExtensionData : cBodyPartExtensionData
    {
        /// <summary>
        /// The MIME-type parameters of the body-part. May be <see langword="null"/>. 
        /// </summary>
        public readonly cBodyStructureParameters Parameters;

        internal cMultiPartExtensionData(cBodyStructureParameters pParameters, cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensionValues) : base(pDisposition, pLanguages, pLocation, pExtensionValues)
        {
            Parameters = pParameters;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMultiPartExtensionData)}({base.ToString()},{Parameters})";
    }

    /// <summary>
    /// Containts the IMAP BODYSTRUCTURE extension-data of a single-part message body-part.
    /// </summary>
    /// <seealso cref="cSinglePartBody.ExtensionData"/>
    public class cSinglePartExtensionData : cBodyPartExtensionData
    {
        /**<summary>The MD5 value of the body-part. May be <see langword="null"/>.</summary>*/
        public readonly string MD5;

        internal cSinglePartExtensionData(string pMD5, cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensions) : base(pDisposition, pLanguages, pLocation, pExtensions)
        {
            MD5 = pMD5;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cSinglePartExtensionData)}({base.ToString()},{MD5})";
    }

    /// <summary>
    /// Represents a multipart message body-part.
    /// </summary>
    /// <remarks>
    /// The following elements of this class will be <see langword="null"/> when populated with IMAP BODY data rather than IMAP BODYSTRUCTURE data;
    /// <list type="bullet">
    /// <item><see cref="ExtensionData"/></item>
    /// <item><see cref="Parameters"/></item>
    /// <item><see cref="Disposition"/></item>
    /// <item><see cref="Languages"/></item>
    /// <item><see cref="Location"/></item>
    /// <item><see cref="ExtensionValues"/></item>
    /// </list>
    /// Instances populated with BODY data are only available via <see cref="iMessageHandle.Body"/>.
    /// </remarks>
    public class cMultiPartBody : cBodyPart
    {
        /// <summary>
        /// The contained body-parts.
        /// </summary>
        public readonly cBodyParts Parts;

        /// <summary>
        /// The MIME subtype of the body-part in code form.
        /// </summary>
        public readonly eMultiPartBodySubTypeCode SubTypeCode;

        /// <summary>
        /// The IMAP BODYSTRUCTURE extension-data for the body-part. May be <see langword="null"/>.
        /// </summary>
        public readonly cMultiPartExtensionData ExtensionData;

        internal cMultiPartBody(IList<cBodyPart> pParts, string pSubType, cSection pSection, cMultiPartExtensionData pExtensionData) : base(kMimeType.Multipart, pSubType, pSection)
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
        /// Gets the MIME-type parameters of the body-part. May be <see langword="null"/>.
        /// </summary>
        public cBodyStructureParameters Parameters => ExtensionData?.Parameters;

        /// <summary>
        /// Gets the disposition of the body-part. May be <see langword="null"/>.
        /// </summary>
        public override cBodyPartDisposition Disposition => ExtensionData?.Disposition;

        /// <summary>
        /// The language(s) of the body-part. May be <see langword="null"/>.
        /// </summary>
        public override cStrings Languages => ExtensionData?.Languages;

        /// <summary>
        /// The location URI of the body-part. May be <see langword="null"/>.
        /// </summary>
        public override string Location => ExtensionData?.Location;

        /// <summary>
        /// Any additional extension-data for the body-part. May be <see langword="null"/>.
        /// </summary>
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData?.ExtensionValues;

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMultiPartBody)}({base.ToString()},{Parts},{ExtensionData})";
    }

    /// <summary>
    /// Contains RFC 2183 disposition data.
    /// </summary>
    /// <seealso cref="cBodyPart.Disposition"/>
    /// <seealso cref="cBodyPartExtensionData.Disposition"/>
    /// <seealso cref="cSinglePartBody.Disposition"/>
    /// <seealso cref="cMultiPartBody.Disposition"/>
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
        /// The disposition parameters. May be <see langword="null"/>.
        /// </summary>
        public readonly cBodyStructureParameters Parameters;

        internal cBodyPartDisposition(string pType, cBodyStructureParameters pParameters)
        {
            Type = pType ?? throw new ArgumentNullException(nameof(pType));
            Parameters = pParameters;

            if (Type.Equals("INLINE", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eDispositionTypeCode.inline;
            else if (Type.Equals("ATTACHMENT", StringComparison.InvariantCultureIgnoreCase)) TypeCode = eDispositionTypeCode.attachment;
            else TypeCode = eDispositionTypeCode.unknown;
        }

        /// <summary>
        /// Gets the suggested filename. May be <see langword="null"/>.
        /// </summary>
        public string FileName => Parameters?.First("filename")?.StringValue;

        /// <summary>
        /// Gets the creation date. May be <see langword="null"/>.
        /// </summary>
        public DateTime? CreationDate => Parameters?.First("creation-date")?.DateTimeValue;

        /// <summary>
        /// Gets the modification date. May be <see langword="null"/>.
        /// </summary>
        public DateTime? ModificationDate => Parameters?.First("modification-date")?.DateTimeValue;

        /// <summary>
        /// Gets the last read date. May be <see langword="null"/>.
        /// </summary>
        public DateTime? ReadDate => Parameters?.First("read-date")?.DateTimeValue;

        /// <summary>
        /// Gets the approximate size in bytes. May be <see langword="null"/>.
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cBodyPartDisposition)}({Type},{Parameters},{TypeCode})";
    }

    /// <summary>
    /// Represents a single-part message body-part.
    /// </summary>
    /// <remarks>
    /// The following elements of this class will be <see langword="null"/> when populated with IMAP BODY data rather than IMAP BODYSTRUCTURE data;
    /// <list type="bullet">
    /// <item><see cref="ExtensionData"/></item>
    /// <item><see cref="MD5"/></item>
    /// <item><see cref="Disposition"/></item>
    /// <item><see cref="Languages"/></item>
    /// <item><see cref="Location"/></item>
    /// <item><see cref="ExtensionValues"/></item>
    /// </list>
    /// Instances populated with BODY data are only available via <see cref="iMessageHandle.Body"/>.
    /// </remarks>
    /// <seealso cref="cMessage.FetchSizeInBytes(cSinglePartBody)"/>
    /// <seealso cref="cMessage.Fetch(cSinglePartBody, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cAttachment.Part"/>
    public class cSinglePartBody : cBodyPart
    {
        /// <summary>
        /// The MIME-type parameters of the body-part. May be <see langword="null"/>.
        /// </summary>
        public readonly cBodyStructureParameters Parameters;

        /// <summary>
        /// The MIME content-id of the body-part. May be <see langword="null"/>.
        /// </summary>
        public readonly string ContentId;

        /// <summary>
        /// The MIME content description of the body-part. May be <see langword="null"/>.
        /// </summary>
        public readonly cCulturedString Description;

        /// <summary>
        /// The MIME content-transfer-encoding of the body-part in text form.
        /// </summary>
        public readonly string ContentTransferEncoding;

        /// <summary>
        /// The MIME content-transfer-encoding of the body-part in code form.
        /// </summary>
        public readonly eDecodingRequired DecodingRequired;

        /// <summary>
        /// The size in bytes of the encoded body-part.
        /// </summary>
        public readonly uint SizeInBytes;

        /// <summary>
        /// The IMAP BODYSTRUCTURE extension-data for the body-part. May be <see langword="null"/>.
        /// </summary>
        public readonly cSinglePartExtensionData ExtensionData;

        internal cSinglePartBody(string pType, string pSubType, cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, cSinglePartExtensionData pExtensionData) : base(pType, pSubType, pSection)
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

        /**<summary>Gets the MD5 value of the body-part. May be <see langword="null"/>.</summary>*/
        public string MD5 => ExtensionData?.MD5;

        /// <summary>
        /// The disposition of the body-part. May be <see langword="null"/>.
        /// </summary>
        public override cBodyPartDisposition Disposition => ExtensionData?.Disposition;

        /// <summary>
        /// The language(s) of the body-part. May be <see langword="null"/>.
        /// </summary>
        public override cStrings Languages => ExtensionData?.Languages;

        /// <summary>
        /// The location URI of the body-part. May be <see langword="null"/>.
        /// </summary>
        public override string Location => ExtensionData?.Location;

        /// <summary>
        /// Any additional extension-data for the body-part. May be <see langword="null"/>.
        /// </summary>
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData?.ExtensionValues;

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cSinglePartBody)}({base.ToString()},{Parameters},{ContentId},{Description},{ContentTransferEncoding},{DecodingRequired},{SizeInBytes},{ExtensionData})";
    }

    /// <summary>
    /// Represents a message body-part that contains a message.
    /// </summary>
    /// <remarks>
    /// The <see cref="BodyStructure"/> element of this class will be <see langword="null"/> when populated with IMAP BODY data rather than IMAP BODYSTRUCTURE data.
    /// Instances populated with BODY data are only available via <see cref="iMessageHandle.Body"/>.
    /// </remarks>
    public class cMessageBodyPart : cSinglePartBody
    {
        /// <summary>
        /// The IMAP envelope of the encapsulated message.
        /// </summary>
        public readonly cEnvelope Envelope;

        private readonly cBodyPart mBody;

        /// <summary>
        /// The IMAP BODYSTRUCTURE information for the encapsulated message. May be <see langword="null"/>.
        /// </summary>
        public readonly cBodyPart BodyStructure;

        /// <summary>
        /// The size in text-lines of the encapsulated message.
        /// </summary>
        public readonly uint SizeInLines;

        internal cMessageBodyPart(cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, cEnvelope pEnvelope, cBodyPart pBody, cBodyPart pBodyStructure, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(kMimeType.Message, kMimeSubType.RFC822, pSection, pParameters, pContentId, pDescription, pContentTransferEncoding, pSizeInBytes, pExtensionData)
        {
            Envelope = pEnvelope;
            mBody = pBody;
            BodyStructure = pBodyStructure;
            SizeInLines = pSizeInLines;
        }

        /// <summary>
        /// The IMAP BODY or BODYSTRUCTURE information for the encapsulated message, whichever is available.
        /// </summary>
        public cBodyPart Body => mBody ?? BodyStructure;

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cMessageBodyPart)}({base.ToString()},{Envelope},{BodyStructure ?? mBody},{SizeInLines})";
    }

    /// <summary>
    /// Represents a message body-part that contains text.
    /// </summary>
    /// <seealso cref="cMessage.Fetch(cTextBodyPart)"/>.
    public class cTextBodyPart : cSinglePartBody
    {
        /// <summary>
        /// The MIME subtype of the part in code form.
        /// </summary>
        public readonly eTextBodyPartSubTypeCode SubTypeCode;

        /// <summary>
        /// The size in text-lines of the part.
        /// </summary>
        public readonly uint SizeInLines;

        internal cTextBodyPart(string pSubType, cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(kMimeType.Text, pSubType, pSection, pParameters, pContentId, pDescription, pContentTransferEncoding, pSizeInBytes, pExtensionData)
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

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cTextBodyPart)}({base.ToString()},{SizeInLines})";
    }

    /// <summary>
    /// Represents an attribute-value pair.
    /// </summary>
    /// <remarks>
    /// The value may have a language associated with it. See RFC 2184.
    /// </remarks>
    /// <seealso cref="cAttachment.Parameters"/>
    /// <seealso cref="cSinglePartBody.Parameters"/>
    /// <seealso cref="cMultiPartBody.Parameters"/>
    /// <seealso cref="cBodyPartDisposition.Parameters"/>
    /// <seealso cref="cMultiPartExtensionData"/>
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
        /// The language tag of the value. May be <see langword="null"/>.
        /// </summary>
        public readonly string LanguageTag;

        internal cBodyStructureParameter(IList<byte> pName, IList<byte> pValue)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pValue == null) throw new ArgumentNullException(nameof(pValue));
            Name = cTools.UTF8BytesToString(pName);
            RawValue = new cBytes(pValue);
            StringValue = cTools.UTF8BytesToString(pValue);
            LanguageTag = null;
        }

        internal cBodyStructureParameter(IList<byte> pName, IList<byte> pValue, string pStringValue, string pLanguageTag)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pValue == null) throw new ArgumentNullException(nameof(pValue));
            Name = cTools.UTF8BytesToString(pName);
            RawValue = new cBytes(pValue);
            StringValue = pStringValue;
            LanguageTag = pLanguageTag;
        }

        /// <summary>
        /// Gets the value as a <see cref="UInt32"/>. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Will return <see langword="null"/> if the value cannot be parsed as an IMAP 'number'.
        /// </remarks>
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
        /// Gets the value as a <see cref="DateTime"/>. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Will return <see langword="null"/> if the value cannot be parsed as an RFC 5322 date-time.
        /// </remarks>
        public DateTime? DateTimeValue
        {
            get
            {
                cBytesCursor lCursor = new cBytesCursor(RawValue);
                if (lCursor.GetRFC822DateTime(out var lResult) && lCursor.Position.AtEnd) return lResult;
                return null;
            }
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cBodyStructureParameter)}({Name},{RawValue},{StringValue},{LanguageTag})";
    }

    /// <summary>
    /// A read-only collection of attribute-value pairs.
    /// </summary>
    /// <seealso cref="cAttachment.Parameters"/>
    /// <seealso cref="cSinglePartBody.Parameters"/>
    /// <seealso cref="cMultiPartBody.Parameters"/>
    /// <seealso cref="cBodyPartDisposition.Parameters"/>
    /// <seealso cref="cMultiPartExtensionData"/>
    public class cBodyStructureParameters : ReadOnlyCollection<cBodyStructureParameter>
    {
        internal cBodyStructureParameters(IList<cBodyStructureParameter> pParameters) : base(pParameters) { }

        /// <summary>
        /// Returns the first parameter with the specified attribute name.
        /// </summary>
        /// <param name="pName">The attribute name.</param>
        /// <returns>The parameter if there is at least one with a matching name, otherwise <see langword="null"/>.</returns>
        public cBodyStructureParameter First(string pName) => this.FirstOrDefault(p => p.Name.Equals(pName, StringComparison.InvariantCultureIgnoreCase));

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBodyStructureParameters));
            foreach (var lParameter in this) lBuilder.Append(lParameter);
            return lBuilder.ToString();
        }
    }
}