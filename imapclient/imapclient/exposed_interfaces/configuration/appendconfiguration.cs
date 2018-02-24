﻿using System;
using System.Threading;

namespace work.bacome.imapclient
{
    public class cAppendConfiguration 
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<long> SetMaximum;
        public readonly Action<int> Increment;

        public cAppendConfiguration(int pTimeout) 
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            SetMaximum = null;
            Increment = null;
        }

        public cAppendConfiguration(CancellationToken pCancellationToken, Action<long> pSetMaximum, Action<int> pIncrement)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            SetMaximum = pSetMaximum;
            Increment = pIncrement;
        }
    }

    public class cAppendMailMessageConfiguration
    {
        public readonly int Timeout;
        public readonly CancellationToken CancellationToken;
        public readonly Action<long> AppendSetMaximum;
        public readonly Action<int> AppendIncrement;
        public readonly Action<long> QuotedPrintableEncodeSetMaximum;
        public readonly Action<int> QuotedPrintableEncodeIncrement;
        public readonly cBatchSizerConfiguration ReadConfiguration;
        public readonly cBatchSizerConfiguration WriteConfiguration;

        public cAppendMailMessageConfiguration(int pTimeout, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
            Timeout = pTimeout;
            CancellationToken = CancellationToken.None;
            AppendSetMaximum = null;
            AppendIncrement = null;
            QuotedPrintableEncodeSetMaximum = null;
            QuotedPrintableEncodeIncrement = null;
            ReadConfiguration = pReadConfiguration;
            WriteConfiguration = pWriteConfiguration;
        }

        public cAppendMailMessageConfiguration(CancellationToken pCancellationToken, Action<long> pAppendSetMaximum, Action<int> pAppendIncrement, Action<long> pQuotedPrintableEncodeSetMaximum, Action<int> pQuotedPrintableEncodeIncrement, cBatchSizerConfiguration pReadConfiguration = null, cBatchSizerConfiguration pWriteConfiguration = null)
        {
            Timeout = -1;
            CancellationToken = pCancellationToken;
            AppendSetMaximum = pAppendSetMaximum;
            AppendIncrement = pAppendIncrement;
            QuotedPrintableEncodeSetMaximum = pQuotedPrintableEncodeSetMaximum;
            QuotedPrintableEncodeIncrement = pQuotedPrintableEncodeIncrement;
            ReadConfiguration = pReadConfiguration;
            WriteConfiguration = pWriteConfiguration;
        }
    }
}