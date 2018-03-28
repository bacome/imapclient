using System;
using System.Net.Mail;
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

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pData == null) throw new ArgumentNullException(nameof(pData));

            // special case
            if (pData.Count == 0) return new cAppendFeedback();

            // check input
            foreach (var lItem in pData)
            {
                // verify that this instance supports the formats used in the data
                if ((lItem.Format & lSession.SupportedFormats) != lItem.Format) throw new cAppendDataFormatException(lItem);

                // verify that the data will be readable during the append ...
                //  the problem is that anything that is a (or anything that might be converted to a) stream that needs to be served by this client will not work
                //   we will attempt to read from the stream while sending the data => there will be a deadlock 
                //    (the fetch will be queued on the command pipeline while the command pipeline is in the middle of sending the append
                //      the append will wait for the results of the fetch, but the fetch can't be sent until the append has finished)
                //
                // the way this should be done is by connecting a separate client to the same account and using a message instance from the separate client
                //  (this is the best/safest design as it will work with or without catenate)
                //
                // alternatively, read into a temporary file and then use the file as the source of the data (this will obviously never use catenate though).
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

        internal cAppendFeedback Append(iMailboxHandle pMailboxHandle, cMailMessageList pMessages, cStorableFlags pFlags, DateTime? pReceived, cAppendMailMessageConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(Append), 2);
            var lTask = ZAppendAsync(pMailboxHandle, pMessages, pFlags, pReceived, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cAppendFeedback> AppendAsync(iMailboxHandle pMailboxHandle, cMailMessageList pMessages, cStorableFlags pFlags, DateTime? pReceived, cAppendMailMessageConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethodV(nameof(cIMAPClient), nameof(AppendAsync), 2);
            return ZAppendAsync(pMailboxHandle, pMessages, pFlags, pReceived, pConfiguration, lContext);
        }

        private async Task<cAppendFeedback> ZAppendAsync(iMailboxHandle pMailboxHandle, cMailMessageList pMessages, cStorableFlags pFlags, DateTime? pReceived, cAppendMailMessageConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZAppendAsync), pMailboxHandle, pMessages, pFlags, pReceived);

            if (IsDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            // check input
            foreach (var lMessage in pMessages)
            {

                foreach (var lAlternateView in lMessage.AlternateViews)
                {
                    ZAppendCheckAttachment(lMessage, lAlternateView);
                    foreach (var lLinkedResource in lAlternateView.LinkedResources) ZAppendCheckAttachment(lMessage, lLinkedResource);
                }

                foreach (var lAttachment in lMessage.Attachments) ZAppendCheckAttachment(lMessage, lAttachment);
            }

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(Timeout, lToken.CancellationToken);
                    return await ZZAppendAsync(lMC, pMailboxHandle, pMessages, null, null, LocalStreamReadConfiguration, LocalStreamWriteConfiguration, pFlags, pReceived, null, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await ZZAppendAsync(lMC, pMailboxHandle, pMessages, pConfiguration.ConvertSetMaximum, pConfiguration.ConvertIncrement, pConfiguration.ReadConfiguration ?? LocalStreamReadConfiguration, pConfiguration.WriteConfiguration ?? LocalStreamWriteConfiguration, pFlags, pReceived, pConfiguration.AppendSetMaximum, pConfiguration.AppendIncrement, lContext).ConfigureAwait(false);
            }
        }

        private void ZAppendCheckAttachment(MailMessage pMessage, AttachmentBase pAttachment)
        {
            // verify that the data will be readable during the append ... (see above for why)
            if (pAttachment.ContentStream is cIMAPMessageDataStream lMessageDataStream && ReferenceEquals(lMessageDataStream.Client, this)) throw new cMailMessageFormException(pMessage, pAttachment, new cMessageDataClientException());
        }

        private async Task<cAppendFeedback> ZZAppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cMailMessageList pMessages, Action<long> pConvertSetMaximum, Action<int> pConvertIncrement, cBatchSizerConfiguration pReadConfiguration, cBatchSizerConfiguration pWriteConfiguration, cStorableFlags pFlags, DateTime? pReceived, Action<long> pAppendSetMaximum, Action<int> pAppendIncrement, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZZAppendAsync), pMC, pMailboxHandle, pMessages, pReadConfiguration, pWriteConfiguration, pFlags, pReceived);

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            // special case
            if (pMessages.Count == 0) return new cAppendFeedback();

            using (var lDisposables = new cConvertMailMessageDisposables())
            {
                var lData = await YConvertMailMessagesAsync(pMC, lDisposables, true, pMessages, pConvertSetMaximum, pConvertIncrement, pReadConfiguration, pWriteConfiguration, pFlags, pReceived, lContext).ConfigureAwait(false);
                return await lSession.AppendAsync(pMC, pMailboxHandle, lData, pAppendSetMaximum, pAppendIncrement, lContext).ConfigureAwait(false);
            }
        }
    }
}
