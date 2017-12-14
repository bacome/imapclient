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

            private async Task<cAppendResult> ZAppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cSessionAppendDataList pMessages, Action<int> pIncrement, cTrace.cContext pParentContext)
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
                        if (lHook.AppendUID == null) return new cAppended(pMessages.Count);
                        else return lHook.AppendUID;
                    }

                    lContext.TraceInformation("append unsuccessful");
                    return new cAppendFailed(pMessages.Count, lResult, lTryIgnore);
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

                public cAppendUID AppendUID { get; private set; } = null;

                public override void ProcessTextCode(eResponseTextContext pTextContext, cByteList pCode, cByteList pArguments, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cAppendCommandHook), nameof(ProcessTextCode), pTextContext, pCode, pArguments);

                    if (pTextContext == eResponseTextContext.success && pCode.Equals(kAppendUID))
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
                                AppendUID = new cAppendUID(lUIDValidity, lUIDs);
                                return;
                            }
                        }

                        lContext.TraceWarning("likely malformed appenduid response");
                    }
                }
            }
        }
    }
}