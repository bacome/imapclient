using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents a message cache.
    /// </summary>
    /// <seealso cref="iMessageHandle"/>
    /// <seealso cref="iSelectedMailboxDetails"/>
    public interface iMessageCache : IReadOnlyList<iMessageHandle>
    {
        /**<summary>Gets the mailbox that this message cache belongs to.</summary>*/
        iMailboxHandle MailboxHandle { get; }

        /**<summary>Indicates that the mailbox does not support the persistent storage of mod-sequences.</summary>*/
        bool NoModSeq { get; }

        /// <summary>
        /// Gets the number of recent messages in the mailbox.
        /// </summary>
        /// <remarks>
        /// See RFC 3501 for a definition of recent.
        /// </remarks>
        int RecentCount { get; }

        /// <summary>
        /// Gets the predicted next UID for the mailbox. May be zero.
        /// </summary>
        /// <remarks>
        /// Zero indicates that the value is not known.
        /// The value may not be up-to-date.
        /// </remarks>
        uint UIDNext { get; }

        /// <summary>
        /// Indicates how out-of-date the <see cref="UIDNext"/> is.
        /// </summary>
        int UIDNextUnknownCount { get; }

        /// <summary>
        /// Gets the UIDValidity of the mailbox. May be zero.
        /// </summary>
        /// <remarks>
        /// Zero indicates that the server does not support unique identifiers.
        /// </remarks>
        uint UIDValidity { get; }

        /// <summary>
        /// Gets the number of unseen messages in the mailbox.
        /// </summary>
        /// <remarks>
        /// The value may not be accurate.
        /// </remarks>
        int UnseenCount { get; }

        /**<summary>Indicates how inaccurate the <see cref="UnseenCount"/> may be.</summary>*/
        int UnseenUnknownCount { get; }

        /// <summary>
        /// Gets the highest mod-sequence for the mailbox. May be zero.
        /// </summary>
        /// <remarks>
        /// Zero indicates that <see cref="cCapabilities.CondStore"/> is not in use or that the mailbox does not support the persistent storage of mod-sequences.
        /// </remarks>
        ulong HighestModSeq { get; }
    }
}
