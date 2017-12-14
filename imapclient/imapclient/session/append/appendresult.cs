using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private abstract class cAppendResult { }

            private class cAppended : cAppendResult
            {
                public readonly int Count;

                public cAppended(int pCount)
                {
                    if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
                    Count = pCount;
                }

                public override string ToString() => $"{nameof(cAppended)}({Count})";
            }

            private class cAppendUID : cAppendResult
            {
                public readonly uint UIDValidity;
                public readonly cUIntList UIDs;

                public cAppendUID(uint pUIDValidity, cUIntList pUIDs)
                {
                    UIDValidity = pUIDValidity;
                    UIDs = pUIDs ?? throw new ArgumentNullException(nameof(pUIDs));
                }

                public override string ToString() => $"{nameof(cAppendUID)}({UIDValidity},{UIDs})";
            }

            private class cAppendFailed : cAppendResult
            {
                public readonly int Count;
                public readonly cCommandResult Result;
                public readonly fCapabilities TryIgnore;

                public cAppendFailed(int pCount, cCommandResult pResult, fCapabilities pTryIgnore)
                {
                    if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
                    Count = pCount;
                    Result = pResult ?? throw new ArgumentNullException(nameof(pResult));
                    TryIgnore = pTryIgnore;
                }

                public override string ToString() => $"{nameof(cAppendFailed)}({Count},{Result},{TryIgnore})";
            }
        }
    }
}