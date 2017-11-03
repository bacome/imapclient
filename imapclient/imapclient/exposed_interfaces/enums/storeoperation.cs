using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The store operation type
    /// </summary>
    public enum eStoreOperation
    {
        /** <summary>add flags to the flags already set</summary> */
        add,

        /** <summary>remove flags from the flags already set</summary> */
        remove,

        /** <summary>replace the flags</summary> */
        replace
    }
}
