using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        internal cAppendFeedback Append(iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cAppendConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(Append));
            var lTask = ZAppendAsync(pMailboxHandle, pMessages, pConfiguration, lContext);
            mSynchroniser.Wait(lTask, lContext);
            return lTask.Result;
        }

        internal Task<cAppendFeedback> AppendAsync(iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cAppendConfiguration pConfiguration)
        {
            var lContext = mRootContext.NewMethod(nameof(cIMAPClient), nameof(AppendAsync));
            return ZAppendAsync(pMailboxHandle, pMessages, pConfiguration, lContext);
        }

        private async Task<cAppendFeedback> ZAppendAsync(iMailboxHandle pMailboxHandle, cAppendDataList pMessages, cAppendConfiguration pConfiguration, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(ZAppendAsync), pMailboxHandle, pMessages, pConfiguration);

            if (mDisposed) throw new ObjectDisposedException(nameof(cIMAPClient));

            var lSession = mSession;
            if (lSession == null || !lSession.IsConnected) throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);

            if (pMailboxHandle == null) throw new ArgumentNullException(nameof(pMailboxHandle));
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            if (pMessages.Count == 0) return new cAppendFeedback();

            if (pConfiguration == null)
            {
                using (var lToken = mCancellationManager.GetToken(lContext))
                {
                    var lMC = new cMethodControl(mTimeout, lToken.CancellationToken);
                    return await lSession.AppendAsync(lMC, pMailboxHandle, pMessages, null, null, lContext).ConfigureAwait(false);
                }
            }
            else
            {
                var lMC = new cMethodControl(pConfiguration.Timeout, pConfiguration.CancellationToken);
                return await lSession.AppendAsync(lMC, pMailboxHandle, pMessages, pConfiguration.SetMaximum, pConfiguration.Increment, lContext).ConfigureAwait(false);
            }
        }
    }
}
