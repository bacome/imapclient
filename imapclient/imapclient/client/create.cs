using System;
using System.Threading.Tasks;
using work.bacome.async;
using work.bacome.trace;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Creates a new mailbox on the connected server.
        /// </summary>
        /// <param name="pMailboxName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// Some servers exhibit unusual behaviour when the mailbox name includes 'unusual' characters (e.g. '/' when it isn't the delimiter). 
        /// Observed behaviours are;
        /// <list type="bullet">
        /// An <item>'OK' reply, but the mailbox created has a truncated name.</item>
        /// </list>
        /// </para>
        /// <para>
        /// Some servers exhibit unusual behaviour when <paramref name="pAsFutureParent"/> is <see langword="true"/> despite this appearing to be in violation of RFC 3501 section 6.3.3.
        /// Observed behaviours are;
        /// <list type="bullet">
        /// <item>An 'OK' reply and a mailbox created with the name specified (correct behaviour according to RFC 3501).</item>
        /// <item>A 'NO' reply and no mailbox created.</item>
        /// <item>An 'OK [CANNOT]' reply and no mailbox created.</item>
        /// <item>An 'OK' reply and a mailbox created with the name specified, but the mailbox has the \NoSelect flag set (so the mailbox can't be used to store messages).</item>
        /// </list>
        /// </para>
        /// </remarks>
        public cMailbox Create(cMailboxName pMailboxName, bool pAsFutureParent = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Create));
            var lTask = ZCreateAsync(pMailboxName, pAsFutureParent, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        /// <summary>
        /// Ansynchronously creates a new mailbox on the connected server.
        /// </summary>
        /// <param name="pMailboxName">The mailbox name to use.</param>
        /// <param name="pAsFutureParent">Indicates to the server that you intend to create child mailboxes in the new mailbox.</param>
        /// <returns></returns>
        /// <inheritdoc cref="Create(cMailboxName, bool)" select="remarks"/>
        public Task<cMailbox> CreateAsync(cMailboxName pMailboxName, bool pAsFutureParent = false)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(CreateAsync));
            return ZCreateAsync(pMailboxName, pAsFutureParent, lContext);
        }

        private async Task<cMailbox> ZCreateAsync(cMailboxName pMailboxName, bool pAsFutureParent, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZCreateAsync), pMailboxName, pAsFutureParent);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxName == null) throw new ArgumentNullException(nameof(pMailboxName));

            using (var lToken = mCancellationManager.GetToken(lContext))
            {
                var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                var lMailboxHandle = await lSession.CreateAsync(lMC, pMailboxName, pAsFutureParent, lContext).ConfigureAwait(false);
                return new cMailbox(this, lMailboxHandle);
            }
        }
    }
}