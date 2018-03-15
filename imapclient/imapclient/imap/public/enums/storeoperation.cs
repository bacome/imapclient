using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a store operation type.
    /// </summary>
    public enum eStoreOperation
    {
        /** <summary>Add flags to the flags that are already set.</summary> */
        add,

        /** <summary>Remove flags from the flags that are already set.</summary> */
        remove,

        /** <summary>Replace the flags that are already set.</summary> */
        replace
    }
}
