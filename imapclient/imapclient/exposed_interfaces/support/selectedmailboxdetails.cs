using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Contains details of a selected mailbox.
    /// </summary>
    /// <seealso cref="cIMAPClient.SelectedMailboxDetails"/>
    public interface iSelectedMailboxDetails
    {
        /**<summary>Gets the mailbox that the details are for.</summary>*/
        iMailboxHandle Handle { get; }
        /**<summary>Indicates whether the mailbox is selected for update.</summary>*/
        bool SelectedForUpdate { get; }
        /**<summary>Indicates whether the mailbox can be modified.</summary>*/
        bool AccessReadOnly { get; }
        /**<summary>Gets the message cache of this mailbox.</summary>*/
        iMessageCache Cache { get; }
    }
}