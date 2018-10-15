using System;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cCommandHookUIDFetch : cCommandHook
            {
                private readonly cSelectedMailbox mSelectedMailbox;
                private readonly uint mUID;
                private iMessageHandle mMessageHandle = null;

                public cCommandHookUIDFetch(cSelectedMailbox pSelectedMailbox, uint pUID)
                {
                    mSelectedMailbox = pSelectedMailbox;
                    mUID = pUID;
                }

                public iMessageHandle MessageHandle => mMessageHandle;

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandHookUIDFetch), nameof(ProcessData));

                    if (mMessageHandle == null && pData is cResponseDataFetch lFetch && lFetch.UID == mUID)
                    {
                        mMessageHandle = mSelectedMailbox.GetHandle(lFetch.MSN);
                        return eProcessDataResult.observed;
                    }

                    return eProcessDataResult.notprocessed;
                }
            }
        }
    }
}