using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The TLS requirement for the <see cref="cLogin"/> and/ or <see cref="cSASL"/> components of <see cref="cCredentials"/> to be used during <see cref="cIMAPClient.Connect"/>.
    /// </summary>
    /// <remarks>
    /// TLS can be established immediately upon connect if the <see cref="cIMAPClient.Server"/> specifies <see cref="cServer.SSL"/> 
    /// or
    /// after connecting using the IMAP STARTTLS command if both the client and server support it - see <see cref="cCapabilities.StartTLS"/>, <see cref="cIMAPClient.Capabilities"/> and <see cref="cIMAPClient.IgnoreCapabilities"/>.
    /// </remarks>
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