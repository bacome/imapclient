using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a TLS requirement.
    /// </summary>
    /// <seealso cref="cIMAPClient.SetPlainAuthenticationParameters(string, string, eTLSRequirement, bool)"/>
    /// <seealso cref="cLogin"/>
    /// <seealso cref="cSASLPlain"/>
    /// <seealso cref="cSASLAnonymous"/>
    /// <seealso cref="cSASL"/>
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