using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The TLS requirement for the components of <see cref="cCredentials"/> to be used during <see cref="cIMAPClient.Connect"/>.
    /// </summary>
    /// <seealso cref="cLogin.TLSRequirement"/>
    /// <seealso cref="cSASL.TLSRequirement"/>
    /// <seealso cref="cSASLAnonymous.TLSRequirement"/>
    /// <seealso cref="cSASLPlain.TLSRequirement"/>
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