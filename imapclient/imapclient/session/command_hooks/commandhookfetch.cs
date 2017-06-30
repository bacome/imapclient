using System;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cCommandHookFetchBase : cCommandHook
            {
                private static readonly cBytes kFetchSpace = new cBytes("FETCH ");

                private cCapability mCapability;
                private bool mBinary;
                private cSection mSection;
                private uint mOrigin;
                private uint mTo = 0;

                public cCommandHookFetchBase(cCapability pCapability, bool pBinary, cSection pSection, uint pOrigin)
                {
                    mCapability = pCapability;
                    mBinary = pBinary;
                    mSection = pSection;
                    mOrigin = pOrigin;
                }

                public cBody Body { get; private set; } = null; // note that this body may start before the origin position requested and may be longer or shorter than requested

                public override eProcessDataResult ProcessData(cBytesCursor pCursor, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookFetchBase), nameof(ProcessData));

                    cResponseDataFetch lFetch;

                    if (pCursor.Parsed)
                    {
                        lFetch = pCursor.ParsedAs as cResponseDataFetch;
                        if (lFetch == null) return eProcessDataResult.notprocessed;
                    }
                    else
                    {
                        if (!pCursor.GetNZNumber(out _, out var lMSN) || !pCursor.SkipByte(cASCII.SPACE) || !pCursor.SkipBytes(kFetchSpace)) return eProcessDataResult.notprocessed;

                        if (!cResponseDataFetch.Process(pCursor, lMSN, mCapability, out lFetch, lContext))
                        {
                            lContext.TraceWarning("likely malformed fetch response");
                            return eProcessDataResult.notprocessed;
                        }
                    }

                    if (!IsThisTheMessageThatIAmInterestedIn(lFetch)) return eProcessDataResult.notprocessed;

                    eProcessDataResult lResult = eProcessDataResult.notprocessed;

                    foreach (var lBody in lFetch.Bodies)
                    {
                        uint lOrigin = (lBody.Origin ?? 0);

                        if (lBody.Binary == mBinary && lBody.Section == mSection && lOrigin <= mOrigin)
                        {
                            uint lTo = lOrigin + (uint)lBody.Bytes.Count;

                            if (lTo >= mOrigin)
                            {
                                lResult = eProcessDataResult.observed;

                                if (Body == null || lTo > mTo)
                                {
                                    Body = lBody;
                                    mTo = lTo;
                                }
                            }
                        }
                    }

                    return lResult;
                }

                protected abstract bool IsThisTheMessageThatIAmInterestedIn(cResponseDataFetch pFetch);
            }

            private class cCommandHookFetchMSN : cCommandHookFetchBase
            {
                private uint mMSN;

                public cCommandHookFetchMSN(cCapability pCapability, bool pBinary, cSection pSection, uint pOrigin, uint pMSN) : base (pCapability, pBinary, pSection, pOrigin)
                {
                    mMSN = pMSN;
                }

                protected override bool IsThisTheMessageThatIAmInterestedIn(cResponseDataFetch pFetch) => pFetch.MSN == mMSN;
            }

            private class cCommandHookFetchUID : cCommandHookFetchBase
            {
                private uint mUID;

                public cCommandHookFetchUID(cCapability pCapability, bool pBinary, cSection pSection, uint pOrigin, uint pUID) : base (pCapability, pBinary, pSection, pOrigin)
                {
                    mUID = pUID;
                }

                protected override bool IsThisTheMessageThatIAmInterestedIn(cResponseDataFetch pFetch) => pFetch.UID == mUID;
            }
        }
    }
}