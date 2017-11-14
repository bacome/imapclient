using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A set of enableable IMAP extensions.
    /// </summary>
    /// <seealso cref="cIMAPClient.EnabledExtensions"/>
    [Flags]
    public enum fEnableableExtensions
    {
        /**<summary>A constant for no extensions</summary>*/
        none = 0,
        /**<summary>RFC 6855</summary>*/
        utf8 = 1,
        /**<summary>A mask for all extensions</summary>*/
        all = 0b1
    }
}