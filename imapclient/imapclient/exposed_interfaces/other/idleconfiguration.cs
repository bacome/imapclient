using System;

namespace work.bacome.imapclient
{
    public class cIdleConfiguration
    {
        public readonly int StartDelay;
        public readonly int IdleRestartInterval;
        public readonly int PollInterval;

        public cIdleConfiguration(int pStartDelay = 2000, int pIdleRestartInterval = 1200000, int pPollInterval = 60000)
        {
            if (pStartDelay < 0) throw new ArgumentOutOfRangeException(nameof(pStartDelay));
            if (pIdleRestartInterval < 1000) throw new ArgumentOutOfRangeException(nameof(pIdleRestartInterval));
            if (pPollInterval < 1000) throw new ArgumentOutOfRangeException(nameof(pPollInterval));

            StartDelay = pStartDelay;
            IdleRestartInterval = pIdleRestartInterval;
            PollInterval = pPollInterval;
        }

        public override string ToString() => $"{nameof(cIdleConfiguration)}({StartDelay},{IdleRestartInterval},{PollInterval})";
    }
}