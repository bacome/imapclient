using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

                // the size of the append in bytes (for progress)
                int lByteCount = 0;

                // URLs must be generated relative to any currently selected mailbox, which means that the selected mailbox can't change until the URLs have all been sent
                //  (including from to to null)
                //
                iMailboxHandle lMailbox = mMailboxCache.SelectedMailboxDetails.MailboxHandle;
                int lURLCount = 0;

                List<cAppendData> lMessages = new List<cAppendData>();

                var lSelectedMailboxHandle = mMailboxCache.SelectedMailboxDetails.MailboxHandle;

                foreach (var lMessage in pMessages)
                {
                    switch (lMessage)
                    {
                        case cMessageAppendData lWholeMessage:

                            {
                                cStorableFlags lFlags;
                                DateTime lReceived;

                                if (lWholeMessage.Flags == null) lFlags = new cStorableFlags(lWholeMessage.Message.Flags.Except(cFetchableFlags.Recent, StringComparer.InvariantCultureIgnoreCase));
                                else lFlags = lWholeMessage.Flags;

                                if (lWholeMessage.Received == null) lReceived = lWholeMessage.Message.Received;
                                else lReceived = lWholeMessage.Received.Value;

                                if (_Capabilities.Catenate && lWholeMessage.AllowCatenate && lWholeMessage.Message.Client.ConnectedAccountId == _ConnectedAccountId)
                                {
                                    // catenate using a URL
                                    cURLAppendDataPart lURL = new cURLAppendDataPart();


                                    lMessages.Add(new cCatenateAppendData(lFlags, lReceived, new cURLAppendDataPart()));
                                }
                                else
                                {
                                    ;?; // MAX BUFFER SIze
                                    cMessageStream lStream = new cMessageStream(lWholeMessage.Message, cSection.All, eDecodingRequired.none, );
                                    lMessages.Add(new cStreamAppendData(lStream, lWholeMessage.Message.Size, lFlags, lReceived));
                                }

                                lCount += lWholeMessage.Message.Size;
                                break;
                            }

                        case cMessagePartAppendData lMessagePart:

                            {
                                DateTime lReceived;

                                if (lMessagePart.Received == null) lReceived = lMessagePart.Message.Received;
                                else lReceived = lMessagePart.Received.Value;




                                throw new cAppendDataNotSupportedException("message part");
                                break;
                            }

                        case cMailMessageAppendData lMailMessage:

                            throw new cAppendDataNotSupportedException("mail message");
                            break;

                        case cStringAppendData lString:

                            break;

                        case cFileAppendData lFile:

                            break;

                        case cUIDAppendData lUID:

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

                                    case cUIDAppendDataPart lUID:

                                        break;

                                    case cFileAppendDataPart lFile:

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