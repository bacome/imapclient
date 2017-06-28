using System;
using System.Threading;

namespace work.bacome.imapclient
{
    public class cFetchControl
    {
        private int mTimeout = -1;

        public int Timeout
        {
            get => mTimeout;

            set
            {
                if (value < -1) throw new ArgumentOutOfRangeException();
                mTimeout = value;
            }
        }

        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        public Action<int> IncrementProgress { get; set; } = null;

        public cFetchSizeConfiguration WriteConfiguration { get; set; } = null;

        public cFetchControl(int pTimeout)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            mTimeout = pTimeout;
        }

        public cFetchControl(CancellationToken pCancellationToken, Action<int> pIncrementProgress = null)
        {
            CancellationToken = pCancellationToken;
            IncrementProgress = pIncrementProgress;
        }
    }
}