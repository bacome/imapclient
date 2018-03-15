using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kAppendDataUTF8SpaceLParen = new cTextCommandPart("UTF8 (");

            private static readonly cBatchSizerConfiguration kSessionAppendMessageReadConfiguration = new cBatchSizerConfiguration(100000, 100000, 1, 100000); // 100k chunks

            private class cSessionAppendDataList : List<cSessionAppendData>
            {
                public cSessionAppendDataList() { }

                public override string ToString()
                {
                    var lBuilder = new cListBuilder(nameof(cSessionAppendDataList));
                    foreach (var lMessage in this) lBuilder.Append(lMessage);
                    return lBuilder.ToString();
                }
            }

            private abstract class cSessionAppendData
            {
                public readonly cStorableFlags Flags;
                public readonly DateTime? Received;

                public cSessionAppendData(cStorableFlags pFlags, DateTime? pReceived)
                {
                    Flags = pFlags;
                    Received = pReceived;
                }

                // this is the length that is used  
                //  1) to determine the batch size for groups of append 
                //  2) in the progress-setmaximum
                //
                // so it has to match the number of bytes that we are going to emit progress-increments for
                //  it should not include the bytes of command text NOR the bytes of URLs
                //
                public abstract uint Length { get; } 

                // this adds the command text (and disposables) of the 'append-data' syntax element of rfc 4466 (with the rfc 4469 catenate and rfc 6855 utf8 extensions)
                //  and returns the features used when doing so
                public abstract fIMAPCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder);

                protected fIMAPCapabilities YAddAppendData(cAppendCommandDetailsBuilder pBuilder, cLiteralCommandPartBase pPart)
                {
                    fIMAPCapabilities lCapabilities;

                    if (pPart.Binary) lCapabilities = fIMAPCapabilities.binary;
                    else lCapabilities = 0;

                    if (pBuilder.UTF8)
                    {
                        pBuilder.Add(kAppendDataUTF8SpaceLParen, pPart, cCommandPart.RParen);
                        return fIMAPCapabilities.utf8accept | fIMAPCapabilities.utf8only | lCapabilities;
                    }

                    pBuilder.Add(pPart);
                    return lCapabilities;
                }
            }

            private class cSessionMessageAppendData : cSessionAppendData
            {
                private readonly cIMAPClient mClient;
                private readonly iMessageHandle mMessageHandle;
                private readonly cSection mSection;
                private readonly uint mLength;

                public cSessionMessageAppendData(cStorableFlags pFlags, DateTime? pReceived, cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, uint pLength) : base(pFlags, pReceived)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
                    mSection = pSection ?? throw new ArgumentNullException(nameof(pSection));
                    if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                }

                public override uint Length => mLength;

                public override fIMAPCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    cIMAPMessageDataStream lStream = new cIMAPMessageDataStream(mClient, mMessageHandle, mSection, pBuilder.TargetBufferSize);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddAppendData(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.AppendDataBinary, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionMessageAppendData)}({Flags},{Received},{mMessageHandle},{mSection},{mLength})";
            }

            private class cSessionLiteralAppendData : cSessionAppendData
            {
                private cBytes mBytes;

                public cSessionLiteralAppendData(cStorableFlags pFlags, DateTime? pReceived, cBytes pBytes) : base(pFlags, pReceived)
                {
                    if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
                    if (pBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pBytes));
                    mBytes = pBytes;
                }

                public override uint Length => (uint)mBytes.Count;

                public override fIMAPCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder) => YAddAppendData(pBuilder, new cLiteralCommandPart(mBytes, pBuilder.AppendDataBinary, false, false, pBuilder.Increment));

                public override string ToString() => $"{nameof(cSessionLiteralAppendData)}({Flags},{Received},{mBytes})";
            }

            private class cSessionFileAppendData : cSessionAppendData
            {
                private readonly string mPath;
                private readonly uint mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionFileAppendData(cStorableFlags pFlags, DateTime? pReceived, string pPath, uint pLength, cBatchSizerConfiguration pReadConfiguration) : base(pFlags, pReceived)
                {
                    mPath = pPath ?? throw new ArgumentNullException(nameof(pPath));
                    if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override uint Length => mLength;

                public override fIMAPCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    FileStream lStream = new FileStream(mPath, FileMode.Open, FileAccess.Read);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddAppendData(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.AppendDataBinary, pBuilder.Increment, mReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionFileAppendData)}({Flags},{Received},{mPath},{mLength},{mReadConfiguration})";
            }

            private class cSessionStreamAppendData : cSessionAppendData
            {
                private readonly Stream mStream;
                private readonly uint mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionStreamAppendData(cStorableFlags pFlags, DateTime? pReceived, Stream pStream, uint pLength, cBatchSizerConfiguration pReadConfiguration) : base(pFlags, pReceived)
                {
                    mStream = pStream;
                    if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override uint Length => mLength;

                public override fIMAPCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder) => YAddAppendData(pBuilder, new cStreamCommandPart(mStream, mLength, pBuilder.AppendDataBinary, pBuilder.Increment, mReadConfiguration));

                public override string ToString() => $"{nameof(cSessionStreamAppendData)}({Flags},{Received},{mLength},{mReadConfiguration})";
            }

            private class cCatenateAppendData : cSessionAppendData
            {
                private static readonly cCommandPart kCATENATESpaceLParen = new cTextCommandPart("CATENATE (");

                private readonly ReadOnlyCollection<cCatenateAppendDataPart> mParts;
                private readonly uint mLength = 0;

                public cCatenateAppendData(cStorableFlags pFlags, DateTime? pReceived, IEnumerable<cCatenateAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));

                    List<cCatenateAppendDataPart> lParts = new List<cCatenateAppendDataPart>();

                    long lLength = 0;

                    foreach (var lPart in pParts)
                    {
                        if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                        lParts.Add(lPart);
                        lLength += lPart.Length;
                    }

                    if (lParts.Count == 0 || lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pParts));

                    mParts = lParts.AsReadOnly();
                    mLength = (uint)lLength;
                }

                public override uint Length => mLength;

                public override fIMAPCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    fIMAPCapabilities lCapabilities = fIMAPCapabilities.catenate;

                    pBuilder.Add(kCATENATESpaceLParen);

                    bool lFirst = true;

                    foreach (var lPart in mParts)
                    {
                        if (lFirst) lFirst = false;
                        else pBuilder.Add(cCommandPart.Space);

                        lCapabilities |= lPart.AddCatPart(pBuilder);
                    }

                    pBuilder.Add(cCommandPart.RParen);

                    return lCapabilities;
                }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cCatenateAppendData));
                    lBuilder.Append(Flags);
                    lBuilder.Append(Received);
                    lBuilder.Append(mLength);
                    foreach (var lPart in mParts) lBuilder.Append(lPart);
                    return lBuilder.ToString();
                }
            }

            private abstract class cCatenateAppendDataPart
            {
                // this is the length that is used  
                //  1) to determine the batch size for groups of append 
                //  2) in the progress-setmaximum
                //
                // so it has to match the number of bytes that we are going to emit progress-increments for
                //  it should not include the bytes of command text NOR the bytes of URLs
                //
                public abstract uint Length { get; }

                // this adds the command text (and disposables) of the 'cat-part' syntax element of rfc 4469 (with the rfc 6855 utf8 extension)
                //  i.e. the literal or url
                //
                public abstract fIMAPCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder);
            }

            private class cCatenateURLAppendDataPart : cCatenateAppendDataPart
            {
                private static readonly cCommandPart kURLSpace = new cTextCommandPart("URL ");

                private static readonly cBytes kSemicolonUIDVALIDITYEquals = new cBytes(";UIDVALIDITY=");
                private static readonly cBytes kSlashSemicolonUIDEquals = new cBytes("/;UID=");
                private static readonly cBytes kSlashSemicolonSECTIONEquals = new cBytes("/;SECTION=");
                private static readonly cBytes kHEADER = new cBytes("HEADER");
                private static readonly cBytes kTEXT = new cBytes("TEXT");

                private cCommandPart mPart;

                private cCatenateURLAppendDataPart(cCommandPart pPart) { mPart = pPart; }

                public static bool TryConstruct(iMessageHandle pMessageHandle, cSection pSection, cCommandPartFactory pFactory, out cCatenateURLAppendDataPart rPart)
                {
                    // always generates a url that starts with "/<mailboxname>", but may not generate a URL if the mailbox name/ section aren't supported

                    if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
                    if (pSection == null) throw new ArgumentNullException(nameof(pSection));
                    if (pFactory == null) throw new ArgumentNullException(nameof(pFactory));

                    // require uid
                    if (pMessageHandle.UID == null) { rPart = null; return false; }

                    // doit
                    return TryConstruct(pMessageHandle.MessageCache.MailboxHandle, pMessageHandle.UID, pSection, pFactory, out rPart);
                }

                public static bool TryConstruct(iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, cCommandPartFactory pFactory, out cCatenateURLAppendDataPart rPart)
                {
                    // always generates a url that starts with "/<mailboxname>", but may not generate a URL if the mailbox name/ section aren't supported

                    if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                    if (pUID == null) throw new ArgumentNullException(nameof(pUID));
                    if (pSection == null) throw new ArgumentNullException(nameof(pSection));
                    if (pFactory == null) throw new ArgumentNullException(nameof(pFactory));

                    // only some types of URL are supported
                    if (pSection.TextPart != eSectionTextPart.all && pSection.TextPart != eSectionTextPart.header && pSection.TextPart != eSectionTextPart.text) { rPart = null; return false; }

                    // the bytes of the url build up here
                    cByteList lBytes = new cByteList();

                    // leading slash
                    lBytes.Add(cASCII.SLASH);

                    // mailbox name

                    var lMailboxName = pMailboxHandle.MailboxName;

                    if (!pFactory.TryAsURLMailbox(lMailboxName.Path, lMailboxName.Delimiter, out var lEncodedMailboxPath)) { rPart = null; return false; }

                    foreach (byte lByte in Encoding.UTF8.GetBytes(lEncodedMailboxPath))
                    {
                        if (lByte == cASCII.SLASH || !cCharset.BChar.Contains(lByte))
                        {
                            lBytes.Add(cASCII.PERCENT);
                            lBytes.AddRange(cTools.ByteToHexBytes(lByte));
                        }
                        else lBytes.Add(lByte);
                    }

                    // uid

                    lBytes.AddRange(kSemicolonUIDVALIDITYEquals);
                    var lUIDValidity = cTools.UIntToBytesReverse(pUID.UIDValidity);
                    lUIDValidity.Reverse();
                    lBytes.AddRange(lUIDValidity);

                    lBytes.AddRange(kSlashSemicolonUIDEquals);
                    var lUID = cTools.UIntToBytesReverse(pUID.UID);
                    lUID.Reverse();
                    lBytes.AddRange(lUID);

                    // section, if any

                    if (pSection != cSection.All)
                    {
                        lBytes.AddRange(kSlashSemicolonSECTIONEquals);

                        if (pSection.Part != null)
                        {
                            lBytes.AddRange(Encoding.UTF8.GetBytes(pSection.Part));
                            if (pSection.TextPart != eSectionTextPart.all) lBytes.Add(cASCII.DOT);
                        }

                        if (pSection.TextPart == eSectionTextPart.header) lBytes.AddRange(kHEADER);
                        else if (pSection.TextPart == eSectionTextPart.text) lBytes.AddRange(kTEXT);
                    }

                    // done
                    rPart = new cCatenateURLAppendDataPart(new cTextCommandPart(lBytes));
                    return true;
                }

                public override uint Length => 0;

                public override fIMAPCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder)
                {
                    pBuilder.Add(kURLSpace, mPart);
                    return 0;
                }

                public override string ToString() => $"{nameof(cCatenateURLAppendDataPart)}({mPart})";
            }

            private class cCatenateLiteralAppendDataPart : cCatenateAppendDataPart
            {
                private static readonly cCommandPart kTEXTSpace = new cTextCommandPart("TEXT ");

                private readonly ReadOnlyCollection<cSessionAppendDataPart> mParts;
                private readonly uint mLength;

                public cCatenateLiteralAppendDataPart(IEnumerable<cSessionAppendDataPart> pParts)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));

                    List<cSessionAppendDataPart> lParts = new List<cSessionAppendDataPart>();

                    long lLength = 0;

                    foreach (var lPart in pParts)
                    {
                        if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                        lParts.Add(lPart);
                        lLength += lPart.Length;
                    }

                    if (lParts.Count == 0 || lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pParts));

                    mParts = lParts.AsReadOnly();
                    mLength = (uint)lLength;
                }

                public override uint Length => mLength;

                public override fIMAPCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder)
                {
                    var lParts = new List<cMultiPartLiteralPartBase>();
                    foreach (var lPart in mParts) lPart.AddPart(pBuilder, lParts);

                    if (pBuilder.UTF8)
                    {
                        var lCommandPart = new cMultiPartLiteralCommandPart(true, lParts);
                        pBuilder.Add(kAppendDataUTF8SpaceLParen, lCommandPart, cCommandPart.RParen);
                        return fIMAPCapabilities.utf8accept | fIMAPCapabilities.utf8only;
                    }
                    else
                    {
                        var lCommandPart = new cMultiPartLiteralCommandPart(false, lParts);
                        pBuilder.Add(kTEXTSpace, lCommandPart);
                        return 0;
                    }
                }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cCatenateLiteralAppendDataPart));
                    lBuilder.Append(mLength);
                    foreach (var lPart in mParts) lBuilder.Append(lPart);
                    return lBuilder.ToString();
                }
            }

            private class cSessionMultiPartAppendData : cSessionAppendData
            {
                private readonly ReadOnlyCollection<cSessionAppendDataPart> mParts;
                private readonly uint mLength;

                public cSessionMultiPartAppendData(cStorableFlags pFlags, DateTime? pReceived, IEnumerable<cSessionAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));

                    List<cSessionAppendDataPart> lParts = new List<cSessionAppendDataPart>();

                    long lLength = 0;

                    foreach (var lPart in pParts)
                    {
                        if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                        lParts.Add(lPart);
                        lLength += lPart.Length;
                    }

                    if (lLength == 0 || lLength > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(pParts));

                    mParts = lParts.AsReadOnly();
                    mLength = (uint)lLength;
                }

                public override uint Length => mLength;

                public override fIMAPCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    List<cMultiPartLiteralPartBase> lParts = new List<cMultiPartLiteralPartBase>();
                    foreach (var lPart in mParts) lPart.AddPart(pBuilder, lParts);
                    return YAddAppendData(pBuilder, new cMultiPartLiteralCommandPart(pBuilder.AppendDataBinary, lParts));
                }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cSessionMultiPartAppendData));
                    lBuilder.Append(Flags);
                    lBuilder.Append(Received);
                    lBuilder.Append(mLength);
                    foreach (var lPart in mParts) lBuilder.Append(lPart);
                    return lBuilder.ToString();
                }
            }

            private abstract class cSessionAppendDataPart
            {
                // this is the length that is used  
                //  1) to determine the batch size for groups of append 
                //  2) in the progress-setmaximum
                //
                // so it has to match the number of bytes that we are going to emit progress-increments for
                //  it should not include the bytes of command text
                //
                public abstract uint Length { get; }

                // this adds the command part for the part to pParts and any disposables to the pBuilder
                //
                public abstract void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts);
            }

            private class cSessionMessageAppendDataPart : cSessionAppendDataPart
            {
                private readonly cIMAPClient mClient;
                private readonly iMessageHandle mMessageHandle;
                private readonly iMailboxHandle mMailboxHandle;
                private readonly cUID mUID;
                private readonly cSection mSection;
                private readonly uint mLength;

                public cSessionMessageAppendDataPart(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, uint pLength)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
                    mMailboxHandle = null;
                    mUID = null;
                    mSection = pSection ?? throw new ArgumentNullException(nameof(pSection));

                    if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                }

                public cSessionMessageAppendDataPart(cIMAPClient pClient, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, uint pLength)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = null;
                    mMailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
                    mUID = pUID ?? throw new ArgumentNullException(nameof(pUID));
                    mSection = pSection ?? throw new ArgumentNullException(nameof(pSection));

                    if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                }

                public override uint Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts)
                {
                    cIMAPMessageDataStream lStream;

                    if (mMessageHandle == null) lStream = new cIMAPMessageDataStream(mClient, mMailboxHandle, mUID, mSection, pBuilder.TargetBufferSize);
                    else lStream = new cIMAPMessageDataStream(mClient, mMessageHandle, mSection, pBuilder.TargetBufferSize);

                    pBuilder.Add(lStream); // this is what disposes the stream
                    pParts.Add(new cMultiPartLiteralStreamPart(lStream, mLength, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionMessageAppendDataPart)}({mMessageHandle},{mMailboxHandle},{mUID},{mSection},{mLength})";
            }

            private class cSessionLiteralAppendDataPart : cSessionAppendDataPart
            {
                private cBytes mBytes;

                public cSessionLiteralAppendDataPart(IList<byte> pBytes)
                {
                    if (pBytes == null) throw new ArgumentNullException(nameof(pBytes));
                    mBytes = new cBytes(pBytes);
                }

                public override uint Length => (uint)mBytes.Count;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts) => pParts.Add(new cMultiPartLiteralPart(mBytes, pBuilder.Increment));

                public override string ToString() => $"{nameof(cSessionLiteralAppendDataPart)}({mBytes})";
            }

            private class cSessionFileAppendDataPart : cSessionAppendDataPart
            {
                private readonly string mPath;
                private readonly uint mLength;
                private readonly bool mBase64Encode;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionFileAppendDataPart(string pPath, uint pLength, bool pBase64Encode, cBatchSizerConfiguration pReadConfiguration)
                {
                    mPath = pPath;
                    if (pLength == 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mBase64Encode = pBase64Encode;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override uint Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts)
                {
                    Stream lStream = new FileStream(mPath, FileMode.Open, FileAccess.Read);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    if (mBase64Encode) lStream = new cBase64Encoder(lStream, mReadConfiguration);
                    pParts.Add(new cMultiPartLiteralStreamPart(lStream, mLength, pBuilder.Increment, mReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionFileAppendDataPart)}({mPath},{mLength},{mBase64Encode},{mReadConfiguration})";
            }

            private class cSessionStreamAppendDataPart : cSessionAppendDataPart
            {
                private readonly Stream mStream;
                private readonly uint mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionStreamAppendDataPart(Stream pStream, uint pLength, cBatchSizerConfiguration pReadConfiguration)
                {
                    mStream = pStream;
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override uint Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts) => pParts.Add(new cMultiPartLiteralStreamPart(mStream, mLength, pBuilder.Increment, mReadConfiguration));

                public override string ToString() => $"{nameof(cSessionStreamAppendDataPart)}({mLength},{mReadConfiguration})";
            }
        }
    }
}