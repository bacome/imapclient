﻿using System;
using System.Collections.Generic;
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
            private static readonly cCommandPart kListMailboxesCommandPart = new cTextCommandPart("LIST \"\" ");

            public async Task<IEnumerable<iMailboxHandle>> ListMailboxesAsync(cMethodControl pMC, string pListMailbox, char? pDelimiter, cMailboxPathPattern pPattern, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(ListMailboxesAsync), pMC, pListMailbox, pDelimiter, pPattern);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eIMAPConnectionState.enabled && _ConnectionState != eIMAPConnectionState.notselected && _ConnectionState != eIMAPConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pListMailbox == null) throw new ArgumentNullException(nameof(pListMailbox));
                if (pPattern == null) throw new ArgumentNullException(nameof(pPattern));

                if (!mCommandPartFactory.TryAsListMailbox(pListMailbox, pDelimiter, out var lListMailboxCommandPart)) throw new ArgumentOutOfRangeException(nameof(pListMailbox));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!_Capabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kListMailboxesCommandPart, lListMailboxCommandPart);

                    var lHook = new cCommandHookListMailboxes(mMailboxCache, pPattern);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eIMAPCommandResultType.ok)
                    {
                        lContext.TraceInformation("list mailboxes success");
                        return lHook.MailboxHandles;
                    }

                    if (lResult.ResultType == eIMAPCommandResultType.no) throw new cUnsuccessfulIMAPCommandException(lResult.ResponseText, 0, lContext);
                    throw new cIMAPProtocolErrorException(lResult, 0, lContext);
                }
            }
        }
    }
}