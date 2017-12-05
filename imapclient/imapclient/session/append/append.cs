using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task<cAppendFeedback> AppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cProgress pProgress, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(AppendAsync), pMC, pMailboxHandle, pMessages);

                //throw new NotImplementedException();

                int lCount = 0;

                List<cAppendData> lMessages = new List<cAppendData>();

                var lSelectedMailboxHandle = mMailboxCache.SelectedMailboxDetails.MailboxHandle;

                foreach (var lMessage in pMessages)
                {
                    switch (lMessage)
                    {
                        case cMessageAppendData lWholeMessage:

                            cStorableFlags lFlags;
                            DateTime lReceived;

                            if (lWholeMessage.Flags == null) lFlags = lWholeMessage.Message.Flags;
                            else lFlags = lWholeMessage.Flags;

                            if (lWholeMessage.Received == null) lReceived = lWholeMessage.Message.Received;
                            else lReceived = lWholeMessage.Received.Value;

                            // this checks that the message is in THIS instances selected mailbox
                            //  what it should do is check that the message is in a client that is connected to the same account
                            if (_Capabilities.Catenate && lWholeMessage.Message.Client.ConnectedAccountId == mconnec)
                            {
                                // catenate using a URL



                                lMessages.Add(new cCatenateAppendData(lFlags, lReceived, new cURLAppendDataPart()));
                            }
                            else
                            {
                                cBodyFetchConfiguration lConfiguration = new cBodyFetchConfiguration(pMC, x); // x should be unlimited
                                cMessageStream lStream = new cMessageStream(lWholeMessage.Message, cSection.All, eDecodingRequired.none, lConfiguration, mAppendMaxMessageStreamBufferSize);
                                lMessages.Add(new cStreamAppendData(lStream, lWholeMessage.Message.Size, lFlags, lReceived));
                            }

                            lCount += lWholeMessage.Message.Size;
                            break;

                        case cMessagePartAppendData lMessagePart:

                            throw new cAppendDataNotSupportedException("message part");
                            break;

                        case cMailMessageAppendData lMailMessage:

                            throw new cAppendDataNotSupportedException("mail message");
                            break;

                        case cStringAppendData lString:

                            break;

                        case cStreamAppendData lStream:

                            break;

                        case cMultiPartAppendData lMultiPart:

                            throw new cAppendDataNotSupportedException("multi part");

                            foreach (var lPart in lMultiPart.Parts)
                            {
                                switch (lPart)
                                {
                                    case cMessageAppendDataPart lWholeMessage:

                                        break;

                                    case cMessagePartAppendDataPart lMessagePart:

                                        break;

                                    case cMailMessageAppendDataPart lMailMessage:

                                        break;

                                    case cStringAppendDataPart lString:

                                        break;

                                    case cStreamAppendDataPart lStream:

                                        break;
                                }
                            }

                        default:

                            throw new cInternalErrorException();
                    }
                }
            }
        }
    }
}