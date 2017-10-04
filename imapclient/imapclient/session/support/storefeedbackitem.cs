using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cStoreFeedbackItem
            {
                public readonly cUID UID;
                public readonly iMessageHandle Handle;
                public bool Fetched = false;
                public bool Modified = false;

                public cStoreFeedbackItem(cUID pUID)
                {
                    UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
                    Handle = null;
                }

                public cStoreFeedbackItem(iMessageHandle pHandle)
                {
                    UID = null;
                    Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
                }
            }
        }
    }
}