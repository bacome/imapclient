using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents the message's importance.
    /// </summary>
    /// <seealso cref="cMessage.Importance"/>
    /// <seealso cref="cFilterImportance"/>
    /// <seealso cref="cHeaderFields.Importance"/>
    /// <seealso cref="cHeaderFieldImportance"/>
    public enum eImportance
    {
        /**<summary>Low importance.</summary>*/
        low,
        /**<summary>Normal importance.</summary>*/
        normal,
        /**<summary>High importance.</summary>*/
        high
    }
}