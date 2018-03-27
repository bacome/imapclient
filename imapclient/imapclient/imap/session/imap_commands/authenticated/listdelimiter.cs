using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

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
                if (_ConnectionState != eIMAPConnectionState.enabled && _ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kListDelimiterCommandPart);

                    var lHook = new cListDelimiterCommandHook();
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("list delimiter success");
                        return lHook.Delimiter;
                    }

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, 0, lContext);
                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
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