using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of enableable IMAP extensions.
    /// </summary>
    /// <seealso cref="cIMAPClient.EnabledExtensions"/>
    [Flags]
    public enum fEnableableExtensions
    {
        /**<summary>A constant representing no extensions.</summary>*/
        none = 0,
        /**<summary><see cref="cCapabilities.UTF8Accept"/>, <see cref="cCapabilities.UTF8Only"/></summary>*/
        utf8 = 1,
    }
}