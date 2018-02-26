using System;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cAppendFeedback Append(iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cAppendConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Append), 1);
            var lTask = ZAppendAsync(pMailboxHandle, pMessages, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cAppendFeedback> AppendAsync(iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cAppendConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(AppendAsync), 1);
            return ZAppendAsync(pMailboxHandle, pMessages, pConfiguration, lContext);
        }

        private async Task<cAppendFeedback> ZAppendAsync(iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cAppendConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZAppendAsync), pMailboxHandle, pMessages, pConfiguration);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            // special case
            if (pMessages.Count == 0) return new cAppendFeedback();

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
            foreach (var lMessage in pMessages)
            {
                switch (lMessage)
                {
                    case cMessageAppendData lWholeMessage:

                        if (ReferenceEquals(lWholeMessage.Client, this)) throw new cAppendDataClientException(lMessage);
                        break;

                    case cMessagePartAppendData lMessagePart:

                        if (ReferenceEquals(lMessagePart.Client, this)) throw new cAppendDataClientException(lMessage);
                        break;

                    case cStreamAppendData lStream:

                        {
                            // if the stream where wrapped in another stream this check wouldn't work
                            if (lStream.Stream is cMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this)) throw new cAppendDataClientException(lMessage);
                            break;
                        }

                    case cMultiPartAppendData lMultiPart:

                        foreach (var lPart in lMultiPart.Parts)
                        {
                            switch (lPart)
                            {
                                case cMessageAppendDataPart lWholeMessage:

                                    if (ReferenceEquals(lWholeMessage.Client, this)) throw new cAppendDataClientException(lMessage);
                                    break;

                                case cMessagePartAppendDataPart lMessagePart:

                                    if (ReferenceEquals(lMessagePart.Client, this)) throw new cAppendDataClientException(lMessage);
                                    break;

                                case cUIDSectionAppendDataPart lSection:

                                    if (ReferenceEquals(lSection.Client, this)) throw new cAppendDataClientException(lMessage);
                                    break;

                                case cStreamAppendDataPart lStream:

                                    {
                                        // if the stream where wrapped in another stream this check wouldn't work
                                        if (lStream.Stream is cMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this)) throw new cAppendDataClientException(lMessage);
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
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    return await lSession.AppendAsync(lMC, pMailboxHandle, pMessages, null, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await lSession.AppendAsync(lMC, pMailboxHandle, pMessages, pConfiguration.SetMaximum, pConfiguration.Increment, lContext).ConfigureAwait(false);
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
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    return await ZZAppendAsync(lMC, pMailboxHandle, pMailMessages, null, null, mQuotedPrintableEncodeReadWriteConfiguration, mQuotedPrintableEncodeReadWriteConfiguration, pFlags, pReceived, null, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await ZZAppendAsync(lMC, pMailboxHandle, pMailMessages, pConfiguration.ConvertSetMaximum, pConfiguration.ConvertIncrement, pConfiguration.ReadConfiguration ?? mQuotedPrintableEncodeReadWriteConfiguration, pConfiguration.WriteConfiguration ?? mQuotedPrintableEncodeReadWriteConfiguration, pFlags, pReceived, pConfiguration.AppendSetMaximum, pConfiguration.AppendIncrement, lContext).ConfigureAwait(false);
            }
        }

        private async Task<cAppendFeedback> ZZAppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cMailMessageList pMailMessages, Action<long> pConvertSetMaximum, Action<int> pConvertIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cStorableFlags pFlags, DateTime? pReceived, Action<long> pAppendSetMaximum, Action<int> pAppendIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZAppendAsync), pMC, pMailboxHandle, pMailMessages, pReadConfiguration, pWriteConfiguration, pFlags, pReceived);

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            // special case
            if (pMailMessages.Count == 0) return new cAppendFeedback();

            long lToConvert = 0;
            foreach (var lMailMessage in pMailMessages) lToConvert += ZConvertMailMessageValidate(lMailMessage, lContext);
            mSynchroniser.InvokeActionLong(pConvertSetMaximum, lToConvert, lContext);

            using (var lTempFileCollection = new TempFileCollection())
            {
                var lMessages = new cAppendDataList();

                ;?; // up to here

                lMessages.Add(zcon)


                // foreach , convert

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
