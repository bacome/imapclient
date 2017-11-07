using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>The TLS requirement.</para>
    /// <para>TLS can be established by specifying SSL in the server to be used or by the use of the IMAP STARTTLS command during connect if it is supported by the server.</para>
    /// <para>The library will automatically use IMAP STARTTLS if it is offered and the server does not specify SSL unless the instance has been told to ignore the STARTTLS capability.</para>
    /// <para>See <see cref="cIMAPClient.Server"/>, <see cref="cIMAPClient.SetServer(string, bool)"/>, <see cref="cIMAPClient.SetServer(string, int, bool)"/> and <see cref="cIMAPClient.IgnoreCapabilities"/>.</para>
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