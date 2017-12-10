using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

                List<cSessionAppendData> lMessages = new List<cSessionAppendData>();

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
                                    else throw new cRequestedDataNotReturnedException(lWholeMessage.MessageHandle);
                                }

                                cStorableFlags lFlags;
                                DateTime? lReceived;

                                if (lWholeMessage.Flags == null) lFlags = new cStorableFlags(lWholeMessage.MessageHandle.Flags.Except(cFetchableFlags.Recent, StringComparer.InvariantCultureIgnoreCase));
                                else lFlags = lWholeMessage.Flags;

                                if (lWholeMessage.Received == null) lReceived = lWholeMessage.MessageHandle.Received;
                                else lReceived = lWholeMessage.Received;

                                if (lCatenate)
                                {
                                    cSessionCatenateAppendDataPart[] lParts = new cSessionCatenateAppendDataPart[1];
                                    lParts[0] = new cSessionCatenateURLAppendDataPart(lWholeMessage.MessageHandle, cSection.All);
                                    lMessages.Add(new cSessionCatenateAppendData(lFlags, lReceived, lParts));
                                }
                                else lMessages.Add(new cSessionMessageAppendData(lFlags, lReceived, lWholeMessage.Client, lWholeMessage.MessageHandle, null));

                                break;
                            }

                        case cMessagePartAppendData lMessagePart:

                            {
                                bool lCatenate = _Capabilities.Catenate && lMessagePart.AllowCatenate && lMessagePart.Client.ConnectedAccountId == _ConnectedAccountId;

                                fMessageCacheAttributes lAttributes = 0;
                                if (lMessagePart.Received == null) lAttributes |= fMessageCacheAttributes.received;
                                if (lCatenate) lAttributes |= fMessageCacheAttributes.uid;

                                if (!await lMessagePart.Client.FetchAsync(lMessagePart.MessageHandle, lAttributes))
                                {
                                    if (lMessagePart.MessageHandle.Expunged) throw new cMessageExpungedException(lMessagePart.MessageHandle);
                                    else throw new cRequestedDataNotReturnedException(lMessagePart.MessageHandle);
                                }

                                cStorableFlags lFlags;
                                DateTime? lReceived;

                                if (lMessagePart.Flags == null) lFlags = mAppendDefaultFlags;
                                else lFlags = lMessagePart.Flags;

                                if (lMessagePart.Received == null) lReceived = lMessagePart.MessageHandle.Received;
                                else lReceived = lMessagePart.Received.Value;

                                if (lCatenate)
                                {
                                    cSessionCatenateAppendDataPart[] lParts = new cSessionCatenateAppendDataPart[1];
                                    lParts[0] = new cSessionCatenateURLAppendDataPart(lMessagePart.MessageHandle, lMessagePart.Part.Section);
                                    lMessages.Add(new cSessionCatenateAppendData(lFlags, lReceived, lParts));
                                }
                                else lMessages.Add(new cSessionMessageAppendData(lFlags, lReceived, lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part));

                                break;
                            }

                        case cMailMessageAppendData lMailMessage:

                            {
                                cStorableFlags lFlags;

                                if (lMailMessage.Flags == null) lFlags = mAppendDefaultFlags;
                                else lFlags = lMailMessage.Flags;

                                // TODO ...

                                throw new cMailMessageException();
                            }

                        case cStringAppendData lString:

                            lMessages.Add(new cSessionBytesAppendData(lString, mAppendDefaultFlags));
                            break;

                        case cFileAppendData lFile:

                            lMessages.Add(new cSessionFileAppendData(lFile, mAppendDefaultFlags, mAppendStreamReadConfiguration));
                            break;

                        case cStreamAppendData lStream:

                            lMessages.Add(new cSessionStreamAppendData(lStream, mAppendDefaultFlags, mAppendStreamReadConfiguration));
                            break;

                        case cMultiPartAppendData lMultiPart:

                            {
                                cStorableFlags lFlags;

                                if (lMultiPart.Flags == null) lFlags = mAppendDefaultFlags;
                                else lFlags = lMultiPart.Flags;

                                if (_Capabilities.Catenate)
                                {
                                    List<cSessionCatenateAppendDataPart> lParts = new List<cSessionCatenateAppendDataPart>();

                                    foreach (var lPart in lMultiPart.Parts)
                                    {
                                        switch (lPart)
                                        {
                                            case cMessageAppendDataPart lWholeMessage:

                                                {
                                                    bool lCatenate = lWholeMessage.AllowCatenate && lWholeMessage.Client.ConnectedAccountId == _ConnectedAccountId;

                                                    fMessageCacheAttributes lAttributes = 0;
                                                    if (lCatenate) lAttributes |= fMessageCacheAttributes.uid;
                                                    else lAttributes |= fMessageCacheAttributes.size;

                                                    if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, lAttributes))
                                                    {
                                                        if (lWholeMessage.MessageHandle.Expunged) throw new cMessageExpungedException(lWholeMessage.MessageHandle);
                                                        else throw new cRequestedDataNotReturnedException(lWholeMessage.MessageHandle);
                                                    }

                                                    if (lCatenate) lParts.Add(new cSessionCatenateURLAppendDataPart(lWholeMessage.MessageHandle, cSection.All));
                                                    else lParts.Add(new cSessionCatenateMessageAppendDataPart(lWholeMessage.Client, lWholeMessage.MessageHandle, null));

                                                    break;
                                                }

                                            case cMessagePartAppendDataPart lMessagePart:

                                                {
                                                    bool lCatenate = lMessagePart.AllowCatenate && lMessagePart.Client.ConnectedAccountId == _ConnectedAccountId;

                                                    if (lCatenate && !await lMessagePart.Client.FetchAsync(lMessagePart.MessageHandle, fMessageCacheAttributes.uid))
                                                    {
                                                        if (lMessagePart.MessageHandle.Expunged) throw new cMessageExpungedException(lMessagePart.MessageHandle);
                                                        else throw new cRequestedDataNotReturnedException(lMessagePart.MessageHandle);
                                                    }

                                                    if (lCatenate) lParts.Add(new cSessionCatenateURLAppendDataPart(lMessagePart.MessageHandle, lMessagePart.Part.Section));
                                                    else lParts.Add(new cSessionCatenateMessageAppendDataPart(lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part));

                                                    break;
                                                }

                                            case cUIDSectionAppendDataPart lSection:

                                                ;?;
                                                break;

                                            case cMailMessageAppendDataPart lMailMessage:

                                                throw new cMailMessageException();

                                            case cStringAppendDataPart lString:

                                                ;?;
                                                break;

                                            case cFileAppendDataPart lFile:

                                                ;?;
                                                break;

                                            case cStreamAppendDataPart lStream:

                                                ;?;
                                                break;

                                            default:

                                                throw new cInternalErrorException();
                                        }
                                    }

                                    lMessages.Add(new cSessionCatenateAppendData(lFlags, lMultiPart.Received, lParts));
                                }
                                else
                                {
                                    List<cSessionMultiPartAppendDataPart> lParts = new List<cSessionMultiPartAppendDataPart>();

                                    foreach (var lPart in lMultiPart.Parts)
                                    {
                                        switch (lPart)
                                        {
                                            case cMessageAppendDataPart lWholeMessage:

                                                if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, fMessageCacheAttributes.size))
                                                {
                                                    if (lWholeMessage.MessageHandle.Expunged) throw new cMessageExpungedException(lWholeMessage.MessageHandle);
                                                    else throw new cRequestedDataNotReturnedException(lWholeMessage.MessageHandle);
                                                }

                                                lParts.Add(new cSessionMultiPartMessageAppendDataPart(lWholeMessage.Client, lWholeMessage.MessageHandle, null));

                                                break;

                                            case cMessagePartAppendDataPart lMessagePart:

                                                ;?;
                                                break;

                                            case cUIDSectionAppendDataPart lSection:

                                                ;?;
                                                break;

                                            case cMailMessageAppendDataPart lMailMessage:

                                                ;?;
                                                break;

                                            case cStringAppendDataPart lString:

                                                ;?;
                                                break;

                                            case cFileAppendDataPart lFile:

                                                ;?;
                                                break;

                                            case cStreamAppendDataPart lStream:

                                                ;?;
                                                break;

                                            default:

                                                throw new cInternalErrorException();
                                        }
                                    }

                                    lMessages.Add(new cSessionMultiPartAppendData(lFlags, lMultiPart.Received, lParts));
                                }

                                break;
                            }

                        default:

                            throw new cInternalErrorException();
                    }
                }
            }
        }
    }
}