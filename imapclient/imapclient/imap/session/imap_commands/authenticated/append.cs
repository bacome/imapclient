using System;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
        /* TEMP comment out for cachefile work
            private static readonly cCommandPart kAppendCommandPart = new cTextCommandPart("APPEND ");

            private async Task<cAppendResult> ZAppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cSessionAppendDataList pMessages, Action<int> pIncrement, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ZAppendAsync), pMC, pMailboxHandle, pMessages);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
                if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));
                if (pMessages.Count == 0) throw new ArgumentOutOfRangeException(nameof(pMessages));

                var lItem = mMailboxCache.CheckHandle(pMailboxHandle);

                using (var lBuilder = new cAppendCommandDetailsBuilder((EnabledExtensions & fEnableableExtensions.utf8) != 0, _Capabilities.Binary, mAppendStreamReadConfiguration, pIncrement))
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kAppendCommandPart, lItem.MailboxNameCommandPart);

                    fIMAPCapabilities lTryIgnoring;
                    if (pMessages.Count > 1) lTryIgnoring = fIMAPCapabilities.multiappend;
                    else lTryIgnoring = 0;

                    foreach (var lMessage in pMessages)
                    {
                        if (lMessage.Flags != null && lMessage.Flags.Count > 0)
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
                        lTryIgnoring |= lMessage.AddAppendData(lBuilder);
                    }

                    var lHook = new cAppendCommandHook(pMessages.Count);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("append success");
                        if (lHook.UIDs == null) return new cAppendSucceeded(pMessages.Count);
                        else return lHook.UIDs;
                    }

                    lContext.TraceInformation("append failed");
                    return new cAppendFailedWithResult(pMessages.Count, lResult, lTryIgnoring);
                }
            }

            private class cAppendCommandHook : cCommandHook
            {
                private static readonly cBytes kAppendUID = new cBytes("APPENDUID");

                private int mExpectedCount;

                public cAppendCommandHook(int pExpectedCount)
                {
                    mExpectedCount = pExpectedCount;
                }

                public cAppendSucceededWithUIDs UIDs { get; private set; } = null;

                public override void ProcessTextCode(eIMAPResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cAppendCommandHook), nameof(ProcessTextCode), pTextContext, pCode, pArguments);

                    if (pTextContext == eIMAPResponseTextContext.success && pCode.Equals(kAppendUID))
                    {
                        if (pArguments != null)
                        {
                            cBytesCursor lCursor = new cBytesCursor(pArguments);

                            if (lCursor.GetNZNumber(out _, out var lUIDValidity) &&
                                lCursor.SkipByte(cASCII.SPACE) &&
                                lCursor.GetSequenceSet(out var lUIDSet) &&
                                lCursor.Position.AtEnd &&
                                cUIntList.TryConstruct(lUIDSet, -1, false, out var lUIDs) &&
                                lUIDs.Count == mExpectedCount)
                            {
                                UIDs = new cAppendSucceededWithUIDs(lUIDValidity, lUIDs);
                                return;
                            }
                        }

                        lContext.TraceWarning("likely malformed appenduid response");
                    }
                }
            } */
        }
    }
}