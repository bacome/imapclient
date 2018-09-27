using System;

namespace work.bacome.imapclient
{
    [Serializable]
    public class cModSeqFlags
    {
        public readonly cFetchableFlags Flags;
        public readonly ulong ModSeq;

        internal cModSeqFlags(cFetchableFlags pFlags, ulong pModSeq)
        {
            Flags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
            ModSeq = pModSeq;
        }

        public override string ToString() => $"{nameof(cModSeqFlags)}({Flags},{ModSeq})";
    }
}
