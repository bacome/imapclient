using System;
using System.Collections.Generic;
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
            private static readonly cCommandPart kListDelimiterCommandPart = new cTextCommandPart("LIST \"\" \"\"");

            public async Task<char?> ListDelimiterAsync(cMethodControl pMC, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListDelimiterAsync), pMC);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.enabled && _ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException();

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!mCapabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kListDelimiterCommandPart);

                    var lHook = new cListDelimiterCommandHook();
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("list delimiter success");
                        return lHook.Delimiter;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cListDelimiterCommandHook : cCommandHook
            {
                public cListDelimiterCommandHook() { }

                public char? Delimiter { get; private set; } = null;

                public override eProcessDataResult ProcessData(cResponseData pData, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cListDelimiterCommandHook), nameof(ProcessData));
                    if (!(pData is cResponseDataListDelimiter lListDelimiter)) return eProcessDataResult.notprocessed;
                    Delimiter = lListDelimiter.Delimiter;
                    return eProcessDataResult.observed;
                }
            }
        }
    }
}