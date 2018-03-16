﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

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
                if (_ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

                mMailboxCache.CheckHandle(pMailboxHandle);

                if (pMessages.Count == 0) throw new ArgumentOutOfRangeException(nameof(pMessages));

                // convert the messages to a form that we can use
                cSessionAppendDataList lMessages = await ZAppendGetDataAsync(pMessages, lContext).ConfigureAwait(false);

                // sanity check
                if (lMessages.Count != pMessages.Count) throw new cInternalErrorException("too many messages", lContext);

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

                        default:

                            throw new cInternalErrorException($"switch/default {lResult}", lContext);
                    }
                }

                // sanity check
                if (lFeedbackItems.Count > pMessages.Count) throw new cInternalErrorException("too much feedback", lContext);

                // add feedback for any appends we didn't attempt
                for (int i = lFeedbackItems.Count; i < pMessages.Count; i++) lFeedbackItems.Add(new cAppendFeedbackItem(false));

                // done
                return new cAppendFeedback(lFeedbackItems);
            }

            private async Task<cSessionAppendDataList> ZAppendGetDataAsync(cAppendDataList pMessages, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZAppendGetDataAsync), pMessages);

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

                                if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, lAttributes).ConfigureAwait(false))
                                {
                                    if (lWholeMessage.MessageHandle.Expunged) throw new cMessageExpungedException(lWholeMessage.MessageHandle);
                                    else throw new cRequestedDataNotReturnedException(lWholeMessage.MessageHandle);
                                }

                                cCatenateURLAppendDataPart lURLPart = null;

                                if (lCatenate && !cCatenateURLAppendDataPart.TryConstruct(lWholeMessage.MessageHandle, cSection.All, mCommandPartFactory, out lURLPart))
                                {
                                    lCatenate = false;

                                    if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, fMessageCacheAttributes.size).ConfigureAwait(false))
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

                                if (!await lMessagePart.Client.FetchAsync(lMessagePart.MessageHandle, lAttributes).ConfigureAwait(false))
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

                                List<cCatenateAppendDataPart> lCatenateParts = new List<cCatenateAppendDataPart>();
                                List<cSessionAppendDataPart> lSessionParts = new List<cSessionAppendDataPart>();

                                foreach (var lPart in lMultiPart.Parts)
                                {
                                    switch (lPart)
                                    {
                                        case cMessageAppendDataPart lWholeMessage:

                                            {
                                                bool lCatenate = _Capabilities.Catenate && lWholeMessage.Client.ConnectedAccountId == _ConnectedAccountId;

                                                fMessageCacheAttributes lAttributes = 0;
                                                if (lCatenate) lAttributes |= fMessageCacheAttributes.uid;
                                                else lAttributes |= fMessageCacheAttributes.size;

                                                if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, lAttributes).ConfigureAwait(false))
                                                {
                                                    if (lWholeMessage.MessageHandle.Expunged) throw new cMessageExpungedException(lWholeMessage.MessageHandle);
                                                    else throw new cRequestedDataNotReturnedException(lWholeMessage.MessageHandle);
                                                }

                                                cCatenateURLAppendDataPart lURLPart = null;

                                                if (lCatenate && !cCatenateURLAppendDataPart.TryConstruct(lWholeMessage.MessageHandle, cSection.All, mCommandPartFactory, out lURLPart))
                                                {
                                                    lCatenate = false;

                                                    if (!await lWholeMessage.Client.FetchAsync(lWholeMessage.MessageHandle, fMessageCacheAttributes.size).ConfigureAwait(false))
                                                    {
                                                        if (lWholeMessage.MessageHandle.Expunged) throw new cMessageExpungedException(lWholeMessage.MessageHandle);
                                                        else throw new cRequestedDataNotReturnedException(lWholeMessage.MessageHandle);
                                                    }
                                                }

                                                if (lCatenate)
                                                {
                                                    if (lSessionParts.Count > 0)
                                                    {
                                                        lCatenateParts.Add(new cCatenateLiteralAppendDataPart(lSessionParts));
                                                        lSessionParts.Clear();
                                                    }

                                                    lCatenateParts.Add(lURLPart);
                                                }
                                                else lSessionParts.Add(new cSessionMessageAppendDataPart(lWholeMessage.Client, lWholeMessage.MessageHandle, cSection.All, lWholeMessage.MessageHandle.Size.Value));

                                                break;
                                            }

                                        case cMessagePartAppendDataPart lMessagePart:

                                            {
                                                bool lCatenate = _Capabilities.Catenate && lMessagePart.Client.ConnectedAccountId == _ConnectedAccountId;

                                                if (lCatenate && !await lMessagePart.Client.FetchAsync(lMessagePart.MessageHandle, fMessageCacheAttributes.uid).ConfigureAwait(false))
                                                {
                                                    if (lMessagePart.MessageHandle.Expunged) throw new cMessageExpungedException(lMessagePart.MessageHandle);
                                                    else throw new cRequestedDataNotReturnedException(lMessagePart.MessageHandle);
                                                }

                                                if (lCatenate && cCatenateURLAppendDataPart.TryConstruct(lMessagePart.MessageHandle, lMessagePart.Part.Section, mCommandPartFactory, out var lURLPart))
                                                {
                                                    if (lSessionParts.Count > 0)
                                                    {
                                                        lCatenateParts.Add(new cCatenateLiteralAppendDataPart(lSessionParts));
                                                        lSessionParts.Clear();
                                                    }

                                                    lCatenateParts.Add(lURLPart);
                                                }
                                                else lSessionParts.Add(new cSessionMessageAppendDataPart(lMessagePart.Client, lMessagePart.MessageHandle, lMessagePart.Part.Section, lMessagePart.Part.SizeInBytes));

                                                break;
                                            }

                                        case cUIDSectionAppendDataPart lSection:

                                            {
                                                if (_Capabilities.Catenate && lSection.Client.ConnectedAccountId == _ConnectedAccountId && cCatenateURLAppendDataPart.TryConstruct(lSection.MailboxHandle, lSection.UID, lSection.Section, mCommandPartFactory, out var lURLPart))
                                                {
                                                    if (lSessionParts.Count > 0)
                                                    {
                                                        lCatenateParts.Add(new cCatenateLiteralAppendDataPart(lSessionParts));
                                                        lSessionParts.Clear();
                                                    }

                                                    lCatenateParts.Add(lURLPart);
                                                }
                                                else lSessionParts.Add(new cSessionMessageAppendDataPart(lSection.Client, lSection.MailboxHandle, lSection.UID, lSection.Section, lSection.Length));

                                                break;
                                            }

                                        case cFileAppendDataPart lFile:

                                            lSessionParts.Add(new cSessionFileAppendDataPart(lFile.Path, lFile.Length, lFile.Base64Encode, lFile.ReadConfiguration ?? mAppendStreamReadConfiguration));
                                            break;

                                        case cStreamAppendDataPart lStreamPart:

                                            {
                                                cBatchSizerConfiguration lConfiguration = lStreamPart.ReadConfiguration ?? mAppendStreamReadConfiguration;

                                                if (lStreamPart.Stream.CanSeek) lStreamPart.Stream.Position = 0;

                                                Stream lStream;
                                                if (lStreamPart.Base64Encode) lStream = new cBase64Encoder(lStreamPart.Stream, lConfiguration);
                                                else lStream = lStreamPart.Stream;

                                                lSessionParts.Add(new cSessionStreamAppendDataPart(lStream, lStreamPart.Length, lConfiguration));
                                                break;
                                            }

                                        case cLiteralAppendDataPartBase lLiteral:

                                            Encoding lEncoding;
                                            if ((EnabledExtensions & fEnableableExtensions.utf8) == 0) lEncoding = lMultiPart.Encoding ?? mEncoding;
                                            else lEncoding = null;

                                            lSessionParts.Add(new cSessionLiteralAppendDataPart(lLiteral.GetBytes(lEncoding)));
                                            break;

                                        default:

                                            throw new cInternalErrorException($"multipart switch/default {lPart}", lContext);
                                    }
                                }

                                if (lCatenateParts.Count > 0)
                                {
                                    if (lSessionParts.Count > 0) lCatenateParts.Add(new cCatenateLiteralAppendDataPart(lSessionParts));
                                    lMessages.Add(new cCatenateAppendData(lFlags, lMultiPart.Received, lCatenateParts));
                                }
                                else lMessages.Add(new cSessionMultiPartAppendData(lFlags, lMultiPart.Received, lSessionParts));

                                break;
                            }

                        default:

                            throw new cInternalErrorException($"message switch/default {lMessage}", lContext);
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
                        pResults.Add(new cAppendFailedWithException(pMessages.Count, new OperationCanceledException()));
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