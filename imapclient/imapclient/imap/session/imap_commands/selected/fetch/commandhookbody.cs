using System;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cCommandHookFetchBody : cCommandHook
            {
                private readonly bool mBinary;
                private readonly cSection mSection;
                private readonly uint mOrigin;
                private uint mTo = 0;

                public cCommandHookFetchBody(bool pBinary, cSection pSection, uint pOrigin)
                {
                    mBinary = pBinary;
                    mSection = pSection;
                    mOrigin = pOrigin;
                }

                public cBody Body { get; private set; } = null; // note that this body may start before the origin position requested and may be longer or shorter than requested

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookFetchBody), nameof(ProcessData));

                    if (!(pData is cResponseDataFetch lFetch)) return eProcessDataResult.notprocessed;
                    if (!IsThisTheMessageThatIAmInterestedIn(lFetch)) return eProcessDataResult.notprocessed;

                    eProcessDataResult lResult = eProcessDataResult.notprocessed;

                    foreach (var lBody in lFetch.Bodies)
                    {
                        uint lOrigin = (lBody.Origin ?? 0);

                        if (lBody.Binary == mBinary && lBody.Section == mSection && lOrigin <= mOrigin)
                        {
                            if (lBody.Bytes == null)
                            {
                                // this is the case where NIL is returned by the server (e.g. * 1 FETCH (BODY[] NIL))
                                //  most likely because the message has been expunged
                                if (lOrigin >= mOrigin) lResult = eProcessDataResult.observed;
                            }
                            else
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
                    }

                    return lResult;
                }

                protected abstract bool IsThisTheMessageThatIAmInterestedIn(cResponseDataFetch pFetch);
            }

            private class cCommandHookFetchBodyMSN : cCommandHookFetchBody
            {
                private readonly uint mMSN;

                public cCommandHookFetchBodyMSN(bool pBinary, cSection pSection, uint pOrigin, uint pMSN) : base (pBinary, pSection, pOrigin)
                {
                    mMSN = pMSN;
                }

                protected override bool IsThisTheMessageThatIAmInterestedIn(cResponseDataFetch pFetch) => pFetch.MSN == mMSN;
            }

            private class cCommandHookFetchBodyUID : cCommandHookFetchBody
            {
                private readonly uint mUID;

                public cCommandHookFetchBodyUID(bool pBinary, cSection pSection, uint pOrigin, uint pUID) : base (pBinary, pSection, pOrigin)
                {
                    mUID = pUID;
                }

                protected override bool IsThisTheMessageThatIAmInterestedIn(cResponseDataFetch pFetch) => pFetch.UID == mUID;
            }
        }
    }
}