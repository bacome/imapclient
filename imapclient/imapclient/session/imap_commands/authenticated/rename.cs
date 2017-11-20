﻿using System;
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
            private static readonly cCommandPart kRenameCommandPart = new cTextCommandPart("RENAME ");

            public async Task<iMailboxHandle> RenameAsync(cMethodControl pMC, iMailboxHandle pHandle, cMailboxName pMailboxName, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(RenameAsync), pMC, pHandle, pMailboxName);

                if (mDisposed) throw new ObjectDisposedException(nameof(cSession));
                if (_ConnectionState != eConnectionState.notselected && _ConnectionState != eConnectionState.selected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                if (pHandle == null) throw new ArgumentNullException(nameof(pHandle));
                if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

                var lItem = mMailboxCache.CheckHandle(pHandle);
                if (!mCommandPartFactory.TryAsMailbox(pMailboxName.Path, pMailboxName.Delimiter, out var lMailboxCommandPart, out _)) throw new ArgumentOutOfRangeException(nameof(pMailboxName));

                using (var lBuilder = new cCommandDetailsBuilder())
                {
                    if (!mCapabilities.QResync) lBuilder.Add(await mSelectExclusiveAccess.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // block select if mailbox-data delivered during the command would be ambiguous
                    lBuilder.Add(await mMSNUnsafeBlock.GetBlockAsync(pMC, lContext).ConfigureAwait(false)); // this command is msnunsafe

                    lBuilder.Add(kRenameCommandPart, lItem.MailboxNameCommandPart, cCommandPart.Space, lMailboxCommandPart);

                    if (pHandle.MailboxName.IsInbox) lItem = null; // renaming the inbox has special behaviour
                    var lHook = new cRenameCommandHook(mMailboxCache, lItem, pMailboxName);
                    lBuilder.Add(lHook);

                    var lResult = await mPipeline.ExecuteAsync(pMC, lBuilder.EmitCommandDetails(), lContext).ConfigureAwait(false);

                    if (lResult.ResultType == eCommandResultType.ok)
                    {
                        lContext.TraceInformation("rename success");
                        return lHook.Handle;
                    }

                    if (lResult.ResultType == eCommandResultType.no) throw new cUnsuccessfulCompletionException(lResult.ResponseText, 0, lContext);
                    throw new cProtocolErrorException(lResult, 0, lContext);
                }
            }

            private class cRenameCommandHook : cCommandHook
            {
                private readonly cMailboxCache mCache;
                private readonly cMailboxCacheItem mItem;
                private readonly cMailboxName mMailboxName;
                private int mSequence;

                public cRenameCommandHook(cMailboxCache pCache, cMailboxCacheItem pItem, cMailboxName pMailboxName)
                {
                    mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
                    mItem = pItem; // may be null if we are renaming the inbox
                    mMailboxName = pMailboxName ?? throw new ArgumentNullException(nameof(pMailboxName));
                }

                public iMailboxHandle Handle { get; private set; }

                public override void CommandStarted(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cRenameCommandHook), nameof(CommandStarted));
                    mSequence = mItem.MailboxCache.Sequence;
                }

                public override void CommandCompleted(cCommandResult pResult, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cRenameCommandHook), nameof(CommandCompleted), pResult);

                    if (pResult.ResultType != eCommandResultType.ok) return;

                    if (mItem != null)
                    {
                        mItem.ResetExists(lContext);
                        if (mItem.MailboxName.Delimiter != null) mCache.ResetExists(new cMailboxPathPattern(mItem.MailboxName.Path + mItem.MailboxName.Delimiter, "*", mItem.MailboxName.Delimiter), mSequence, lContext);
                    }

                    Handle = mCache.Create(mMailboxName, lContext);
                }
            }
        }
    }
}