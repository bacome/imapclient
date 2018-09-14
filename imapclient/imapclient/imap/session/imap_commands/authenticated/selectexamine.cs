using System;
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
            private static readonly cCommandPart kSelectForUpdateCommandPart = new cTextCommandPart("SELECT ");
            private static readonly cCommandPart kSelectReadOnlyCommandPart = new cTextCommandPart("EXAMINE ");
            private static readonly cCommandPart kSelectCommandPartCondStore = new cTextCommandPart(" (CONDSTORE)");
            private static readonly cCommandPart kSelectCommandPartQResync = new cTextCommandPart(;

            public async Task<cSelectResult> SelectExamineAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, bool pForUpdate, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(SelectAsync), pMC, pMailboxHandle, pForUpdate);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));

                var lItem = mMailboxCache.CheckHandle(pMailboxHandle);

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    lBuilder.Add(await mSelectExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // get exclusive access to the selected mailbox
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    if (pForUpdate) lBuilder.Add(kSelectForUpdateCommandPart);
                    else lBuilder.Add(kSelectReadOnlyCommandPart);

                    lBuilder.Add(lItem.MailboxNameCommandPart);

                    bool lUsingQResync;

                    if ((EnabledExtensions & fEnableableExtensions.qresync) != 0)
                    {
                        var lUIDValidity = PersistentCache.GetUIDValidity(pMailboxHandle.MailboxId, lContext);

                        if (lUIDValidity != 0)
                        {
                            var lHighestModSeq = PersistentCache.GetHighestModSeq(pMailboxHandle.MailboxId, lUIDValidity, lContext);

                            if (lHighestModSeq != 0)
                            {
                                ;/; // the dnager is that someone else adds things to the cache after I do this
                                //  that means that for those items added I could be out of sync after selecting (because I didn't ask 
                                //  for those items to be synched.
                                //
                                // => after the select and before enabling setting the highestmodseq I should check the cache again for UIDs
                                //  if there are new ones I should manually sync flags for those ones
                                //  => the output is 
                                //   the set of UIDs for which qresync has been done (may be none if qresync is off)
                                //
                                var lUIDs = PersistentCache.GetUIDs(pMailboxHandle.MailboxId, lUIDValidity, lContext);

                                if (lUIDs.Count == 0)
                                {

                                }
                                else
                                {
                                    // use qresync
                                }
                            }
                        }


                        ;?; // only use qresync if we have a uidvalidity, a highest mod seq, and some uids cached
                        ;?; //  otherwise behave as if condstore is on and synchronise manually
                        ;?; //  NOTE: if there are no UIDs then DONT manually sync (no need to)
                        ;?; //



                        lBuilder.a;
                    }
                    else
                    {
                        lUsingQResync = false;

                        ;?; // set manually sync expunged

                        if (_Capabilities.CondStore)
                        {
                            lBuilder.Add(kSelectCommandPartCondStore);
                            // and 
                        }
                    }

                    var lHook = new cCommandHookSelect(mMailboxCache, _Capabilities, pMailboxHandle, true, lusingqresync);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("select success");
                        return lHook.Result;
                    }

                    fIMAPCapabilities lTryIgnoring;
                    if (_Capabilities.CondStore) lTryIgnoring = fIMAPCapabilities.condstore;
                    if (_Capabilities.QResync) lTryIgnoring = fIMAPCapabilities.qresync;
                    else lTryIgnoring = 0;

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, lTryIgnoring, lContext);
                    throw new cIMAPProtocolErrorException(lResult, lTryIgnoring, lContext);
                }
            }
        }
    }
}