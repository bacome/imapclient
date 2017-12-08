using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kSessionAppendDataUTF8SpaceLParen = new cTextCommandPart("UTF8 (");
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

                public abstract int Length { get; }

                public abstract void AppendData(cAppendCommandDetailsBuilder pBuilder);
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
                    if (mPart == null) mLength = (int)mMessageHandle.Size;
                    else mLength = (int)mPart.SizeInBytes;
                }

                public override int Length => mLength;

                public override void AppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    cMessageDataStream lStream = new cMessageDataStream(mClient, mMessageHandle, mPart, pBuilder.TargetBufferSize);
                    pBuilder.Add(lStream);

                    if (pBuilder.UTF8)
                    {
                        pBuilder.Add(kSessionAppendDataUTF8SpaceLParen);
                        pBuilder.Add(new cStreamCommandPart(lStream, mLength, true, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                        pBuilder.Add(cCommandPart.RParen);
                    }
                    else pBuilder.Add(new cStreamCommandPart(lStream, mLength, pBuilder.Binary, pBuilder.Increment, kSessionAppendMessageReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionMessageAppendData)}({Flags},{Received},{mMessageHandle},{mPart},{mLength})";
            }

            private class cSessionBytesAppendData : cSessionAppendData
            {
                private cBytes mBytes;

                public cSessionBytesAppendData(cStorableFlags pFlags, DateTime? pReceived, cBytes pBytes) : base(pFlags, pReceived)
                {
                    mBytes = pBytes ?? throw new ArgumentNullException(nameof(pBytes));
                    if (mBytes.Count == 0) throw new ArgumentOutOfRangeException(nameof(pBytes));
                }

                public override int Length => mBytes.Count;

                public override void AppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    if (pBuilder.UTF8)
                    {
                        pBuilder.Add(kSessionAppendDataUTF8SpaceLParen);
                        pBuilder.Add(new cLiteralCommandPart(mBytes, true, false, false, pBuilder.Increment));
                        pBuilder.Add(cCommandPart.RParen);
                    }
                    else pBuilder.Add(new cLiteralCommandPart(mBytes, pBuilder.Binary, false, false, pBuilder.Increment));
                }

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

                public override void AppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    FileStream lStream = new FileStream(mPath, FileMode.Open);
                    pBuilder.Add(lStream);

                    if (pBuilder.UTF8)
                    {
                        pBuilder.Add(kSessionAppendDataUTF8SpaceLParen);
                        pBuilder.Add(new cStreamCommandPart(lStream, mLength, true, pBuilder.Increment, mReadConfiguration));
                        pBuilder.Add(cCommandPart.RParen);
                    }
                    else pBuilder.Add(new cStreamCommandPart(lStream, mLength, pBuilder.Binary, pBuilder.Increment, mReadConfiguration));
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
                    mStream = pStream ?? throw new ArgumentNullException(nameof(pStream));
                    if (pLength < 1) throw new ArgumentOutOfRangeException(nameof(pLength));
                    mLength = pLength;
                    mReadConfiguration = pReadConfiguration ?? throw new ArgumentNullException(nameof(pReadConfiguration));
                }

                public override int Length => mLength;

                public override void AppendData(cAppendCommandDetailsBuilder pBuilder)
                {
                    if (pBuilder.UTF8)
                    {
                        pBuilder.Add(kSessionAppendDataUTF8SpaceLParen);
                        pBuilder.Add(new cStreamCommandPart(mStream, mLength, true, pBuilder.Increment, mReadConfiguration));
                        pBuilder.Add(cCommandPart.RParen);
                    }
                    else pBuilder.Add(new cStreamCommandPart(mStream, mLength, pBuilder.Binary, pBuilder.Increment, mReadConfiguration));
                }

                public override string ToString() => $"{nameof(cSessionStreamAppendData)}({Flags},{Received},{mLength},{mReadConfiguration})";
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

                    if (mLength == 0) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.);

                    mParts = lParts.AsReadOnly();
                }
            }


            // this is a catenated collection of parts
            private class cSessionCatenateAppendData : cSessionAppendData
            {
                private readonly ReadOnlyCollection<cSessionAppendDataPart> mParts;
                private readonly int mLength = 0;

                public cSessionCatenateAppendData(cStorableFlags pFlags, DateTime? pReceived, IEnumerable<cSessionAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));

                    ;?; // copy the code above

                    foreach (var lPart in pParts)
                    {
                        if (lPart == null) throw new ArgumentOutOfRangeException(nameof(pParts), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                        mLength += lPart.Length;
                    }
                }
            }

            private abstract class cSessionAppendDataPart
            {
                public abstract int Length { get; }
                public abstract cSinglePartLiteralCommandPart CommandPart { get; }
            }

            private class cSessionMessageAppendDataPart : cSessionAppendDataPart
            {
                private readonly cIMAPClient mClient;
                private readonly iMessageHandle mMessageHandle;
                private readonly cMessageBodyPart mPart;
                private readonly int mLength;

                public cSessionMessageAppendDataPart(cIMAPClient pClient, iMessageHandle pMessageHandle, cMessageBodyPart pPart)
                {
                    mClient = pClient ?? throw new ArgumentNullException(nameof(pClient));
                    mMessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
                    mPart = pPart;
                    if (mPart == null) mLength = (int)mMessageHandle.Size;
                    else mLength = (int)mPart.SizeInBytes;
                }

                public override int Length => mLength;

                public override string ToString() => $"{nameof(cSessionMessageAppendDataPart)}({mMessageHandle},{mPart},{mLength})";
            }




            /*
            ;?; // note that the multipart may have to be converted to a single part if catenate isn't supported

            private class cCatenateAppendData : cAppendData
            {
                public readonly ReadOnlyCollection<cAppendDataPart> Parts;

                public cCatenateAppendData(cStorableFlags pFlags, DateTime? pReceived, cAppendDataPart pPart) : base(pFlags, pReceived)
                {
                    if (pPart == null) throw new ArgumentNullException(nameof(pPart));
                    List<cAppendDataPart> lParts = new List<cAppendDataPart>();
                    lParts.Add(pPart);
                    Parts = lParts.AsReadOnly();
                }

                public cCatenateAppendData(cStorableFlags pFlags, DateTime? pReceived, List<cAppendDataPart> pParts) : base(pFlags, pReceived)
                {
                    if (pParts == null) throw new ArgumentNullException(nameof(pParts));
                    if (pParts.Count == 0) throw new ArgumentOutOfRangeException(nameof(pParts));
                    Parts = pParts.AsReadOnly();
                }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cCatenateAppendData));
                    lBuilder.Append(Flags);
                    lBuilder.Append(Received);
                    foreach (var lPart in Parts) lBuilder.Append(lPart);
                    return lBuilder.ToString();
                }
            }

            private class cURLAppendDataPart : cAppendDataPart
            {
                public readonly string URL;

                public cURLAppendDataPart(imailboxh)
                {
                    URL = pURL ?? throw new ArgumentNullException(nameof(pURL));
                }
            } */
        }
    }
}