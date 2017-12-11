using System;
using System.Collections.Generic;
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

                // convert the messages to a form that we can use
                List<cSessionAppendData> lMessages = await ZAppendMessages(pMessages).ConfigureAwait(false);

                // progress
                int lCount = 0;
                foreach (var lMessage in lMessages) lCount += lMessage.Length;
                pProgress.SetCount(lCount, lContext);

                // append in batches
                foreach (var lMessage in lMessages)
                {

                }
            }

            private async Task<List<cSessionAppendData>> ZAppendMessages(cAppendDataList pMessages)
            {
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

                                cCatenateURLAppendDataPart lURLPart = null;

                                if (lCatenate && !cCatenateURLAppendDataPart.TryConstruct(lWholeMessage.MessageHandle, cSection.All, mCommandPartFactory, out lURLPart))
                                {
                                    lCatenate = false;

                                    if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, fMessageCacheAttributes.size))
                                    {
                                        if (lWholeMessage.MessageHandle.Expunged) throw new cMessageExpungedException(lWholeMessage.MessageHandle);
                                        else throw new cRequestedDataNotReturnedException(lWholeMessage.MessageHandle);
                                    }
                                }

                                cStorableFlags lFlags;
                                DateTime? lReceived;

                                if (lWholeMessage.Flags == null) lFlags = new cStorableFlags(lWholeMessage.MessageHandle.Flags.Except(cFetchableFlags.Recent, StringComparer.InvariantCultureIgnoreCase));
                                else lFlags = lWholeMessage.Flags;

                                if (lWholeMessage.Received == null) lReceived = lWholeMessage.MessageHandle.Received;
                                else lReceived = lWholeMessage.Received;

                                if (lCatenate) lMessages.Add(new cCatenateAppendData(lFlags, lReceived, new cCatenateURLAppendDataPart[] { lURLPart }));
                                else lMessages.Add(new cSessionMessageAppendData(lFlags, lReceived, lWholeMessage.Client, lWholeMessage.MessageHandle, cSection.All, (int)lWholeMessage.MessageHandle.Size));

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

                                cCatenateURLAppendDataPart lURLPart = null;

                                if (lCatenate && !cCatenateURLAppendDataPart.TryConstruct(lMessagePart.MessageHandle, lMessagePart.Part.Section, mCommandPartFactory, out lURLPart)) lCatenate = false;

                                cStorableFlags lFlags;
                                DateTime? lReceived;

                                if (lMessagePart.Flags == null) lFlags = mAppendDefaultFlags;
                                else lFlags = lMessagePart.Flags;

                                if (lMessagePart.Received == null) lReceived = lMessagePart.MessageHandle.Received;
                                else lReceived = lMessagePart.Received.Value;

                                if (lCatenate) lMessages.Add(new cCatenateAppendData(lFlags, lReceived, new cCatenateURLAppendDataPart[] { lURLPart }));
                                else lMessages.Add(new cSessionMessageAppendData(lFlags, lReceived, lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part.Section, (int)lMessagePart.Part.SizeInBytes));

                                break;
                            }

                        case cStringAppendData lString:

                            lMessages.Add(new cSessionBytesAppendData(lString.Flags ?? mAppendDefaultFlags, lString.Received, lString.String));
                            break;

                        case cFileAppendData lFile:

                            lMessages.Add(new cSessionFileAppendData(lFile.Flags ?? mAppendDefaultFlags, lFile.Received, lFile.Path, lFile.Length, lFile.ReadConfiguration ?? mAppendStreamReadConfiguration));
                            break;

                        case cStreamAppendData lStream:

                            lMessages.Add(new cSessionStreamAppendData(lStream.Flags ?? mAppendDefaultFlags, lStream.Received, lStream.Stream, lStream.Length, lStream.ReadConfiguration ?? mAppendStreamReadConfiguration));
                            break;

                        case cMultiPartAppendData lMultiPart:

                            {
                                cStorableFlags lFlags;

                                if (lMultiPart.Flags == null) lFlags = mAppendDefaultFlags;
                                else lFlags = lMultiPart.Flags;

                                if (_Capabilities.Catenate)
                                {
                                    List<cCatenateAppendDataPart> lParts = new List<cCatenateAppendDataPart>();

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

                                                    cCatenateURLAppendDataPart lURLPart = null;

                                                    if (lCatenate && !cCatenateURLAppendDataPart.TryConstruct(lWholeMessage.MessageHandle, cSection.All, mCommandPartFactory, out lURLPart))
                                                    {
                                                        lCatenate = false;

                                                        if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, fMessageCacheAttributes.size))
                                                        {
                                                            if (lWholeMessage.MessageHandle.Expunged) throw new cMessageExpungedException(lWholeMessage.MessageHandle);
                                                            else throw new cRequestedDataNotReturnedException(lWholeMessage.MessageHandle);
                                                        }
                                                    }

                                                    if (lCatenate) lParts.Add(lURLPart);
                                                    else lParts.Add(new cCatenateMessageAppendDataPart(lWholeMessage.Client, lWholeMessage.MessageHandle, cSection.All, (int)lWholeMessage.MessageHandle.Size));

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

                                                    if (lCatenate && cCatenateURLAppendDataPart.TryConstruct(lMessagePart.MessageHandle, lMessagePart.Part.Section, mCommandPartFactory, out var lURLPart)) lParts.Add(lURLPart);
                                                    else lParts.Add(new cCatenateMessageAppendDataPart(lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part.Section, (int)lMessagePart.Part.SizeInBytes));

                                                    break;
                                                }

                                            case cUIDSectionAppendDataPart lSection:

                                                {
                                                    if (lSection.AllowCatenate && lSection.Client.ConnectedAccountId == _ConnectedAccountId && cCatenateURLAppendDataPart.TryConstruct(lSection.MailboxHandle, lSection.UID, lSection.Section, mCommandPartFactory, out var lURLPart)) lParts.Add(lURLPart);
                                                    else lParts.Add(new cCatenateMessageAppendDataPart(lSection.Client, lSection.MailboxHandle, lSection.UID, lSection.Section, lSection.Length));
                                                    break;
                                                }

                                            case cStringAppendDataPart lString:

                                                lParts.Add(new cCatenateBytesAppendDataPart(lString.String));
                                                break;

                                            case cFileAppendDataPart lFile:

                                                lParts.Add(new cCatenateFileAppendDataPart(lFile.Path, lFile.Length, lFile.ReadConfiguration ?? mAppendStreamReadConfiguration));
                                                break;

                                            case cStreamAppendDataPart lStream:

                                                lParts.Add(new cCatenateStreamAppendDataPart(lStream.Stream, lStream.Length, lStream.ReadConfiguration ?? mAppendStreamReadConfiguration));
                                                break;

                                            default:

                                                throw new cInternalErrorException();
                                        }
                                    }

                                    lMessages.Add(new cCatenateAppendData(lFlags, lMultiPart.Received, lParts));
                                }
                                else
                                {
                                    List<cSessionAppendDataPart> lParts = new List<cSessionAppendDataPart>();

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

                                                lParts.Add(new cSessionMessageAppendDataPart(lWholeMessage.Client, lWholeMessage.MessageHandle, cSection.All, (int)lWholeMessage.MessageHandle.Size));

                                                break;

                                            case cMessagePartAppendDataPart lMessagePart:

                                                lParts.Add(new cSessionMessageAppendDataPart(lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part.Section, (int)lMessagePart.Part.SizeInBytes));
                                                break;

                                            case cUIDSectionAppendDataPart lSection:

                                                lParts.Add(new cSessionMessageAppendDataPart(lSection.Client, lSection.MailboxHandle, lSection.UID, lSection.Section, lSection.Length));
                                                break;

                                            case cStringAppendDataPart lString:

                                                lParts.Add(new cSessionBytesAppendDataPart(lString.String));
                                                break;

                                            case cFileAppendDataPart lFile:

                                                lParts.Add(new cSessionFileAppendDataPart(lFile.Path, lFile.Length, lFile.ReadConfiguration ?? mAppendStreamReadConfiguration));
                                                break;

                                            case cStreamAppendDataPart lStream:

                                                lParts.Add(new cSessionStreamAppendDataPart(lStream.Stream, lStream.Length, lStream.ReadConfiguration ?? mAppendStreamReadConfiguration));
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

                return lMessages;
            }
        }
    }
}