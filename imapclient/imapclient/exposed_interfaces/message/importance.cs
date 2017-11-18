using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The message importance.
    /// </summary>
    /// <seealso cref="cMessage.Importance"/>
    /// <seealso cref="cFilterImportance"/>
    /// <seealso cref="cHeaderFields.Importance"/>
    /// <seealso cref="cHeaderFieldImportance"/>
    public enum eImportance
    {
        low,
        normal,
        high
    }
}