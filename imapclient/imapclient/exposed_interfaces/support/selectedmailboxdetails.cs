using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Contains details of a selected mailbox.
    /// </summary>
    /// <seealso cref="cIMAPClient.SelectedMailboxDetails"/>
    public interface iSelectedMailboxDetails
    {
        /**<summary>Gets the internal mailbox handle of the mailbox that the details are for.</summary>*/
        iMailboxHandle Handle { get; }
        /**<summary>Indicates if the mailbox is selected for update.</summary>*/
        bool SelectedForUpdate { get; }
        /**<summary>Indicates if the mailbox can be modified.</summary>*/
        bool AccessReadOnly { get; }
        /**<summary>Gets an object that represents the internal message cache for this mailbox.</summary>*/
        iMessageCache Cache { get; }
    }
}