using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of enableable IMAP extensions.
    /// </summary>
    [Flags]
    public enum fEnableableExtensions
    {
        /**<summary>A constant representing no extensions.</summary>*/
        none = 0,
        /**<summary><see cref="cIMAPCapabilities.UTF8Accept"/>, <see cref="cIMAPCapabilities.UTF8Only"/></summary>*/
        utf8 = 1 << 0,
        /**<summary><see cref="cIMAPCapabilities.QResync"/></summary>*/
        qresync = 1 << 1
    }
}