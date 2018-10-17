﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents the RFC 2045 MIME type of a message body-part.
    /// </summary>
    /// <seealso cref="cBodyPart.TypeCode"/>
    /// <seealso cref="cAttachment.TypeCode"/>"/>
    public enum eBodyPartTypeCode
    {
        /**<summary>The MIME type was not recognised by the library.</summary>*/
        other,
        /**<summary>Text.</summary>*/
        text,
        /**<summary>Image data.</summary>*/
        image,
        /**<summary>Audio data.</summary>*/
        audio,
        /**<summary>Video data.</summary>*/
        video,
        /**<summary>Some other kind of data.</summary>*/
        application,
        /**<summary>Multiple entities of independent data types.</summary>*/
        multipart,
        /**<summary>An encapsulated message.</summary>*/
        message
    }

    /// <summary>
    /// Represents the RFC 2183 disposition type of a message body-part.
    /// </summary>
    /// <seealso cref="cBodyPartDisposition.TypeCode"/>
    public enum eDispositionTypeCode
    {
        /**<summary>The disposition type was not recognised by the library.</summary>*/
        other,
        /**<summary>Inline.</summary>*/
        inline,
        /**<summary>Attachment.</summary>*/
        attachment
    }

    /// <summary>
    /// Represents the RFC 2045 MIME subtype of a text message body-part.
    /// </summary>
    /// <seealso cref="cTextBodyPart.SubTypeCode"/>
    public enum eTextBodyPartSubTypeCode
    {
        /**<summary>The subtype was not recognised by the library.</summary>*/
        other,
        /**<summary>Plain text.</summary>*/
        plain,
        /**<summary>HTML.</summary>*/
        html
    }

    /// <summary>
    /// Represents the RFC 2045 MIME subtype of a multipart message body-part.
    /// </summary>
    /// <seealso cref="cMultiPartBody.SubTypeCode"/>
    public enum eMultiPartBodySubTypeCode
    {
        /**<summary>The subtype was not recognised by the library.</summary>*/
        other,
        /**<summary>Independent parts in a particular order.</summary>*/
        mixed,
        /**<summary>Independent parts in a particular order.</summary>*/
        digest,
        /**<summary>Alternative versions of the same information.</summary>*/
        alternative,
        /**<summary>Inter-related parts (RFC 2387).</summary>*/
        related
    }

    internal static class kMimeType
    {
        public const string Multipart = "Multipart";
        public const string Message = "Message";
        public const string Text = "Text";
    }

    internal static class kMimeSubType
    {
        public const string RFC822 = "RFC822";
    }

    /// <summary>
    /// Represents a message body-part.
    /// </summary>
    [Serializable]
    public abstract class cBodyPart
    {
        /// <summary>
        /// The MIME type of the body-part as a string.
        /// </summary>
        public readonly string Type;

        /// <summary>
        /// The MIME subtype of the body-part as a string.
        /// </summary>
        public readonly string SubType;

        /// <summary>
        /// The section specification of the body-part.
        /// </summary>
        public readonly cSection Section;

        [NonSerialized]
        private eBodyPartTypeCode mTypeCode;

        internal cBodyPart(string pType, string pSubType, cSection pSection)
        {
            Type = pType ?? throw new ArgumentNullException(nameof(pType));
            SubType = pSubType ?? throw new ArgumentNullException(nameof(pSubType));
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
            if (!Section.CouldDescribeABodyPart)  throw new ArgumentOutOfRangeException(nameof(pSection));
            ZFinishConstruct();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Type == null) throw new cDeserialiseException(nameof(cBodyPart), nameof(Type), kDeserialiseExceptionMessage.IsNull);
            if (SubType == null) throw new cDeserialiseException(nameof(cBodyPart), nameof(SubType), kDeserialiseExceptionMessage.IsNull);
            if (Section == null) throw new cDeserialiseException(nameof(cBodyPart), nameof(Section), kDeserialiseExceptionMessage.IsNull);
            if (!Section.CouldDescribeABodyPart) throw new cDeserialiseException(nameof(cBodyPart), nameof(Section), kDeserialiseExceptionMessage.IsInvalid);
            ZFinishConstruct();
        }

        private void ZFinishConstruct()
        {
            if (Type.Equals(kMimeType.Text, StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eBodyPartTypeCode.text;
            else if (Type.Equals("IMAGE", StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eBodyPartTypeCode.image;
            else if (Type.Equals("AUDIO", StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eBodyPartTypeCode.audio;
            else if (Type.Equals("VIDEO", StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eBodyPartTypeCode.video;
            else if (Type.Equals("APPLICATION", StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eBodyPartTypeCode.application;
            else if (Type.Equals(kMimeType.Multipart, StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eBodyPartTypeCode.multipart;
            else if (Type.Equals(kMimeType.Message, StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eBodyPartTypeCode.message;
            else mTypeCode = eBodyPartTypeCode.other;
        }

        /// <summary>
        /// The MIME type of the body-part as a code.
        /// </summary>
        public eBodyPartTypeCode TypeCode => mTypeCode;

        public abstract fMessageDataFormat Format { get; }

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
        /// Gets any additional extension-data for the body-part. May be <see langword="null"/>.
        /// </summary>
        public abstract cBodyPartExtensionValues ExtensionValues { get; }

        internal abstract bool TryGetSinglePartBody(cSection pSection, out cSinglePartBody rSinglePartBody);

        internal virtual bool CouldBeTheBodyStructureOf(cSection pMessageSection) => false;

        internal virtual bool LikelyIs(cBodyPart pPart)
        {
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));
            return Type == pPart.Type && SubType == pPart.SubType && Section == pPart.Section && Format == pPart.Format;
        }

        internal virtual bool LikelyContains(cBodyPart pPart)
        {
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cBodyPart)}({Type},{SubType},{Section},{mTypeCode})";
    }

    /// <summary>
    /// Represents an element of additional IMAP BODYSTRUCTURE extension-data.
    /// </summary>
    [Serializable]
    public abstract class cBodyPartExtensionValue
    {
        internal cBodyPartExtensionValue() { }
    }

    /// <summary>
    /// Contains an element of additional IMAP BODYSTRUCTURE extension-data that is a string.
    /// </summary>
    [Serializable]
    public class cBodyPartExtensionString : cBodyPartExtensionValue
    {
        /**<summary>The additional extension-data.</summary>*/
        public string String;
        internal cBodyPartExtensionString(string pString) { String = pString; }
        /// <inheritdoc />
        public override string ToString() => $"{nameof(cBodyPartExtensionString)}({String})";
    }

    /// <summary>
    /// Contains an element of additional IMAP BODYSTRUCTURE extension-data that is a number.
    /// </summary>
    [Serializable]
    public class cBodyPartExtensionNumber : cBodyPartExtensionValue
    {
        /**<summary>The additional extension-data.</summary>*/
        public uint Number;
        internal cBodyPartExtensionNumber(uint pNumber) { Number = pNumber; }
        /// <inheritdoc />
        public override string ToString() => $"{nameof(cBodyPartExtensionNumber)}({Number})";
    }

    /// <summary>
    /// Contains an element of additional IMAP BODYSTRUCTURE extension-data that is a collection of values.
    /// </summary>
    [Serializable]
    public class cBodyPartExtensionValues : cBodyPartExtensionValue, IEnumerable<cBodyPartExtensionValue>
    {
        /**<summary>The additional extension-data.</summary>*/
        public ReadOnlyCollection<cBodyPartExtensionValue> Values;

        internal cBodyPartExtensionValues(IList<cBodyPartExtensionValue> pValues) { Values = new ReadOnlyCollection<cBodyPartExtensionValue>(pValues); }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            foreach (var lValue in Values) if (lValue == null) throw new cDeserialiseException(nameof(cBodyPartExtensionValues), nameof(Values), kDeserialiseExceptionMessage.ContainsNulls);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<cBodyPartExtensionValue> GetEnumerator() => Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cBodyPartExtensionValues));
            foreach (var lValue in Values) lBuilder.Append(lValue);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// An immutable collection of message body-parts.
    /// </summary>
    [Serializable]
    public class cBodyParts : ReadOnlyCollection<cBodyPart>
    {
        internal cBodyParts(IList<cBodyPart> pParts) : base(pParts) { }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            foreach (var lPart in this) if (lPart == null) throw new cDeserialiseException(nameof(cBodyParts), null, kDeserialiseExceptionMessage.ContainsNulls);
        }

        /// <inheritdoc />
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
    [Serializable]
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

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Languages != null) foreach (var lLanguage in Languages) if (lLanguage == null) throw new cDeserialiseException(nameof(cBodyPartExtensionData), nameof(Languages), kDeserialiseExceptionMessage.ContainsNulls);
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cBodyPartExtensionData)}({Disposition},{Languages},{Location},{ExtensionValues})";
    }

    /// <summary>
    /// Contains the IMAP BODYSTRUCTURE extension-data of a multipart message body-part.
    /// </summary>
    [Serializable]
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

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMultiPartExtensionData)}({base.ToString()},{Parameters})";
    }

    /// <summary>
    /// Containts the IMAP BODYSTRUCTURE extension-data of a single-part message body-part.
    /// </summary>
    [Serializable]
    public class cSinglePartExtensionData : cBodyPartExtensionData
    {
        /**<summary>The MD5 value of the body-part. May be <see langword="null"/>.</summary>*/
        public readonly string MD5;

        internal cSinglePartExtensionData(string pMD5, cBodyPartDisposition pDisposition, cStrings pLanguages, string pLocation, cBodyPartExtensionValues pExtensions) : base(pDisposition, pLanguages, pLocation, pExtensions)
        {
            MD5 = pMD5;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSinglePartExtensionData)}({base.ToString()},{MD5})";
    }

    /// <summary>
    /// Represents a multipart message body-part.
    /// </summary>
    /// <remarks>
    /// The following elements of instances will be <see langword="null"/> when populated with IMAP BODY data rather than IMAP BODYSTRUCTURE data;
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
    [Serializable]
    public class cMultiPartBody : cBodyPart
    {
        /// <summary>
        /// The contained body-parts.
        /// </summary>
        public readonly cBodyParts Parts;

        private readonly bool mUTF8Enabled;

        [NonSerialized]
        private fMessageDataFormat mFormat;

        [NonSerialized]
        private eMultiPartBodySubTypeCode mSubTypeCode;

        /// <summary>
        /// The IMAP BODYSTRUCTURE extension-data for the body-part. May be <see langword="null"/>.
        /// </summary>
        public readonly cMultiPartExtensionData ExtensionData;

        internal cMultiPartBody(IList<cBodyPart> pParts, bool pUTF8Enabled, string pSubType, cSection pSection, cMultiPartExtensionData pExtensionData) : base(kMimeType.Multipart, pSubType, pSection)
        {
            Parts = new cBodyParts(pParts);
            mUTF8Enabled = pUTF8Enabled;
            ExtensionData = pExtensionData;
            ZFinishConstruct();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Parts == null) throw new cDeserialiseException(nameof(cMultiPartBody), nameof(Parts), kDeserialiseExceptionMessage.IsNull);

            string lSubPartPrefix = Section.GetSubPartPrefix();
            int lSubPart = 1;

            foreach (var lPart in Parts)
            {
                if (lPart == null) throw new cDeserialiseException(nameof(cMultiPartBody), nameof(Parts), kDeserialiseExceptionMessage.ContainsNulls);
                if (lPart.Section.Part != lSubPartPrefix + lSubPart++) throw new cDeserialiseException(nameof(cMultiPartBody), nameof(Parts), kDeserialiseExceptionMessage.IncorrectSequence);
            }

            ZFinishConstruct();
        }

        private void ZFinishConstruct()
        {
            foreach (var lPart in Parts) mFormat |= lPart.Format;
            if (mUTF8Enabled) mFormat = mFormat | fMessageDataFormat.eightbitutf8headers; // just in case the mime headers have utf8 in them

            if (SubType.Equals("MIXED", StringComparison.InvariantCultureIgnoreCase)) mSubTypeCode = eMultiPartBodySubTypeCode.mixed;
            else if (SubType.Equals("DIGEST", StringComparison.InvariantCultureIgnoreCase)) mSubTypeCode = eMultiPartBodySubTypeCode.digest;
            else if (SubType.Equals("ALTERNATIVE", StringComparison.InvariantCultureIgnoreCase)) mSubTypeCode = eMultiPartBodySubTypeCode.alternative;
            else if (SubType.Equals("RELATED", StringComparison.InvariantCultureIgnoreCase)) mSubTypeCode = eMultiPartBodySubTypeCode.related;
            else mSubTypeCode = eMultiPartBodySubTypeCode.other;
        }

        /// <summary>
        /// The MIME subtype of the body-part as a code.
        /// </summary>
        public eMultiPartBodySubTypeCode SubTypeCode => mSubTypeCode;

        /// <summary>
        /// Gets the MIME-type parameters of the body-part. May be <see langword="null"/>.
        /// </summary>
        public cBodyStructureParameters Parameters => ExtensionData?.Parameters;

        public override fMessageDataFormat Format => mFormat;

        /// <inheritdoc />
        public override cBodyPartDisposition Disposition => ExtensionData?.Disposition;

        /// <inheritdoc />
        public override cStrings Languages => ExtensionData?.Languages;

        /// <inheritdoc />
        public override string Location => ExtensionData?.Location;

        /// <inheritdoc />
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData?.ExtensionValues;

        internal override bool TryGetSinglePartBody(cSection pSection, out cSinglePartBody rSinglePartBody)
        {
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));
            foreach (var lPart in Parts) if (lPart.TryGetSinglePartBody(pSection, out rSinglePartBody)) return true;
            rSinglePartBody = null;
            return false;
        }

        internal override bool CouldBeTheBodyStructureOf(cSection pMessageSection)
        {
            if (pMessageSection == null) return Section == cSection.Text;
            if (!pMessageSection.CouldDescribeASinglePartBody) throw new ArgumentOutOfRangeException(nameof(pMessageSection));
            return Section.Part == pMessageSection.Part && Section == cSection.Text;
        }

        internal override bool LikelyIs(cBodyPart pPart)
        {
            if (!base.LikelyIs(pPart)) return false;
            var lPart = pPart as cMultiPartBody;
            if (lPart == null) return false;
            if (Parts.Count != lPart.Parts.Count || mUTF8Enabled != lPart.mUTF8Enabled) return false;
            for (int i = 0; i < Parts.Count; i++) if (!Parts[i].LikelyIs(lPart.Parts[i])) return false;
            return true;
        }

        internal override bool LikelyContains(cBodyPart pPart)
        {
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));

            foreach (var lPart in Parts)
            {
                if (lPart.LikelyIs(pPart)) return true;
                if (lPart.LikelyContains(pPart)) return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMultiPartBody)}({base.ToString()},{Parts},{ExtensionData})";
    }

    /// <summary>
    /// Contains RFC 2183 disposition data.
    /// </summary>
    [Serializable]
    public class cBodyPartDisposition
    {
        /// <summary>
        /// The disposition type as a string. 
        /// </summary>
        public readonly string Type;

        /// <summary>
        /// The disposition type as a code. 
        /// </summary>
        [NonSerialized]
        private eDispositionTypeCode mTypeCode;

        /// <summary>
        /// The disposition parameters. May be <see langword="null"/>.
        /// </summary>
        public readonly cBodyStructureParameters Parameters;

        internal cBodyPartDisposition(string pType, cBodyStructureParameters pParameters)
        {
            Type = pType ?? throw new ArgumentNullException(nameof(pType));
            Parameters = pParameters;
            ZFinishConstruct();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Type == null) throw new cDeserialiseException(nameof(cBodyPartDisposition), nameof(Type), kDeserialiseExceptionMessage.IsNull);
            ZFinishConstruct();
        }

        private void ZFinishConstruct()
        {
            if (Type.Equals("INLINE", StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eDispositionTypeCode.inline;
            else if (Type.Equals("ATTACHMENT", StringComparison.InvariantCultureIgnoreCase)) mTypeCode = eDispositionTypeCode.attachment;
            else mTypeCode = eDispositionTypeCode.other;
        }

        public eDispositionTypeCode TypeCode => mTypeCode;

        /// <summary>
        /// Gets the suggested filename. May be <see langword="null"/>.
        /// </summary>
        public string FileName => Parameters?.First("filename")?.StringValue;

        /// <summary>
        /// Gets the creation date. May be <see langword="null"/>.
        /// </summary>
        public cTimestamp CreationDate => Parameters?.First("creation-date")?.TimestampValue;

        /// <summary>
        /// Gets the modification date. May be <see langword="null"/>.
        /// </summary>
        public cTimestamp ModificationDate => Parameters?.First("modification-date")?.TimestampValue;

        /// <summary>
        /// Gets the read date. May be <see langword="null"/>.
        /// </summary>
        public cTimestamp ReadDate => Parameters?.First("read-date")?.TimestampValue;

        /// <summary>
        /// Gets the approximate size in bytes. May be <see langword="null"/>.
        /// </summary>
        public uint? Size => Parameters?.First("size")?.UIntValue;

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cBodyPartDisposition)}({Type},{Parameters},{mTypeCode})";
    }

    /// <summary>
    /// Represents a single-part message body-part.
    /// </summary>
    /// <remarks>
    /// The following elements of instances will be <see langword="null"/> when populated with IMAP BODY data rather than IMAP BODYSTRUCTURE data;
    /// <list type="bullet">
    /// <item><see cref="Disposition"/></item>
    /// <item><see cref="ExtensionData"/></item>
    /// <item><see cref="ExtensionValues"/></item>
    /// <item><see cref="Languages"/></item>
    /// <item><see cref="Location"/></item>
    /// <item><see cref="MD5"/></item>
    /// </list>
    /// Instances populated with BODY data are only available via <see cref="iMessageHandle.Body"/>.
    /// </remarks>
    [Serializable]
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
        /// The MIME content-transfer-encoding of the body-part as a string.
        /// </summary>
        public readonly string ContentTransferEncoding;

        private readonly bool m8bitImpliesUTF8Headers;

        /// <summary>
        /// The size in bytes of the encoded body-part.
        /// </summary>
        public readonly uint SizeInBytes;

        /// <summary>
        /// The IMAP BODYSTRUCTURE extension-data for the body-part. May be <see langword="null"/>.
        /// </summary>
        public readonly cSinglePartExtensionData ExtensionData;

        [NonSerialized]
        private fMessageDataFormat mFormat;

        [NonSerialized]
        private eDecodingRequired mDecodingRequired;

        internal cSinglePartBody(string pType, string pSubType, cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, bool p8bitImpliesUTF8Headers, uint pSizeInBytes, cSinglePartExtensionData pExtensionData) : base(pType, pSubType, pSection)
        {
            if (!pSection.CouldDescribeASinglePartBody) throw new ArgumentOutOfRangeException(nameof(pSection));
            Parameters = pParameters;
            ContentId = pContentId;
            Description = pDescription;
            ContentTransferEncoding = pContentTransferEncoding ?? throw new ArgumentNullException(nameof(pContentTransferEncoding));
            m8bitImpliesUTF8Headers = p8bitImpliesUTF8Headers;
            SizeInBytes = pSizeInBytes;
            ExtensionData = pExtensionData;
            ZFinishConstruct();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (!Section.CouldDescribeASinglePartBody) throw new cDeserialiseException(nameof(cSinglePartBody), nameof(Section), kDeserialiseExceptionMessage.IsInvalid);
            if (ContentTransferEncoding == null) throw new cDeserialiseException(nameof(cSinglePartBody), nameof(ContentTransferEncoding), kDeserialiseExceptionMessage.IsNull);
            if (m8bitImpliesUTF8Headers && (TypeCode != eBodyPartTypeCode.message) || !SubType.Equals(kMimeSubType.RFC822, StringComparison.InvariantCultureIgnoreCase)) throw new cDeserialiseException(nameof(cSinglePartBody), nameof(m8bitImpliesUTF8Headers), kDeserialiseExceptionMessage.IsInconsistent);
            ZFinishConstruct();
        }

        private void ZFinishConstruct()
        {
            if (ContentTransferEncoding.Equals("7BIT", StringComparison.InvariantCultureIgnoreCase))
            {
                mFormat = 0;
                mDecodingRequired = eDecodingRequired.none;
            }
            else if (ContentTransferEncoding.Equals("8BIT", StringComparison.InvariantCultureIgnoreCase))
            {
                if (m8bitImpliesUTF8Headers) mFormat = fMessageDataFormat.eightbitutf8headers;
                else mFormat = fMessageDataFormat.eightbit;

                mDecodingRequired = eDecodingRequired.none;
            }
            else if (ContentTransferEncoding.Equals("BINARY", StringComparison.InvariantCultureIgnoreCase))
            {
                if (m8bitImpliesUTF8Headers) mFormat = fMessageDataFormat.all;
                else mFormat = fMessageDataFormat.eightbitbinary;

                mDecodingRequired = eDecodingRequired.none;
            }
            else if (ContentTransferEncoding.Equals("QUOTED-PRINTABLE", StringComparison.InvariantCultureIgnoreCase))
            {
                mFormat = 0;
                mDecodingRequired = eDecodingRequired.quotedprintable;
            }
            else if (ContentTransferEncoding.Equals("BASE64", StringComparison.InvariantCultureIgnoreCase))
            {
                mFormat = 0;
                mDecodingRequired = eDecodingRequired.base64;
            }
            else
            {
                // note that rfc 2045 section 6.4 specifies that if 'unknown' then the part has to be treated as application/octet-stream
                //  however I think I should be able to assume that it is 7bit data (otherwise the CTE should be binary)
                mFormat = 0;
                mDecodingRequired = eDecodingRequired.other;
            }
        }

        /// <summary>
        /// The MIME content-transfer-encoding of the body-part as a code.
        /// </summary>
        public eDecodingRequired DecodingRequired => mDecodingRequired;

        /**<summary>Gets the MD5 value of the body-part. May be <see langword="null"/>.</summary>*/
        public string MD5 => ExtensionData?.MD5;

        public override fMessageDataFormat Format => mFormat;

        /// <inheritdoc />
        public override cBodyPartDisposition Disposition => ExtensionData?.Disposition;

        /// <inheritdoc />
        public override cStrings Languages => ExtensionData?.Languages;

        /// <inheritdoc />
        public override string Location => ExtensionData?.Location;

        /// <inheritdoc />
        public override cBodyPartExtensionValues ExtensionValues => ExtensionData?.ExtensionValues;

        internal override bool TryGetSinglePartBody(cSection pSection, out cSinglePartBody rSinglePartBody)
        {
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));

            if (Section == pSection)
            {
                rSinglePartBody = this;
                return true;
            }

            rSinglePartBody = null;
            return false;
        }

        internal override bool CouldBeTheBodyStructureOf(cSection pMessageSection)
        {
            if (pMessageSection == null) return Section.Part == "1";
            if (!pMessageSection.CouldDescribeASinglePartBody) throw new ArgumentOutOfRangeException(nameof(pMessageSection));
            return Section.Part == pMessageSection.Part + ".1";
        }

        internal override bool LikelyIs(cBodyPart pPart)
        {
            if (!base.LikelyIs(pPart)) return false;
            var lPart = pPart as cSinglePartBody;
            if (lPart == null) return false;
            return ContentTransferEncoding == lPart.ContentTransferEncoding && SizeInBytes == lPart.SizeInBytes;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cSinglePartBody)}({base.ToString()},{Parameters},{ContentId},{Description},{ContentTransferEncoding},{Format},{DecodingRequired},{SizeInBytes},{ExtensionData})";
    }

    /// <summary>
    /// Represents a message body-part that contains a message.
    /// </summary>
    [Serializable]
    public class cMessageBodyPart : cSinglePartBody
    {
        /// <summary>
        /// The IMAP ENVELOPE data of the encapsulated message.
        /// </summary>
        public readonly cEnvelope Envelope;

        /// <summary>
        /// The IMAP body structure information for the encapsulated message.
        /// </summary>
        public readonly cBodyPart BodyStructure;

        /// <summary>
        /// The size in text-lines of the encapsulated message.
        /// </summary>
        public readonly uint SizeInLines;

        internal cMessageBodyPart(cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, bool p8bitImpliesUTF8Headers, uint pSizeInBytes, cEnvelope pEnvelope, cBodyPart pBodyStructure, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(kMimeType.Message, kMimeSubType.RFC822, pSection, pParameters, pContentId, pDescription, pContentTransferEncoding, p8bitImpliesUTF8Headers, pSizeInBytes, pExtensionData)
        {
            Envelope = pEnvelope ?? throw new ArgumentNullException(nameof(pEnvelope));
            BodyStructure = pBodyStructure ?? throw new ArgumentNullException(nameof(pBodyStructure));
            if (!BodyStructure.CouldBeTheBodyStructureOf(pSection)) throw new ArgumentOutOfRangeException(nameof(pBodyStructure));
            SizeInLines = pSizeInLines;
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (Envelope == null) throw new cDeserialiseException(nameof(cMessageBodyPart), nameof(Envelope), kDeserialiseExceptionMessage.IsNull);
            if (BodyStructure == null) throw new cDeserialiseException(nameof(cMessageBodyPart), nameof(BodyStructure), kDeserialiseExceptionMessage.IsNull);
            if (!BodyStructure.CouldBeTheBodyStructureOf(Section)) throw new cDeserialiseException(nameof(cMessageBodyPart), nameof(BodyStructure), kDeserialiseExceptionMessage.IsInvalid);
        }

        internal override bool TryGetSinglePartBody(cSection pSection, out cSinglePartBody rSinglePartBody)
        {
            if (pSection == null) throw new ArgumentNullException(nameof(pSection));

            if (Section == pSection)
            {
                rSinglePartBody = this;
                return true;
            }

            return BodyStructure.TryGetSinglePartBody(pSection, out rSinglePartBody);
        }

        internal override bool LikelyIs(cBodyPart pPart)
        {
            if (!base.LikelyIs(pPart)) return false;
            var lPart = pPart as cMessageBodyPart;
            if (lPart == null) return false;
            return SizeInLines == lPart.SizeInLines && BodyStructure.LikelyIs(lPart.BodyStructure);
        }

        internal override bool LikelyContains(cBodyPart pPart) 
        {
            if (pPart == null) throw new ArgumentNullException(nameof(pPart));
            if (BodyStructure == null) return false;
            if (BodyStructure.LikelyIs(pPart)) return true;
            if (BodyStructure.LikelyContains(pPart)) return true;
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMessageBodyPart)}({base.ToString()},{Envelope},{BodyStructure},{SizeInLines})";
    }

    /// <summary>
    /// Represents a message body-part that contains text.
    /// </summary>
    [Serializable]
    public class cTextBodyPart : cSinglePartBody
    {
        /// <summary>
        /// The size in text-lines of the body-part.
        /// </summary>
        public readonly uint SizeInLines;

        [NonSerialized]
        private eTextBodyPartSubTypeCode mSubTypeCode;

        internal cTextBodyPart(string pSubType, cSection pSection, cBodyStructureParameters pParameters, string pContentId, cCulturedString pDescription, string pContentTransferEncoding, uint pSizeInBytes, uint pSizeInLines, cSinglePartExtensionData pExtensionData) : base(kMimeType.Text, pSubType, pSection, pParameters, pContentId, pDescription, pContentTransferEncoding, false, pSizeInBytes, pExtensionData)
        {
            SizeInLines = pSizeInLines;
            ZFinishConstruct();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            ZFinishConstruct();
        }

        private void ZFinishConstruct()
        {
            if (SubType.Equals("PLAIN", StringComparison.InvariantCultureIgnoreCase)) mSubTypeCode = eTextBodyPartSubTypeCode.plain;
            else if (SubType.Equals("HTML", StringComparison.InvariantCultureIgnoreCase)) mSubTypeCode = eTextBodyPartSubTypeCode.html;
            else mSubTypeCode = eTextBodyPartSubTypeCode.other;
        }

        /// <summary>
        /// The MIME subtype of the body-part as a code.
        /// </summary>
        public eTextBodyPartSubTypeCode SubTypeCode => mSubTypeCode;

        /// <summary>
        /// The character set of the text data.
        /// </summary>
        public string Charset => Parameters?.First("charset")?.StringValue ?? "us-ascii";

        internal override bool LikelyIs(cBodyPart pPart)
        {
            if (!base.LikelyIs(pPart)) return false;
            var lPart = pPart as cTextBodyPart;
            if (lPart == null) return false;
            return SizeInLines == lPart.SizeInLines;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cTextBodyPart)}({base.ToString()},{SizeInLines})";
    }

    /// <summary>
    /// Represents an IMAP BODYSTRUCTURE attribute-value pair.
    /// </summary>
    /// <remarks>
    /// The value may have a language associated with it. See RFC 2184.
    /// </remarks>
    [Serializable]
    public class cBodyStructureParameter
    {
        private readonly cBytes mRawName;

        /// <summary>
        /// The un-decoded value.
        /// </summary>
        public readonly cBytes RawValue;

        [NonSerialized]
        private string mName;

        [NonSerialized]
        private string mStringValue;

        [NonSerialized]
        private string mLanguageTag;

        internal cBodyStructureParameter(IList<byte> pName, IList<byte> pValue)
        {
            if (pName == null) throw new ArgumentNullException(nameof(pName));
            if (pValue == null) throw new ArgumentNullException(nameof(pValue));
            mRawName = new cBytes(pName);
            RawValue = new cBytes(pValue);
            ZFinishConstruct();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (mRawName == null) throw new cDeserialiseException(nameof(cBodyStructureParameter), nameof(mRawName), kDeserialiseExceptionMessage.IsNull);
            if (RawValue == null) throw new cDeserialiseException(nameof(cBodyStructureParameter), nameof(RawValue), kDeserialiseExceptionMessage.IsNull);
            // could validate that the name and value are in the correct domain 
            ZFinishConstruct();
        }

        private void ZFinishConstruct()
        {
            if (mRawName.Count == 0 || mRawName[mRawName.Count - 1] != cASCII.ASTERISK)
            {
                // just plain name=value
                mName = cMailTools.UTF8BytesToString(mRawName);
                mStringValue = cMailTools.UTF8BytesToString(RawValue);
                mLanguageTag = null;
                return;
            }

            // should be rfc 2231 syntax ... we will attempt to decode it if the IMAP server has decoded the parameter value continuations as per rfc 2231.6

            // check if there is another '*' in the name
            for (int i = 0; i < mRawName.Count - 1; i++)
                if (mRawName[i] == cASCII.ASTERISK)
                {
                    // not decoded rfc 2231, store name=value
                    mName = cMailTools.UTF8BytesToString(mRawName);
                    mStringValue = cMailTools.UTF8BytesToString(RawValue);
                    mLanguageTag = null;
                    return;
                }

            // try to parse the value

            cBytesCursor lCursor = new cBytesCursor(RawValue);

            // note that the code to get the charset is not strictly correct, as only registered charset names should be extracted
            //  also note that "'" is a valid character in a charset name but it is also the rfc 2231 delimiter, so if there were a charset with a "'" in the name this code would garble it
            //   (that is why charsetnamedash is used rather than charsetname)

            lCursor.GetToken(cCharset.CharsetNameDash, null, null, out var lCharsetName); // charset is optional

            if (lCursor.SkipByte(cASCII.QUOTE))
            {
                lCursor.GetLanguageTag(out mLanguageTag); // language tag is optional

                if (lCursor.SkipByte(cASCII.QUOTE))
                {
                    lCursor.GetToken(cCharset.All, cASCII.PERCENT, null, out cByteList lValueBytes); // the value itself is optional (!)

                    if (lCursor.Position.AtEnd && cMailTools.TryCharsetBytesToString(lCharsetName, lValueBytes, out mStringValue))
                    {
                        // successful decode of rfc 2231 syntax

                        // trim off the asterisk at the end of the name
                        byte[] lTrimmedName = new byte[mRawName.Count - 1];
                        for (int i = 0; i < mRawName.Count - 1; i++) lTrimmedName[i] = mRawName[i];

                        mName = cMailTools.UTF8BytesToString(lTrimmedName);

                        return;
                    }
                }
            }

            // failed to decode rfc 2231 syntax: store name=value
            mName = cMailTools.UTF8BytesToString(mRawName);
            mStringValue = cMailTools.UTF8BytesToString(RawValue);
            mLanguageTag = null;
        }

        /// <summary>
        /// The name of the attribute.
        /// </summary>
        public string Name => mName;

        /// <summary>
        /// The decoded value.
        /// </summary>
        public string StringValue => mStringValue;

        /// <summary>
        /// The language tag of the value. May be <see langword="null"/>.
        /// </summary>
        public string LanguageTag => mLanguageTag;

        /// <summary>
        /// Gets the value as a <see cref="UInt32"/>. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> if the value cannot be parsed as an IMAP 'number'.
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
        /// Gets the value as a <see cref="cTimestamp"/>. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Will be <see langword="null"/> if the value cannot be parsed as an RFC 5322 date-time.
        /// </remarks>
        public cTimestamp TimestampValue
        {
            get
            {
                cBytesCursor lCursor = new cBytesCursor(RawValue);
                if (lCursor.GetRFC822DateTime(out var lResult) && lCursor.Position.AtEnd) return lResult;
                return null;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cBodyStructureParameter)}({mRawName},{RawValue},{mName},{mStringValue},{mLanguageTag})";
    }

    /// <summary>
    /// An immutable collection of IMAP BODYSTRUCTURE attribute-value pairs.
    /// </summary>
    [Serializable]
    public class cBodyStructureParameters : ReadOnlyCollection<cBodyStructureParameter>
    {
        internal cBodyStructureParameters(IList<cBodyStructureParameter> pParameters) : base(pParameters) { }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            foreach (var lParameter in this) if (lParameter == null) throw new cDeserialiseException(nameof(cBodyStructureParameters), null, kDeserialiseExceptionMessage.ContainsNulls);
        }

        /// <summary>
        /// Returns the first parameter with the specified attribute name.
        /// </summary>
        /// <param name="pName"></param>
        /// <returns>The parameter if there is at least one with a matching name, otherwise <see langword="null"/>.</returns>
        public cBodyStructureParameter First(string pName) => this.FirstOrDefault(p => p.Name.Equals(pName, StringComparison.InvariantCultureIgnoreCase));

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cBodyStructureParameters));
            foreach (var lParameter in this) lBuilder.Append(lParameter);
            return lBuilder.ToString();
        }
    }
}