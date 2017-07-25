using System;
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
            private static readonly cCommandPart kStatusStatusCommandPart = new cCommandPart("STATUS ");
            private static readonly cCommandPart kStatusSearchUnseenCommandPart = new cCommandPart("SEARCH UNSEEN");
            private static readonly cCommandPart kStatusExtendedSearchUnseenCommandPart = new cCommandPart("SEARCH RETURN () UNSEEN");

            // TODO

            public async Task<cMailboxStatus> StatusAsync(cMethodControl pMC, cMailboxId pMailboxId, fStatusAttributes pAttributes, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(StatusAsync), pMC, pMailboxId, pAttributes);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_State != eState.selected) throw new InvalidOperationException();

                if (pMailboxId.AccountId != _ConnectedAccountId) throw new cAccountNotConnectedException(lContext);

                if (!mStringFactory.TryAsMailbox(pMailboxId.MailboxName, out var lMailboxCommandPart, out var lEncodedMailboxName)) throw new ArgumentOutOfRangeException(nameof(pMailboxId));

                if ((pAttributes & fStatusAttributes.all) == 0) throw new ArgumentOutOfRangeException(nameof(pAttributes));

                using (var lCommand = new cCommand())
                {
                    lCommand.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select

                    if (_SelectedMailbox != null && _SelectedMailbox.MailboxId == pMailboxId)
                    {
                        var lUnseen = _SelectedMailbox.Unseen;
                        if (lUnseen != null || (pAttributes & fStatusAttributes.unseen) == 0) return new cMailboxStatus(_SelectedMailbox.Messages, _SelectedMailbox.Recent, _SelectedMailbox.UIDNext, _SelectedMailbox.UIDValidity, lUnseen);

                        lCommand.Add(await mSetUnseenExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // set unseen commands must be single threaded

                        var lMessages = _SelectedMailbox.SetUnseenBegin(lContext);
                        var lRecent = _SelectedMailbox.Recent;
                        var lUIDNext = _SelectedMailbox.UIDNext;
                        var lUIDValidity = _SelectedMailbox.UIDValidity;

                        var lCapability = _Capability;

                        if (lCapability.ESearch)
                        {
                            lCommand.Add(kStatusExtendedSearchUnseenCommandPart);

                            var lHook = new cStatusExtendedSearchUnseenCommandHook(lCommand.Tag, _SelectedMailbox);
                            lCommand.Add(lHook);

                            var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                            if (lResult.ResultType == eCommandResultType.ok)
                            {
                                lContext.TraceInformation("extended search unseen success");
                                if (lHook.Unseen == null) throw new cUnexpectedServerActionException(fCapabilities.ESearch, "unseen not calculated on a successful extended search unseen", lContext);
                                return new cMailboxStatus(lMessages, lRecent, lUIDNext, lUIDValidity, lHook.Unseen);
                            }

                            if (lHook.Unseen != null) lContext.TraceError("unseen calculated on a failed extended search unseen");

                            if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, fCapabilities.ESearch, lContext);
                            throw new cProtocolErrorException(lResult, fCapabilities.ESearch, lContext);
                        }
                        else
                        {
                            lCommand.Add(await mSearchExclusiveAccess.GetTokenAsync(pMC, lContext).ConfigureAwait(false)); // search commands must be single threaded (so we can tell which result is which)

                            lCommand.Add(kStatusSearchUnseenCommandPart);

                            var lHook = new cStatusSearchUnseenCommandHook(_SelectedMailbox);
                            lCommand.Add(lHook);

                            var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                            if (lResult.ResultType == eCommandResultType.ok)
                            {
                                lContext.TraceInformation("search unseen success");
                                if (lHook.Unseen == null) throw new cUnexpectedServerActionException(0, "unseen not calculated on a successful search unseen", lContext);
                                return new cMailboxStatus(lMessages, lRecent, lUIDNext, lUIDValidity, lHook.Unseen);
                            }

                            if (lHook.Unseen != null) lContext.TraceError("unseen calculated on a failed search unseen");

                            if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                            throw new cProtocolErrorException(lResult, 0, lContext);
                        }
                    }
                    else
                    {
                        lCommand.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // status is msnunsafe

                        lCommand.Add(kStatusStatusCommandPart);
                        lCommand.Add(lMailboxCommandPart);
                        lCommand.Add(cCommandPart.Space);
                        lCommand.Add(pAttributes);

                        var lHook = new cStatusCommandHook(lEncodedMailboxName);
                        lCommand.Add(lHook);

                        var lResult = await mPipeline.ExecuteAsync(pMC, lCommand, lContext).ConfigureAwait(false);

                        if (lResult.ResultType == eCommandResultType.ok)
                        {
                            lContext.TraceInformation("status success");
                            if (lHook.Status == null) throw new cUnexpectedServerActionException(0, "status not received", lContext);
                            return lHook.Status;
                        }

                        if (lHook.Status != null) lContext.TraceError("received status on a failed status");

                        if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                        throw new cProtocolErrorException(lResult, 0, lContext);
                    }
                }
            }

            private class cStatusSearchUnseenCommandHook : cCommandHookBaseSearch
            {
                private readonly cSelectedMailbox mSelectedMailbox;

                public cStatusSearchUnseenCommandHook(cSelectedMailbox pSelectedMailbox)
                {
                    mSelectedMailbox = pSelectedMailbox ?? throw new ArgumentNullException(nameof(pSelectedMailbox));
                }

                public int? Unseen { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusSearchUnseenCommandHook), nameof(CommandCompleted), pResult, pException);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok && mMSNs != null) Unseen = mSelectedMailbox.SetUnseen(mMSNs, lContext);
                }
            }

            private class cStatusExtendedSearchUnseenCommandHook : cCommandHookBaseSearchExtended
            {
                public cStatusExtendedSearchUnseenCommandHook(cCommandTag pCommandTag, cSelectedMailbox pSelectedMailbox) : base(pCommandTag, pSelectedMailbox) { }

                public int? Unseen { get; private set; } = null;

                public override void CommandCompleted(cCommandResult pResult, Exception pException, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cStatusExtendedSearchUnseenCommandHook), nameof(CommandCompleted), pResult, pException);
                    if (pResult != null && pResult.ResultType == eCommandResultType.ok && mSequenceSets != null) Unseen = mSelectedMailbox.SetUnseen(cUIntList.FromSequenceSets(mSequenceSets, (uint)mSelectedMailbox.Messages), lContext);
                }
            }
        }
    }
}