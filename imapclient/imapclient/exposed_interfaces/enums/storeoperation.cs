using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The type of store operation.
    /// </summary>
    public enum eStoreOperation
    {
        /** <summary>Add flags to the flags already set.</summary> */
        add,

        /** <summary>Remove flags from the flags already set.</summary> */
        remove,

        /** <summary>Replace the flags.</summary> */
        replace
    }
}
