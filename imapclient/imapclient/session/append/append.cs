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

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);



                // throw new NotImplementedException(); /*


                // URLs must be generated relative to any currently selected mailbox, which means that the selected mailbox can't change until the URLs have all been sent
                //
                var lSelectedMailboxHandle = mMailboxCache.SelectedMailboxDetails?.MailboxHandle;
                int lURLCount = 0;

                // the size of the append in bytes (for progress)
                int lByteCount = 0;

                List<cAppendData> lMessages = new List<cAppendData>();

                foreach (var lMessage in pMessages)
                {
                    switch (lMessage)
                    {
                        case cMessageAppendData lWholeMessage:

                            {
                                bool lCatenate = _Capabilities.Catenate && lWholeMessage.AllowCatenate && lWholeMessage.Client.ConnectedAccountId == _ConnectedAccountId;

                                fMessageCacheAttributes lAttributes = 0;
                                if (lWholeMessage.Flags == null) lAttributes |= fMessageCacheAttributes.flags;
                                if (lWholeMessage.Received == null) lAttributes |= fMessageCacheAttributes.received;
                                if (lCatenate) lAttributes |= fMessageCacheAttributes.uid;
                                else lAttributes |= fMessageCacheAttributes.size;

                                if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, lAttributes))
                                {
                                    if (lWholeMessage.MessageHandle.Expunged) throw new cMessageExpungedException(lWholeMessage.MessageHandle);
                                    else throw new cRequestedDataNotReturnedException(lContext);
                                }

                                cStorableFlags lFlags;
                                DateTime? lReceived;

                                if (lWholeMessage.Flags == null) lFlags = new cStorableFlags(lWholeMessage.MessageHandle.Flags.Except(cFetchableFlags.Recent, StringComparer.InvariantCultureIgnoreCase));
                                else lFlags = lWholeMessage.Flags;

                                if (lWholeMessage.Received == null) lReceived = lWholeMessage.MessageHandle.Received;
                                else lReceived = lWholeMessage.Received;

                                if (lCatenate)
                                {
                                    cURLAppendDataPart lURL = new cURLAppendDataPart(lSelectedMailboxHandle, lWholeMessage.MessageHandle.MessageCache.MailboxHandle, lWholeMessage.MessageHandle.UID));
                                    lMessages.Add(new cCatenateAppendData(lFlags, lReceived, lURL));
                                    lURLCount++;
                                }
                                else
                                {
                                    lMessages.Add(new cMessageAppendData(lWholeMessage.me));
                                    lByteCount += (int)lWholeMessage.MessageHandle.Size.Value;
                                }

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

                        case cMessageSectionAppendData lSection:

                            {
                                ;?;

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

                                    case cMessageSectionAppendDataPart lSection:

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

                                    default:

                                        throw new cInternalErrorException();
                                }
                            }

                        default:

                            throw new cInternalErrorException();
                    }
                }

                // NOTE that if the lURLCount == 0 then we don't need to check that the currently selected mailbox stays the same throughout the append
                //  AND we should never suggest ignoring catentate on failure ...
                if (lURLCount == 0) 


                // */
            }
        }
    }
}