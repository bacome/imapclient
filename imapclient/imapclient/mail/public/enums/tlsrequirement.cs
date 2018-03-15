using System;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents a TLS requirement.
    /// </summary>
    public enum eTLSRequirement
    {
        /** <summary>Don't care whether TLS is active or not.</summary> */
        indifferent,

        /** <summary>TLS must be active.</summary> */
        required,

        /** <summary>TLS must not be active.</summary> */
        disallowed
    }
}