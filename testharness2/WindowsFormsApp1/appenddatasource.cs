using System;
using work.bacome.imapclient;

namespace testharness2
{
    public abstract class cAppendDataSource { }

    public class cAppendDataSourceMessage : cAppendDataSource
    {
        public readonly cMessage Message;

        public cAppendDataSourceMessage(cMessage pMessage)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
        }
    }
}