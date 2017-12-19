﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using work.bacome.imapclient.support;

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
                public abstract int Length { get; } 

                // this adds the command text (and disposables) of the 'append-data' syntax element of rfc 4466 (with the rfc 4469 catenate and rfc 6855 utf8 extensions)
                //  and returns the features used when doing so
                public abstract fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder);

                protected fCapabilities YAddAppendData(cAppendCommandDetailsBuilder pBuilder, cLiteralCommandPartBase pPart)
                {
                    fCapabilities lCapabilities;

                    if (pPart.Binary) lCapabilities = fCapabilities.binary;
                    else lCapabilities = 0;

                    if (pBuilder.UTF8)
                    {
                        pBuilder.Add(kAppendDataUTF8SpaceLParen, pPart, cCommandPart.RParen);
                        return fCapabilities.utf8accept | fCapabilities.utf8only | lCapabilities;
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
                private readonly int mLength;

                public cSessionMessageAppendData(cStorableFlags pFlags, DateTime? pReceived, cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, int pLength) : base(pFlags, pReceived)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
                    mSection = pSection ?? throw new ArgumentNullException(nameof(pSection));
                    if (pLength < 1) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                }

                public override int Length => mLength;

                public override fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    cMessageDataStream lStream = new cMessageDataStream(mClient, mMessageHandle, mSection, pBuilder.TargetBufferSize);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddAppendData(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.AppendDataBinary, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionMessageAppendData)}({Flags},{Received},{mMessageHandle},{mSection},{mLength})";
            }

            private class cSessionBytesAppendData : cSessionAppendData
            {
                private cBytes mBytes;

                public cSessionBytesAppendData(cStorableFlags pFlags, DateTime? pReceived, string pString) : base(pFlags, pReceived)
                {
                    mBytes = new cBytes(Encoding.UTF8.GetBytes(pString));
                    if (mBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pString));
                }

                public override int Length => mBytes.Count;

                public override fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder) => YAddAppendData(pBuilder, new cLiteralCommandPart(mBytes, pBuilder.AppendDataBinary, false, false, pBuilder.Increment));

                public override string ToString() => $"{nameof(cSessionBytesAppendData)}({Flags},{Received},{mBytes})";
            }

            private class cSessionFileAppendData : cSessionAppendData
            {
                private readonly string mPath;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionFileAppendData(cStorableFlags pFlags, DateTime? pReceived, string pPath, int pLength, cBatchSizerConfiguration pReadConfiguration) : base(pFlags, pReceived)
                {
                    mPath = pPath ?? throw new ArgumentNullException(nameof(pPath));
                    if (pLength < 1) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override int Length => mLength;

                public override fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    FileStream lStream = new FileStream(mPath, FileMode.Open);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddAppendData(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.AppendDataBinary, pBuilder.Increment, mReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionFileAppendData)}({Flags},{Received},{mPath},{mLength},{mReadConfiguration})";
            }

            private class cSessionStreamAppendData : cSessionAppendData
            {
                private readonly Stream mStream;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionStreamAppendData(cStorableFlags pFlags, DateTime? pReceived, Stream pStream, int pLength, cBatchSizerConfiguration pReadConfiguration) : base(pFlags, pReceived)
                {
                    mStream = pStream;
                    if (pLength < 1) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override int Length => mLength;

                public override fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder) => YAddAppendData(pBuilder, new cStreamCommandPart(mStream, mLength, pBuilder.AppendDataBinary, pBuilder.Increment, mReadConfiguration));

                public override string ToString() => $"{nameof(cSessionStreamAppendData)}({Flags},{Received},{mLength},{mReadConfiguration})";
            }

            private class cCatenateAppendData : cSessionAppendData
            {
                private static readonly cCommandPart kCATENATESpaceLParen = new cTextCommandPart("CATENATE (");

                private readonly ReadOnlyCollection<cCatenateAppendDataPart> mParts;
                private readonly int mLength = 0;

                public cCatenateAppendData(cStorableFlags pFlags, DateTime? pReceived, IEnumerable<cCatenateAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));

                    List<cCatenateAppendDataPart> lParts = new List<cCatenateAppendDataPart>();

                    foreach (var lPart in pParts)
                    {
                        if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                        lParts.Add(lPart);
                        mLength += lPart.Length;
                    }

                    if (lParts.Count == 0) throw new ArgumentOutOfRangeException(nameof(pParts));

                    mParts = lParts.AsReadOnly();
                }

                public override int Length => mLength;

                public override fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    fCapabilities lCapabilities = fCapabilities.catenate;
                    pBuilder.Add(kCATENATESpaceLParen);
                    foreach (var lPart in mParts) lCapabilities |= lPart.AddCatPart(pBuilder);
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
                private static readonly cCommandPart kTEXTSpace = new cTextCommandPart("TEXT ");

                // this is the length that is used  
                //  1) to determine the batch size for groups of append 
                //  2) in the progress-setmaximum
                //
                // so it has to match the number of bytes that we are going to emit progress-increments for
                //  it should not include the bytes of command text NOR the bytes of URLs
                //
                public abstract int Length { get; }

                // this adds the command text (and disposables) of the 'cat-part' syntax element of rfc 4469 (with the rfc 6855 utf8 extension)
                //  i.e. the literal or url
                //
                public abstract fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder);

                protected fCapabilities YAddCatPart(cAppendCommandDetailsBuilder pBuilder, cLiteralCommandPartBase pPart)
                {
                    fCapabilities lCapabilities;

                    if (pPart.Binary) lCapabilities = fCapabilities.binary;
                    else lCapabilities = 0;

                    if (pBuilder.UTF8)
                    {
                        pBuilder.Add(kAppendDataUTF8SpaceLParen, pPart, cCommandPart.RParen);
                        return fCapabilities.utf8accept | fCapabilities.utf8only | lCapabilities;
                    }

                    pBuilder.Add(kTEXTSpace, pPart);
                    return lCapabilities;
                }
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

                    // should the transmitted value include the quotes? the RFC seems clear - mailbox does include quotes if they are required
                    //  HOWEVER: then you have to ask: should it include the literal header (the {length}) and then, should it include the binary, literal+/- etc?
                    //  at this stage then I've assumed then that it should include the content only and none of the decoration that is required to get it over the wire 
                    //  BUT: this still leaves the UTF7 conversion, which I've assumed is required
                    //
                    if (!pFactory.TryAsMailbox(lMailboxName.Path, lMailboxName.Delimiter, out _, out var lEncodedMailboxPath)) { rPart = null; return false; }

                    foreach (byte lByte in Encoding.UTF8.GetBytes(lEncodedMailboxPath))
                    {
                        if (lByte == cASCII.SLASH || !cCharset.BChar.Contains(lByte))
                        {
                            lBytes.Add(cASCII.PERCENT);
                            LAddHexDigit(lByte >> 4);
                            LAddHexDigit(lByte & 0b1111);
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

                    void LAddHexDigit(int pNibble)
                    {
                        if (pNibble < 10) lBytes.Add((byte)(cASCII.ZERO + pNibble));
                        else if (pNibble < 16) lBytes.Add((byte)(cASCII.A + pNibble - 10));
                        else throw new cInternalErrorException();
                    }

                }

                public override int Length => 0;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder)
                {
                    pBuilder.Add(kURLSpace, mPart);
                    return 0;
                }

                public override string ToString() => $"{nameof(cCatenateURLAppendDataPart)}({mPart})";
            }

            private class cCatenateMessageAppendDataPart : cCatenateAppendDataPart
            {
                private readonly cIMAPClient mClient;
                private readonly iMessageHandle mMessageHandle;
                private readonly iMailboxHandle mMailboxHandle;
                private readonly cUID mUID;
                private readonly cSection mSection;
                private readonly int mLength;
            
                public cCatenateMessageAppendDataPart(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, int pLength)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
                    mMailboxHandle = null;
                    mUID = null;
                    mSection = pSection ?? throw new ArgumentNullException(nameof(pSection));

                    if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                }

                public cCatenateMessageAppendDataPart(cIMAPClient pClient, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, int pLength)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = null;
                    mMailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
                    mUID = pUID ?? throw new ArgumentNullException(nameof(pUID));
                    mSection = pSection ?? throw new ArgumentNullException(nameof(pSection));

                    if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                }

                public override int Length => mLength;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder)
                {
                    cMessageDataStream lStream;

                    if (mMessageHandle == null) lStream = new cMessageDataStream(mClient, mMailboxHandle, mUID, mSection, pBuilder.TargetBufferSize);
                    else lStream = new cMessageDataStream(mClient, mMessageHandle, mSection, pBuilder.TargetBufferSize);

                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddCatPart(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.CatPartBinary, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cCatenateMessageAppendDataPart)}({mMessageHandle},{mMailboxHandle},{mUID},{mSection},{mLength})";
            }

            private class cCatenateBytesAppendDataPart : cCatenateAppendDataPart
            {
                private cBytes mBytes;

                public cCatenateBytesAppendDataPart(string pString)
                {
                    if (pString == null) throw new ArgumentNullException(nameof(pString));
                    mBytes = new cBytes(Encoding.UTF8.GetBytes(pString));
                }

                public override int Length => mBytes.Count;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder) => YAddCatPart(pBuilder, new cLiteralCommandPart(mBytes, pBuilder.CatPartBinary, false, false, pBuilder.Increment));

                public override string ToString() => $"{nameof(cCatenateBytesAppendDataPart)}({mBytes})";
            }

            private class cCatenateFileAppendDataPart : cCatenateAppendDataPart
            {
                private readonly string mPath;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cCatenateFileAppendDataPart(string pPath, int pLength, cBatchSizerConfiguration pReadConfiguration)
                {
                    mPath = pPath;
                    if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override int Length => mLength;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder)
                {
                    FileStream lStream = new FileStream(mPath, FileMode.Open);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddCatPart(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.CatPartBinary, pBuilder.Increment, mReadConfiguration));
                }

                public override string ToString() => $"{nameof(cCatenateFileAppendDataPart)}({mPath},{mLength},{mReadConfiguration})";
            }

            private class cCatenateStreamAppendDataPart : cCatenateAppendDataPart
            {
                private readonly Stream mStream;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cCatenateStreamAppendDataPart(Stream pStream, int pLength, cBatchSizerConfiguration pReadConfiguration)
                {
                    mStream = pStream;
                    if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override int Length => mLength;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder) => YAddCatPart(pBuilder, new cStreamCommandPart(mStream, mLength, pBuilder.CatPartBinary, pBuilder.Increment, mReadConfiguration));

                public override string ToString() => $"{nameof(cCatenateStreamAppendDataPart)}({mLength},{mReadConfiguration})";
            }

            private class cSessionMultiPartAppendData : cSessionAppendData
            {
                private readonly ReadOnlyCollection<cSessionAppendDataPart> mParts;
                private readonly int mLength = 0;

                public cSessionMultiPartAppendData(cStorableFlags pFlags, DateTime? pReceived, IEnumerable<cSessionAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));

                    List<cSessionAppendDataPart> lParts = new List<cSessionAppendDataPart>();

                    foreach (var lPart in pParts)
                    {
                        if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                        lParts.Add(lPart);
                        mLength += lPart.Length;
                    }

                    if (mLength == 0) throw new ArgumentOutOfRangeException(nameof(pParts));

                    mParts = lParts.AsReadOnly();
                }

                public override int Length => mLength;

                public override fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
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
                public abstract int Length { get; }

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
                private readonly int mLength;

                public cSessionMessageAppendDataPart(cIMAPClient pClient, iMessageHandle pMessageHandle, cSection pSection, int pLength)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
                    mMailboxHandle = null;
                    mUID = null;
                    mSection = pSection ?? throw new ArgumentNullException(nameof(pSection));

                    if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                }

                public cSessionMessageAppendDataPart(cIMAPClient pClient, iMailboxHandle pMailboxHandle, cUID pUID, cSection pSection, int pLength)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = null;
                    mMailboxHandle = pMailboxHandle ?? throw new ArgumentNullException(nameof(pMailboxHandle));
                    mUID = pUID ?? throw new ArgumentNullException(nameof(pUID));
                    mSection = pSection ?? throw new ArgumentNullException(nameof(pSection));

                    if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                }

                public override int Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts)
                {
                    cMessageDataStream lStream;

                    if (mMessageHandle == null) lStream = new cMessageDataStream(mClient, mMailboxHandle, mUID, mSection, pBuilder.TargetBufferSize);
                    else lStream = new cMessageDataStream(mClient, mMessageHandle, mSection, pBuilder.TargetBufferSize);

                    pBuilder.Add(lStream); // this is what disposes the stream
                    pParts.Add(new cMultiPartLiteralStreamPart(lStream, mLength, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionMessageAppendDataPart)}({mMessageHandle},{mMailboxHandle},{mUID},{mSection},{mLength})";
            }

            private class cSessionBytesAppendDataPart : cSessionAppendDataPart
            {
                private cBytes mBytes;

                public cSessionBytesAppendDataPart(string pString)
                {
                    if (pString == null) throw new ArgumentNullException(nameof(pString));
                    mBytes = new cBytes(Encoding.UTF8.GetBytes(pString));
                }

                public override int Length => mBytes.Count;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts) => pParts.Add(new cMultiPartLiteralPart(mBytes, pBuilder.Increment));

                public override string ToString() => $"{nameof(cSessionBytesAppendDataPart)}({mBytes})";
            }

            private class cSessionFileAppendDataPart : cSessionAppendDataPart
            {
                private readonly string mPath;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionFileAppendDataPart(string pPath, int pLength, cBatchSizerConfiguration pReadConfiguration)
                {
                    mPath = pPath;
                    if (pLength < 0) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override int Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts)
                {
                    FileStream lStream = new FileStream(mPath, FileMode.Open);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    pParts.Add(new cMultiPartLiteralStreamPart(lStream, mLength, pBuilder.Increment, mReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionFileAppendDataPart)}({mPath},{mLength},{mReadConfiguration})";
            }

            private class cSessionStreamAppendDataPart : cSessionAppendDataPart
            {
                private readonly Stream mStream;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionStreamAppendDataPart(Stream pStream, int pLength, cBatchSizerConfiguration pReadConfiguration)
                {
                    mStream = pStream;
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override int Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts) => pParts.Add(new cMultiPartLiteralStreamPart(mStream, mLength, pBuilder.Increment, mReadConfiguration));

                public override string ToString() => $"{nameof(cSessionStreamAppendDataPart)}({mLength},{mReadConfiguration})";
            }
        }
    }
}