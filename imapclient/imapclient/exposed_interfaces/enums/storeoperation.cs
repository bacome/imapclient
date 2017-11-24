using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a store operation type.
    /// </summary>
    /// <seealso cref="cMessage.Store(eStoreOperation, cStorableFlags, ulong?)"/>
    /// <seealso cref="cIMAPClient.Store(System.Collections.Generic.IEnumerable{cMessage}, eStoreOperation, cStorableFlags, ulong?)"/>
    /// <seealso cref="cMailbox.UIDStore(cUID, eStoreOperation, cStorableFlags, ulong?)"/>
    /// <seealso cref="cMailbox.UIDStore(System.Collections.Generic.IEnumerable{cUID}, eStoreOperation, cStorableFlags, ulong?)"/>
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
