using System;
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
            private static readonly cCommandPart kSessionAppendDataUTF8SpaceLParen = new cTextCommandPart("UTF8 (");
            private static readonly cCommandPart kSessionAppendDataCATENATESpaceLParen = new cTextCommandPart("CATENATE (");
            private static readonly cCommandPart kSessionAppendDataTEXTSpace = new cTextCommandPart("TEXT ");
            private static readonly cCommandPart kSessionAppendDataURLSpace = new cTextCommandPart("URL ");

            private static readonly cBatchSizerConfiguration kSessionAppendMessageReadConfiguration = new cBatchSizerConfiguration(100000, 100000, 1, 100000); // 100k chunks

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
                //  2) in the progress-setcount
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
                        pBuilder.Add(kSessionAppendDataUTF8SpaceLParen, pPart, cCommandPart.RParen);
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
                private readonly cMessageBodyPart mPart;
                private readonly int mLength;

                public cSessionMessageAppendData(cStorableFlags pFlags, DateTime? pReceived, cIMAPClient pClient, iMessageHandle pMessageHandle, cMessageBodyPart pPart) : base(pFlags, pReceived)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));

                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));

                    mPart = pPart;

                    if (pPart == null)
                    {
                        if (pMessageHandle.Size == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
                        mLength = (int)pMessageHandle.Size;
                    }
                    else mLength = (int)pPart.SizeInBytes;
                }

                public override int Length => mLength;

                public override fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    cMessageDataStream lStream = new cMessageDataStream(mClient, mMessageHandle, mPart, pBuilder.TargetBufferSize);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddAppendData(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.AppendDataBinary, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionMessageAppendData)}({Flags},{Received},{mMessageHandle},{mPart},{mLength})";
            }

            private class cSessionBytesAppendData : cSessionAppendData
            {
                private cBytes mBytes;

                /*
                public cSessionBytesAppendData(cStorableFlags pFlags, DateTime? pReceived, cBytes pBytes) : base(pFlags, pReceived)
                {
                    mBytes = pBytes ?? throw new ArgumentNullException(nameof(pBytes));
                    if (mBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pBytes));
                } */

                public cSessionBytesAppendData(cStringAppendData pString, cStorableFlags pDefaultFlags) : base(pString.Flags ?? pDefaultFlags, pString.Received)
                {
                    mBytes = new cBytes(Encoding.UTF8.GetBytes(pString.String));
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

                public cSessionFileAppendData(cFileAppendData pFile, cStorableFlags pDefaultFlags, cBatchSizerConfiguration pDefaultConfiguration) : base(pFile.Flags ?? pDefaultFlags, pFile.Received)
                {
                    mPath = pFile.Path;
                    mLength = pFile.Length;
                    mReadConfiguration = pFile.ReadConfiguration ?? pDefaultConfiguration ?? throw new ArgumentNullException(nameof(pDefaultConfiguration));
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

                public cSessionStreamAppendData(cStreamAppendData pStream, cStorableFlags pDefaultFlags, cBatchSizerConfiguration pDefaultConfiguration) : base(pStream.Flags ?? pDefaultFlags, pStream.Received)
                {
                    mStream = pStream.Stream;
                    mLength = pStream.Length;
                    mReadConfiguration = pStream.ReadConfiguration ?? pDefaultConfiguration ?? throw new ArgumentNullException(nameof(pDefaultConfiguration));
                }

                public override int Length => mLength;

                public override fCapabilities AddAppendData(cAppendCommandDetailsBuilder pBuilder) => YAddAppendData(pBuilder, new cStreamCommandPart(mStream, mLength, pBuilder.AppendDataBinary, pBuilder.Increment, mReadConfiguration));

                public override string ToString() => $"{nameof(cSessionStreamAppendData)}({Flags},{Received},{mLength},{mReadConfiguration})";
            }

            private class cSessionCatenateAppendData : cSessionAppendData
            {
                private readonly ReadOnlyCollection<cSessionCatenateAppendDataPart> mParts;
                private readonly int mLength = 0;

                public cSessionCatenateAppendData(cStorableFlags pFlags, DateTime? pReceived, IEnumerable<cSessionCatenateAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));

                    List<cSessionCatenateAppendDataPart> lParts = new List<cSessionCatenateAppendDataPart>();

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
                    pBuilder.Add(kSessionAppendDataCATENATESpaceLParen);
                    foreach (var lPart in mParts) lCapabilities |= lPart.AddCatPart(pBuilder);
                    pBuilder.Add(cCommandPart.RParen);
                    return lCapabilities;
                }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cSessionCatenateAppendData));
                    lBuilder.Append(Flags);
                    lBuilder.Append(Received);
                    lBuilder.Append(mLength);
                    foreach (var lPart in mParts) lBuilder.Append(lPart);
                    return lBuilder.ToString();
                }
            }

            private abstract class cSessionCatenateAppendDataPart
            {
                // this is the length that is used  
                //  1) to determine the batch size for groups of append 
                //  2) in the progress-setcount
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
                        pBuilder.Add(kSessionAppendDataUTF8SpaceLParen, pPart, cCommandPart.RParen);
                        return fCapabilities.utf8accept | fCapabilities.utf8only | lCapabilities;
                    }

                    pBuilder.Add(kSessionAppendDataTEXTSpace, pPart);
                    return lCapabilities;
                }
            }

            private class cSessionCatenateURLAppendDataPart : cSessionCatenateAppendDataPart
            {
                private cCommandPart mPart;

                public cSessionCatenateURLAppendDataPart(iMessageHandle pMessageHandle, cSection pSection)
                {
                    if (pMessageHandle == null) throw new ArgumentNullException(nameof(pMessageHandle));
                    if (pSection == null) throw new ArgumentNullException(nameof(pSection));

                    if (pMessageHandle.UID == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));

                    // the URL generated is always starts with a / and the full mailbox name path and all . and / characters in mailbox path are percent encoded 


                    ;?;
                    mPart = pPart;
                }

                public override int Length => 0;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder)
                {
                    pBuilder.Add(kSessionAppendDataURLSpace, mPart);
                    return 0;
                }

                public override string ToString() => $"{nameof(cSessionCatenateURLAppendDataPart)}({mPart})";
            }

            private class cSessionCatenateMessageAppendDataPart : cSessionCatenateAppendDataPart
            {
                private readonly cIMAPClient mClient;
                private readonly iMessageHandle mMessageHandle;
                private readonly cSinglePartBody mPart;
                private readonly int mLength;

                public cSessionCatenateMessageAppendDataPart(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));

                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));

                    mPart = pPart;

                    if (pPart == null)
                    {
                        if (pMessageHandle.Size == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
                        mLength = (int)pMessageHandle.Size;
                    }
                    else mLength = (int)pPart.SizeInBytes;
                }

                public override int Length => mLength;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder)
                {
                    cMessageDataStream lStream = new cMessageDataStream(mClient, mMessageHandle, mPart, pBuilder.TargetBufferSize);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddCatPart(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.CatPartBinary, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionCatenateMessageAppendDataPart)}({mMessageHandle},{mPart},{mLength})";
            }

            private class cSessionCatenateBytesAppendDataPart : cSessionCatenateAppendDataPart
            {
                private cBytes mBytes;

                public cSessionCatenateBytesAppendDataPart(cBytes pBytes)
                {
                    mBytes = pBytes ?? throw new ArgumentNullException(nameof(pBytes));
                    if (mBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pBytes));
                }

                public override int Length => mBytes.Count;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder) => YAddCatPart(pBuilder, new cLiteralCommandPart(mBytes, pBuilder.CatPartBinary, false, false, pBuilder.Increment));

                public override string ToString() => $"{nameof(cSessionCatenateBytesAppendDataPart)}({mBytes})";
            }

            private class cSessionCatenateFileAppendDataPart : cSessionCatenateAppendDataPart
            {
                private readonly string mPath;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionCatenateFileAppendDataPart(cFileAppendDataPart pFile, cBatchSizerConfiguration pDefault)
                {
                    mPath = pFile.Path;
                    mLength = pFile.Length;
                    mReadConfiguration = pFile.ReadConfiguration ?? pDefault ?? throw new ArgumentNullException(nameof(pDefault));
                }

                public override int Length => mLength;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder)
                {
                    FileStream lStream = new FileStream(mPath, FileMode.Open);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    return YAddCatPart(pBuilder, new cStreamCommandPart(lStream, mLength, pBuilder.CatPartBinary, pBuilder.Increment, mReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionCatenateFileAppendDataPart)}({mPath},{mLength},{mReadConfiguration})";
            }

            private class cSessionCatenateStreamAppendDataPart : cSessionCatenateAppendDataPart
            {
                private readonly Stream mStream;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionCatenateStreamAppendDataPart(cStreamAppendDataPart pStream, cBatchSizerConfiguration pDefault)
                {
                    mStream = pStream.Stream;
                    mLength = pStream.Length;
                    mReadConfiguration = pStream.ReadConfiguration ?? pDefault ?? throw new ArgumentNullException(nameof(pDefault));
                }

                public override int Length => mLength;

                public override fCapabilities AddCatPart(cAppendCommandDetailsBuilder pBuilder) => YAddCatPart(pBuilder, new cStreamCommandPart(mStream, mLength, pBuilder.CatPartBinary, pBuilder.Increment, mReadConfiguration));

                public override string ToString() => $"{nameof(cSessionCatenateStreamAppendDataPart)}({mLength},{mReadConfiguration})";
            }

            private class cSessionMultiPartAppendData : cSessionAppendData
            {
                private readonly ReadOnlyCollection<cSessionMultiPartAppendDataPart> mParts;
                private readonly int mLength = 0;

                public cSessionMultiPartAppendData(cStorableFlags pFlags, DateTime? pReceived, IEnumerable<cSessionMultiPartAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));

                    List<cSessionMultiPartAppendDataPart> lParts = new List<cSessionMultiPartAppendDataPart>();

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

            private abstract class cSessionMultiPartAppendDataPart
            {
                // this is the length that is used  
                //  1) to determine the batch size for groups of append 
                //  2) in the progress-setcount
                //
                // so it has to match the number of bytes that we are going to emit progress-increments for
                //  it should not include the bytes of command text
                //
                public abstract int Length { get; }

                // this adds the command part for the part to pParts and any disposables to the pBuilder
                //
                public abstract void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts);
            }

            private class cSessionMultiPartMessageAppendDataPart : cSessionMultiPartAppendDataPart
            {
                private readonly cIMAPClient mClient;
                private readonly iMessageHandle mMessageHandle;
                private readonly cSinglePartBody mPart;
                private readonly int mLength;

                public cSessionMultiPartMessageAppendDataPart(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));

                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));

                    mPart = pPart;

                    if (pPart == null)
                    {
                        if (pMessageHandle.Size == null) throw new ArgumentOutOfRangeException(nameof(pMessageHandle));
                        mLength = (int)pMessageHandle.Size;
                    }
                    else mLength = (int)pPart.SizeInBytes;
                }

                public override int Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts)
                {
                    cMessageDataStream lStream = new cMessageDataStream(mClient, mMessageHandle, mPart, pBuilder.TargetBufferSize);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    pParts.Add(new cMultiPartLiteralStreamPart(lStream, mLength, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionMultiPartMessageAppendDataPart)}({mMessageHandle},{mPart},{mLength})";
            }

            private class cSessionMultiPartBytesAppendDataPart : cSessionMultiPartAppendDataPart
            {
                private cBytes mBytes;

                public cSessionMultiPartBytesAppendDataPart(cBytes pBytes)
                {
                    mBytes = pBytes ?? throw new ArgumentNullException(nameof(pBytes));
                    if (mBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pBytes));
                }

                public override int Length => mBytes.Count;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts) => pParts.Add(new cMultiPartLiteralPart(mBytes, pBuilder.Increment));

                public override string ToString() => $"{nameof(cSessionMultiPartBytesAppendDataPart)}({mBytes})";
            }

            private class cSessionMultiPartFileAppendDataPart : cSessionMultiPartAppendDataPart
            {
                private readonly string mPath;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionMultiPartFileAppendDataPart(cFileAppendDataPart pFile, cBatchSizerConfiguration pDefault)
                {
                    mPath = pFile.Path;
                    mLength = pFile.Length;
                    mReadConfiguration = pFile.ReadConfiguration ?? pDefault ?? throw new ArgumentNullException(nameof(pDefault));
                }

                public override int Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts)
                {
                    FileStream lStream = new FileStream(mPath, FileMode.Open);
                    pBuilder.Add(lStream); // this is what disposes the stream
                    pParts.Add(new cMultiPartLiteralStreamPart(lStream, mLength, pBuilder.Increment, mReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionMultiPartFileAppendDataPart)}({mPath},{mLength},{mReadConfiguration})";
            }

            private class cSessionMultiPartStreamAppendDataPart : cSessionMultiPartAppendDataPart
            {
                private readonly Stream mStream;
                private readonly int mLength;
                private readonly cBatchSizerConfiguration mReadConfiguration;

                public cSessionMultiPartStreamAppendDataPart(cStreamAppendDataPart pStream, cBatchSizerConfiguration pDefault)
                {
                    mStream = pStream.Stream;
                    mLength = pStream.Length;
                    mReadConfiguration = pStream.ReadConfiguration ?? pDefault ?? throw new ArgumentNullException(nameof(pDefault));
                }

                public override int Length => mLength;

                public override void AddPart(cAppendCommandDetailsBuilder pBuilder, List<cMultiPartLiteralPartBase> pParts) => pParts.Add(new cMultiPartLiteralStreamPart(mStream, mLength, pBuilder.Increment, mReadConfiguration));

                public override string ToString() => $"{nameof(cSessionMultiPartStreamAppendDataPart)}({mLength},{mReadConfiguration})";
            }
        }
    }
}