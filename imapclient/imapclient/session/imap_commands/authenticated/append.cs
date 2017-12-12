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

            private async Task<cAppendFeedback> ZAppendAsync(cMethodControl pMC, iMailboxHandle pMailboxHandle, cSessionAppendDataList pMessages, Action<int> pIncrement, cTrace.cContext pParentContext)
            {
                ;?;
            }
        }
    }
}