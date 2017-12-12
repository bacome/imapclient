using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private static readonly cCommandPart kAppendCommandPart = new cTextCommandPart("APPEND ");

            private async Task<cAppendBatchFeedback> ZAppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cSessionAppendDataList pMessages, Action<int> pIncrement, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZAppendAsync), pMC, pMailboxHandle, pMessages);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
                if (pMessages.Count == 0) throw new ArgumentOutOfRangeException(nameof(pMessages));

                var lItem = mMailboxCache.CheckHandle(pMailboxHandle);

                using (var lBuilder = new cAppendCommandDetailsBuilder((EnabledExtensions & fEnableableExtensions.utf8) != 0, _Capabilities.Binary, mAppendTargetBufferSize, mAppendStreamReadConfiguration, pIncrement))
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kAppendCommandPart, lItem.MailboxNameCommandPart);

                    fCapabilities lTryIgnore;
                    if (pMessages.Count > 1) lTryIgnore = fCapabilities.multiappend;
                    else lTryIgnore = 0;

                    foreach (var lMessage in pMessages)
                    {
                        if (lMessage.Flags != null)
                        {
                            lBuilder.Add(cCommandPart.Space);
                            lBuilder.BeginList(eListBracketing.bracketed);
                            foreach (var lFlag in lMessage.Flags) lBuilder.Add(new cTextCommandPart(lFlag));
                            lBuilder.EndList();
                        }

                        if (lMessage.Received != null)
                        {
                            lBuilder.Add(cCommandPart.Space);
                            lBuilder.Add(cCommandPartFactory.AsDateTime(lMessage.Received.Value));
                        }

                        lBuilder.Add(cCommandPart.Space);
                        lTryIgnore |= lMessage.AddAppendData(lBuilder);
                    }

                    var lHook = new cAppendCommandHook(pMessages.Count);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("append success");
                        return lHook.AppendBatchFeedback;
                    }

                    if (lResult.ResultType == eCommandResultType.no) return new cAppendFailedBatchFeedback(pMessages.Count, new cUnsuccessfulCompletionException(lResult.ResponseText, lTryIgnore, lContext));
                    return new cAppendFailedBatchFeedback(pMessages.Count, new cProtocolErrorException(lResult, lTryIgnore, lContext));
                }
            }
        }
    }
}