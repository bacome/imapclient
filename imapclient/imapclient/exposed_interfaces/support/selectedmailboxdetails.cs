using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents a selected mailbox.
    /// </summary>
    /// <seealso cref="cIMAPClient.SelectedMailboxDetails"/>
    public interface iSelectedMailboxDetails
    {
        /**<summary>Gets the mailbox.</summary>*/
        iMailboxHandle Handle { get; }
        /**<summary>Indicates whether the mailbox is selected for update.</summary>*/
        bool SelectedForUpdate { get; }
        /**<summary>Indicates whether the mailbox can be modified.</summary>*/
        bool AccessReadOnly { get; }
        /**<summary>Gets the message cache.</summary>*/
        iMessageCache Cache { get; }
    }
}