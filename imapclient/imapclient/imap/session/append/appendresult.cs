using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cAppendResult
            {
                public static readonly cAppendFailedWithException Cancelled = new cAppendFailedWithException(new OperationCanceledException());
            }

            private class cAppendSucceeded : cAppendResult
            {
                public readonly int Count;

                public cAppendSucceeded(int pCount)
                {
                    if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
                    Count = pCount;
                }

                public override string ToString() => $"{nameof(cAppendSucceeded)}({Count})";
            }

            private class cAppendSucceededWithUIDs : cAppendResult
            {
                public readonly uint UIDValidity;
                public readonly cUIntList UIDs;

                public cAppendSucceededWithUIDs(uint pUIDValidity, cUIntList pUIDs)
                {
                    UIDValidity = pUIDValidity;
                    UIDs = pUIDs ?? throw new ArgumentNullException(nameof(pUIDs));
                }

                public override string ToString() => $"{nameof(cAppendSucceededWithUIDs)}({UIDValidity},{UIDs})";
            }

            private class cAppendFailedWithResult : cAppendResult
            {
                public readonly int Count;
                public readonly cCommandResult Result;
                public readonly fIMAPCapabilities TryIgnoring;

                public cAppendFailedWithResult(int pCount, cCommandResult pResult, fIMAPCapabilities pTryIgnoring)
                {
                    if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
                    Count = pCount;
                    Result = pResult ?? throw new ArgumentNullException(nameof(pResult));
                    TryIgnoring = pTryIgnoring;
                }

                public override string ToString() => $"{nameof(cAppendFailedWithResult)}({Count},{Result},{TryIgnoring})";
            }

            private class cAppendFailedWithException : cAppendResult
            {
                public readonly int Count;
                public readonly Exception Exception;

                public cAppendFailedWithException(Exception pException)
                {
                    Count = 1;
                    Exception = pException ?? throw new ArgumentNullException(nameof(pException));
                }

                public cAppendFailedWithException(int pCount, Exception pException)
                {
                    if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
                    Count = pCount;
                    Exception = pException ?? throw new ArgumentNullException(nameof(pException));
                }

                public override string ToString() => $"{nameof(cAppendFailedWithResult)}({Count},{Exception})";
            }
        }
    }
}