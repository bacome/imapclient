using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cAppendFeedback Append(iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cAppendConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Append));
            var lTask = ZAppendAsync(pMailboxHandle, pMessages, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cAppendFeedback> AppendAsync(iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cAppendConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(AppendAsync));
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

                    case cMultiPartAppendDataBase lMultiPart:

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
    }
}
