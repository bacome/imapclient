using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cAppendFeedback Append(iMailboxHandle pMailboxHandle, cAppendDataList pData, cAppendConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Append), 1);
            var lTask = ZAppendAsync(pMailboxHandle, pData, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cAppendFeedback> AppendAsync(iMailboxHandle pMailboxHandle, cAppendDataList pData, cAppendConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(AppendAsync), 1);
            return ZAppendAsync(pMailboxHandle, pData, pConfiguration, lContext);
        }

        private async Task<cAppendFeedback> ZAppendAsync(iMailboxHandle pMailboxHandle, cAppendDataList pData, cAppendConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZAppendAsync), pMailboxHandle, pData, pConfiguration);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pData == null) throw new ArgumentNullException(nameof(pData));

            // special case
            if (pData.Count == 0) return new cAppendFeedback();

            // check input
            foreach (var lItem in pData)
            {
                if ((lItem.Format & lSession.SupportedFormats) != lItem.Format) throw new cAppendDataFormException(lItem);

                // verify that the appenddata will be readable during the append ...
                //  the problem is that anything that is a (or anything that might be converted to a) stream that needs to be served by this client will not work
                //   we will attempt to read from the stream while sending the data => there will be a deadlock 
                //    (the fetch will be queued on the command pipeline while the command pipeline is in the middle of sending the append
                //      the append will wait for the results of the fetch, but the fetch can't be sent until the append has finished)
                //
                // the way this should be done is by connecting a separate client to the same account and using a message instance from the separate client
                //  (this is the best/safest design as it will work with or without catenate)
                //
                // alternatively, read into a temporary file and then use the file as the source of the data (this will obviously never use catenate).
                //
                switch (lItem)
                {
                    case cMessageAppendData lMessage:

                        if (ReferenceEquals(lMessage.Client, this)) throw new cMessageDataClientException();
                        break;

                    case cMessagePartAppendData lMessagePart:

                        if (ReferenceEquals(lMessagePart.Client, this)) throw new cMessageDataClientException();
                        break;

                    case cStreamAppendData lStream:

                        {
                            // if the stream where wrapped in another stream this check wouldn't work
                            if (lStream.Stream is cIMAPMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this)) throw new cMessageDataClientException();
                            break;
                        }

                    case cMessageDataAppendData lMessageData:

                        foreach (var lPart in lMessageData.MessageData.Parts)
                        {
                            switch (lPart)
                            {
                                case cMessageMessageDataPart lMessage:

                                    if (ReferenceEquals(lMessage.Client, this)) throw new cMessageDataClientException();
                                    break;

                                case cMessagePartMessageDataPart lMessagePart:

                                    if (ReferenceEquals(lMessagePart.Client, this)) throw new cMessageDataClientException();
                                    break;

                                case cUIDSectionMessageDataPart lSection:

                                    if (ReferenceEquals(lSection.Client, this)) throw new cMessageDataClientException();
                                    break;

                                case cStreamMessageDataPart lStream:

                                    {
                                        // if the stream where wrapped in another stream this check wouldn't work
                                        if (lStream.Stream is cIMAPMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this)) throw new cMessageDataClientException();
                                        break;
                                    }

                                case cBase64StreamMessageDataPart lStream:

                                    {
                                        // if the stream where wrapped in another stream this check wouldn't work
                                        if (lStream.Stream is cIMAPMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this)) throw new cMessageDataClientException();
                                        break;
                                    }
                            }
                        }

                        break;
                }
            }

            // do the append

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    return await lSession.AppendAsync(lMC, pMailboxHandle, pData, null, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await lSession.AppendAsync(lMC, pMailboxHandle, pData, pConfiguration.SetMaximum, pConfiguration.Increment, lContext).ConfigureAwait(false);
            }
        }

        internal cAppendFeedback Append(iMailboxHandle pMailboxHandle, cMailMessageList pMailMessages, cStorableFlags pFlags, DateTime? pReceived, cAppendMailMessageConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Append), 2);
            var lTask = ZAppendAsync(pMailboxHandle, pMailMessages, pFlags, pReceived, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cAppendFeedback> AppendAsync(iMailboxHandle pMailboxHandle, cMailMessageList pMailMessages, cStorableFlags pFlags, DateTime? pReceived, cAppendMailMessageConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(AppendAsync), 2);
            return ZAppendAsync(pMailboxHandle, pMailMessages, pFlags, pReceived, pConfiguration, lContext);
        }

        private async Task<cAppendFeedback> ZAppendAsync(iMailboxHandle pMailboxHandle, cMailMessageList pMailMessages, cStorableFlags pFlags, DateTime? pReceived, cAppendMailMessageConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZAppendAsync), pMailboxHandle, pMailMessages, pFlags, pReceived);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMailMessages == null) throw new ArgumentNullException(nameof(pMailMessages));

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    return await ZZAppendAsync(lMC, pMailboxHandle, pMailMessages, null, null, LocalStreamReadConfiguration, LocalStreamWriteConfiguration, pFlags, pReceived, null, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await ZZAppendAsync(lMC, pMailboxHandle, pMailMessages, pConfiguration.ConvertSetMaximum, pConfiguration.ConvertIncrement, pConfiguration.ReadConfiguration ?? LocalStreamReadConfiguration, pConfiguration.WriteConfiguration ?? LocalStreamWriteConfiguration, pFlags, pReceived, pConfiguration.AppendSetMaximum, pConfiguration.AppendIncrement, lContext).ConfigureAwait(false);
            }
        }

        private async Task<cAppendFeedback> ZZAppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cMailMessageList pMailMessages, Action<long> pConvertSetMaximum, Action<int> pConvertIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cStorableFlags pFlags, DateTime? pReceived, Action<long> pAppendSetMaximum, Action<int> pAppendIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZAppendAsync), pMC, pMailboxHandle, pMailMessages, pReadConfiguration, pWriteConfiguration, pFlags, pReceived);

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            // special case
            if (pMailMessages.Count == 0) return new cAppendFeedback();

            using (var lDisposables = new cConvertMailMessageDisposables())
            {
                var lMessages = await ZZConvertMailMessagesAsync(pMC, lDisposables, true, pMailMessages, pConvertSetMaximum, pConvertIncrement, pReadConfiguration, pWriteConfiguration, pFlags, pReceived, lContext).ConfigureAwait(false);
                return await lSession.AppendAsync(pMC, pMailboxHandle, lMessages, pAppendSetMaximum, pAppendIncrement, lContext).ConfigureAwait(false);
            }
        }

        /*


            await ZZConvertMailMessageAsync().configureawait(false);






            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);




            // validate that the streams are all good
            ;?; // should include checking that they are poisitionable includes

            foreach (var lMailMessage in pMailMessages)
            {


                ;?; // this should be common with convert mail message 

                // verify that the appenddata will be readable during the append ...
                //  the problem is that anything that is a stream that needs to be served by this client will not work
                //   we will attempt to read from the stream while sending the data => there will be a deadlock 
                //    (the fetch will be queued on the command pipeline while the command pipeline is in the middle of sending the append
                //      the append will wait for the results of the fetch, but the fetch can't be sent until the append has finished)
                //
                // the way this should be done is by connecting a separate client to the same account and using a message instance from the separate client
                //  (this is the best/safest design as it will work with or without catenate)
                //
                // alternatively, read into a temporary file and then use the file as the source of the data (this will obviously never use catenate).

                foreach (var lAlternateView in lMailMessage.AlternateViews)
                    if (lAlternateView.ContentStream is cMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this))
                        throw new cMailMessageFormException(lMailMessage, kMailMessageFormExceptionMessage.MessageDataStreamClient);

                foreach (var lAttachment in lMailMessage.Attachments)
                    if (lAttachment.ContentStream is cMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this))
                        throw new cMailMessageFormException(lMailMessage, kMailMessageFormExceptionMessage.MessageDataStreamClient);


            }













                using (var lTempFileCollection = new TempFileCollection())
            {
                var lMessages = new cAppendDataList();

                // set maximum for the quotedprintableencode 
                //  count the number of things that might need to be converted, adding them up
                //  (this includes the string text)
                //  and call qpsetmax
                ;?;


                // convert the messages
                foreach (var lMailMessage in pMailMessages)
                {
                    // verify that the appenddata will be readable during the append ...
                    //  the problem is that anything that is a stream that needs to be served by this client will not work
                    //   we will attempt to read from the stream while sending the data => there will be a deadlock 
                    //    (the fetch will be queued on the command pipeline while the command pipeline is in the middle of sending the append
                    //      the append will wait for the results of the fetch, but the fetch can't be sent until the append has finished)
                    //
                    // the way this should be done is by connecting a separate client to the same account and using a message instance from the separate client
                    //  (this is the best/safest design as it will work with or without catenate)
                    //
                    // alternatively, read into a temporary file and then use the file as the source of the data (this will obviously never use catenate).
                    
                    foreach (var lAlternateView in lMailMessage.AlternateViews)
                        if (lAlternateView.ContentStream is cMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this))
                            throw new cMailMessageFormException(lMailMessage, kMailMessageFormExceptionMessage.MessageDataStreamClient);

                    foreach (var lAttachment in lMailMessage.Attachments)
                        if (lAttachment.ContentStream is cMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this))
                            throw new cMailMessageFormException(lMailMessage, kMailMessageFormExceptionMessage.MessageDataStreamClient);

                    // convert
                    var lResult = await ZZConvertMailMessageAsync(pMC, lTempFileCollection, lMailMessage, pIncrement, pReadConfiguration, pWriteConfiguration, lContext).ConfigureAwait(false);

                    // add
                    lMessages.Add(new cMultiPartAppendData(pFlags, pReceived, lResult.Parts.AsReadOnly(), lResult.Encoding));
                }

                // append
                return await lSession.AppendAsync(pMC, pMailboxHandle, lMessages, pSetMaximum, pIncrement, lContext).ConfigureAwait(false);
            }
        } */
    }
}
