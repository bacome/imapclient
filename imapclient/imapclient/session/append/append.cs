using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            public async Task<cAppendFeedback> AppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cAppendDataList pMessages, Action<long> pSetMaximum, Action<int> pIncrement, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(AppendAsync), pMC, pMailboxHandle, pMessages);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

                mMailboxCache.CheckHandle(pMailboxHandle);

                if (pMessages.Count == 0) throw new ArgumentOutOfRangeException(nameof(pMessages));

                // convert the messages to a form that we can use
                cSessionAppendDataList lMessages = await ZAppendGetDataAsync(pMessages).ConfigureAwait(false);

                // sanity check
                if (lMessages.Count != pMessages.Count) throw new cInternalErrorException();

                // initialise any progress system that might be in place
                if (pSetMaximum != null)
                {
                    long lLength = 0;
                    foreach (var lMessage in lMessages) lLength += lMessage.Length;
                    mSynchroniser.InvokeActionLong(pSetMaximum, lLength, lContext);
                }

                // result collector
                List<cAppendResult> lResults = new List<cAppendResult>();

                // append messages in batches
                //  NOTE that the append stops on the first error => the results collected may not cover all the messages
                //
                await ZAppendInBatchesAsync(pMC, pMailboxHandle, lMessages, pIncrement, lResults, lContext).ConfigureAwait(false);

                // convert the collected results to feedback

                var lFeedbackItems = new List<cAppendFeedbackItem>();

                foreach (var lResult in lResults)
                {
                    switch (lResult)
                    {
                        case cAppendSucceeded lSucceeded:

                            for (int i = 0; i < lSucceeded.Count; i++) lFeedbackItems.Add(new cAppendFeedbackItem(true));
                            break;

                        case cAppendSucceededWithUIDs lUIDs:

                            foreach (var lUID in lUIDs.UIDs) lFeedbackItems.Add(new cAppendFeedbackItem(new cUID(lUIDs.UIDValidity, lUID)));
                            break;

                        case cAppendFailedWithResult lFailedResult:

                            for (int i = 0; i < lFailedResult.Count; i++) lFeedbackItems.Add(new cAppendFeedbackItem(lFailedResult.Result, lFailedResult.TryIgnoring));
                            break;

                        case cAppendFailedWithException lFailedException:

                            for (int i = 0; i < lFailedException.Count; i++) lFeedbackItems.Add(new cAppendFeedbackItem(lFailedException.Exception));
                            break;

                        case cAppendCancelled lCancelled:

                            for (int i = 0; i < lCancelled.Count; i++) lFeedbackItems.Add(new cAppendFeedbackItem(false));
                            break;

                        default:

                            throw new cInternalErrorException();
                    }
                }

                // sanity check
                if (lFeedbackItems.Count > pMessages.Count) throw new cInternalErrorException();

                // add feedback for any appends we didn't attempt
                for (int i = lFeedbackItems.Count; i < pMessages.Count; i++) lFeedbackItems.Add(new cAppendFeedbackItem(false));

                // done
                return new cAppendFeedback(lFeedbackItems);
            }

            private async Task<cSessionAppendDataList> ZAppendGetDataAsync(cAppendDataList pMessages)
            {
                cSessionAppendDataList lMessages = new cSessionAppendDataList();

                foreach (var lMessage in pMessages)
                {
                    switch (lMessage)
                    {
                        case cMessageAppendData lWholeMessage:

                            {
                                bool lCatenate = _Capabilities.Catenate && lWholeMessage.Client.ConnectedAccountId == _ConnectedAccountId;

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

                                if (lWholeMessage.Received == null) lReceived = lWholeMessage.MessageHandle.ReceivedDateTime;
                                else lReceived = lWholeMessage.Received;

                                if (lCatenate) lMessages.Add(new cCatenateAppendData(lFlags, lReceived, new cCatenateURLAppendDataPart[] { lURLPart }));
                                else lMessages.Add(new cSessionMessageAppendData(lFlags, lReceived, lWholeMessage.Client, lWholeMessage.MessageHandle, cSection.All, lWholeMessage.MessageHandle.Size.Value));

                                break;
                            }

                        case cMessagePartAppendData lMessagePart:

                            {
                                bool lCatenate = _Capabilities.Catenate && lMessagePart.Client.ConnectedAccountId == _ConnectedAccountId;

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

                                if (lMessagePart.Received == null) lReceived = lMessagePart.MessageHandle.ReceivedDateTime;
                                else lReceived = lMessagePart.Received.Value;

                                if (lCatenate) lMessages.Add(new cCatenateAppendData(lFlags, lReceived, new cCatenateURLAppendDataPart[] { lURLPart }));
                                else lMessages.Add(new cSessionMessageAppendData(lFlags, lReceived, lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part.Section, lMessagePart.Part.SizeInBytes));

                                break;
                            }

                        case cLiteralAppendData lLiteral:

                            lMessages.Add(new cSessionLiteralAppendData(lLiteral.Flags ?? mAppendDefaultFlags, lLiteral.Received, lLiteral.Bytes));
                            break;

                        case cFileAppendData lFile:

                            lMessages.Add(new cSessionFileAppendData(lFile.Flags ?? mAppendDefaultFlags, lFile.Received, lFile.Path, lFile.Length, lFile.ReadConfiguration ?? mAppendStreamReadConfiguration));
                            break;

                        case cStreamAppendData lStream:

                            if (lStream.Stream.CanSeek) lStream.Stream.Position = 0;
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
                                                    bool lCatenate = lWholeMessage.Client.ConnectedAccountId == _ConnectedAccountId;

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
                                                    else lParts.Add(new cCatenateMessageAppendDataPart(lWholeMessage.Client, lWholeMessage.MessageHandle, cSection.All, lWholeMessage.MessageHandle.Size.Value));

                                                    break;
                                                }

                                            case cMessagePartAppendDataPart lMessagePart:

                                                {
                                                    bool lCatenate = lMessagePart.Client.ConnectedAccountId == _ConnectedAccountId;

                                                    if (lCatenate && !await lMessagePart.Client.FetchAsync(lMessagePart.MessageHandle, fMessageCacheAttributes.uid))
                                                    {
                                                        if (lMessagePart.MessageHandle.Expunged) throw new cMessageExpungedException(lMessagePart.MessageHandle);
                                                        else throw new cRequestedDataNotReturnedException(lMessagePart.MessageHandle);
                                                    }

                                                    if (lCatenate && cCatenateURLAppendDataPart.TryConstruct(lMessagePart.MessageHandle, lMessagePart.Part.Section, mCommandPartFactory, out var lURLPart)) lParts.Add(lURLPart);
                                                    else lParts.Add(new cCatenateMessageAppendDataPart(lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part.Section, lMessagePart.Part.SizeInBytes));

                                                    break;
                                                }

                                            case cUIDSectionAppendDataPart lSection:

                                                {
                                                    if (lSection.Client.ConnectedAccountId == _ConnectedAccountId && cCatenateURLAppendDataPart.TryConstruct(lSection.MailboxHandle, lSection.UID, lSection.Section, mCommandPartFactory, out var lURLPart)) lParts.Add(lURLPart);
                                                    else lParts.Add(new cCatenateMessageAppendDataPart(lSection.Client, lSection.MailboxHandle, lSection.UID, lSection.Section, lSection.Length));
                                                    break;
                                                }

                                            case cFileAppendDataPart lFile:

                                                lParts.Add(new cCatenateFileAppendDataPart(lFile.Path, lFile.Length, lFile.Base64Encode, lFile.ReadConfiguration ?? mAppendStreamReadConfiguration));
                                                break;

                                            case cStreamAppendDataPart lStreamPart:

                                                {
                                                    cBatchSizerConfiguration lConfiguration = lStreamPart.ReadConfiguration ?? mAppendStreamReadConfiguration;

                                                    if (lStreamPart.Stream.CanSeek) lStreamPart.Stream.Position = 0;

                                                    Stream lStream;
                                                    if (lStreamPart.Base64Encode) lStream = new cBase64Encoder(lStreamPart.Stream, lConfiguration);
                                                    else lStream = lStreamPart.Stream;

                                                    lParts.Add(new cCatenateStreamAppendDataPart(lStream, lStreamPart.Length, lConfiguration));
                                                    break;
                                                }

                                            case cLiteralAppendDataPartBase lLiteral:

                                                Encoding lEncoding;
                                                if ((EnabledExtensions & fEnableableExtensions.utf8) == 0) lEncoding = lMultiPart.Encoding ?? mEncoding;
                                                else lEncoding = null;

                                                lParts.Add(new cCatenateLiteralAppendDataPart(lLiteral.GetBytes(lEncoding)));
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
                                    long lLength = 0;

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

                                                lParts.Add(new cSessionMessageAppendDataPart(lWholeMessage.Client, lWholeMessage.MessageHandle, cSection.All, lWholeMessage.MessageHandle.Size.Value));
                                                lLength += lWholeMessage.MessageHandle.Size.Value;

                                                break;

                                            case cMessagePartAppendDataPart lMessagePart:

                                                lParts.Add(new cSessionMessageAppendDataPart(lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part.Section, lMessagePart.Part.SizeInBytes));
                                                lLength += lMessagePart.Part.SizeInBytes;
                                                break;

                                            case cUIDSectionAppendDataPart lSection:

                                                lParts.Add(new cSessionMessageAppendDataPart(lSection.Client, lSection.MailboxHandle, lSection.UID, lSection.Section, lSection.Length));
                                                lLength += lSection.Length;
                                                break;

                                            case cFileAppendDataPart lFile:

                                                lParts.Add(new cSessionFileAppendDataPart(lFile.Path, lFile.Length, lFile.Base64Encode, lFile.ReadConfiguration ?? mAppendStreamReadConfiguration));
                                                lLength += lFile.Length;
                                                break;

                                            case cStreamAppendDataPart lStreamPart:

                                                {
                                                    cBatchSizerConfiguration lConfiguration = lStreamPart.ReadConfiguration ?? mAppendStreamReadConfiguration;

                                                    if (lStreamPart.Stream.CanSeek) lStreamPart.Stream.Position = 0;

                                                    Stream lStream;
                                                    if (lStreamPart.Base64Encode) lStream = new cBase64Encoder(lStreamPart.Stream, lConfiguration);
                                                    else lStream = lStreamPart.Stream;

                                                    lParts.Add(new cSessionStreamAppendDataPart(lStream, lStreamPart.Length, lConfiguration));
                                                    lLength += lStreamPart.Length;
                                                    break;
                                                }

                                            case cLiteralAppendDataPartBase lLiteral:

                                                Encoding lEncoding;
                                                if ((EnabledExtensions & fEnableableExtensions.utf8) == 0) lEncoding = lMultiPart.Encoding ?? mEncoding;
                                                else lEncoding = null;

                                                lParts.Add(new cSessionLiteralAppendDataPart(lLiteral.GetBytes(lEncoding)));
                                                break;

                                            default:

                                                throw new cInternalErrorException();
                                        }
                                    }

                                    if (lLength > uint.MaxValue) throw new cAppendDataSizeException(lMultiPart);

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

            private async Task ZAppendInBatchesAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cSessionAppendDataList pMessages, Action<int> pIncrement, List<cAppendResult> pResults, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZAppendInBatchesAsync), pMC, pMailboxHandle, pMessages);

                long lTargetLength = mAppendBatchSizer.Current;
                cSessionAppendDataList lMessages = new cSessionAppendDataList();
                long lTotalLength = 0;

                foreach (var lMessage in pMessages)
                {
                    lMessages.Add(lMessage);
                    lTotalLength += lMessage.Length;

                    if (lTotalLength >= lTargetLength)
                    {
                        if (!await ZAppendBatchAsync(pMC, pMailboxHandle, lMessages, lTotalLength, pIncrement, pResults, lContext).ConfigureAwait(false)) return;

                        // start a new batch
                        lTargetLength = mAppendBatchSizer.Current;
                        lMessages.Clear();
                        lTotalLength = 0;
                    }
                }

                if (lMessages.Count > 0) await ZAppendBatchAsync(pMC, pMailboxHandle, lMessages, lTotalLength, pIncrement, pResults, lContext).ConfigureAwait(false);
            }

            private async Task<bool> ZAppendBatchAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cSessionAppendDataList pMessages, long pTotalLength, Action<int> pIncrement, List<cAppendResult> pResults, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZAppendBatchAsync), pMC, pMailboxHandle, pMessages, pTotalLength);

                bool lAllAppendedOK;

                Stopwatch lStopwatch = Stopwatch.StartNew();

                if (_Capabilities.MultiAppend)
                {
                    try
                    {
                        var lResult = await ZAppendAsync(pMC, pMailboxHandle, pMessages, pIncrement, lContext).ConfigureAwait(false);
                        pResults.Add(lResult);
                        if (lResult is cAppendFailedWithResult) lAllAppendedOK = false;
                        else lAllAppendedOK = true;
                    }
                    catch (OperationCanceledException)
                    {
                        pResults.Add(new cAppendCancelled(pMessages.Count));
                        lAllAppendedOK = false;
                    }
                    catch (AggregateException e)
                    {
                        pResults.Add(new cAppendFailedWithException(pMessages.Count, cTools.Flatten(e)));
                        lAllAppendedOK = false;
                    }
                    catch (Exception e)
                    {
                        pResults.Add(new cAppendFailedWithException(pMessages.Count, e));
                        lAllAppendedOK = false;
                    }
                }
                else
                {
                    List<Task<cAppendResult>> lTasks = new List<Task<cAppendResult>>();

                    foreach (var lMessage in pMessages)
                    {
                        cSessionAppendDataList lMessages = new cSessionAppendDataList();
                        lMessages.Add(lMessage);
                        lTasks.Add(ZAppendAsync(pMC, pMailboxHandle, lMessages, pIncrement, lContext));
                    }

                    try
                    {
                        await Task.WhenAll(lTasks).ConfigureAwait(false);
                        lAllAppendedOK = true; // might be all ok; have to inspect the individual results in the loop below
                    }
                    catch (Exception)
                    {
                        // at least one task faulted or was cancelled
                        lAllAppendedOK = false;
                    }

                    foreach (var lTask in lTasks)
                    {
                        if (lTask.IsFaulted) pResults.Add(new cAppendFailedWithException(cTools.Flatten(lTask.Exception)));
                        else if (lTask.IsCanceled) pResults.Add(cAppendResult.Cancelled);
                        else
                        {
                            var lResult = lTask.Result;
                            pResults.Add(lResult);
                            if (lResult is cAppendFailedWithResult) lAllAppendedOK = false;
                        }
                    }
                }

                lStopwatch.Stop();

                // store the time taken so the next append is a better size
                mAppendBatchSizer.AddSample(pTotalLength, lStopwatch.ElapsedMilliseconds);

                // done
                return lAllAppendedOK;
            }
        }
    }
}