using System;

namespace work.bacome.imapclient
{
    [Serializable]
    public class cModSeqFlags
    {
        /**<summary>The IMAP FLAGS data.</summary>*/
        public readonly cFetchableFlags Flags;

        /// <summary>
        /// The RFC 7162 mod-sequence data, may be zero.
        /// </summary>
        /// <remarks>
        /// Zero indicates that either <see cref="cIMAPCapabilities.CondStore"/> is not in use or that the mailbox does not support the persistent storage of mod-sequences.
        /// </remarks>
        public readonly ulong ModSeq;

        internal cModSeqFlags(cFetchableFlags pFlags, ulong pModSeq)
        {
            Flags = pFlags ?? throw new ArgumentNullException(nameof(pFlags));
            ModSeq = pModSeq;
        }

        public override string ToString() => $"{nameof(cModSeqFlags)}({Flags},{ModSeq})";
    }
}
