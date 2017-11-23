using System;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Represents a selected mailbox.
    /// </summary>
    /// <seealso cref="cIMAPClient.SelectedMailboxDetails"/>
    public interface iSelectedMailboxDetails
    {
        /**<summary>Gets the mailbox that is selected.</summary>*/
        iMailboxHandle MailboxHandle { get; }
        /**<summary>Indicates whether the mailbox is selected for update.</summary>*/
        bool SelectedForUpdate { get; }
        /**<summary>Indicates whether the mailbox can be modified.</summary>*/
        bool AccessReadOnly { get; }
        /**<summary>Gets the message cache of the selected mailbox.</summary>*/
        iMessageCache MessageCache { get; }
    }
}