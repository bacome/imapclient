using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The TLS requirement
    /// </summary>
    /// <remarks>
    /// TLS can be established by specifying SSL in the server to be used or by the use of the STARTTLS command during connect if it is supported by the server.
    /// The library will automatically use STARTTLS if it is offered and the server does not specify SSL unless the instance has been told to ignore the STARTTLS capability.
    /// </remarks>
    /// <seealso cref="cIMAPClient.Server"/>
    /// <seealso cref="cIMAPClient.SetServer(string, bool)"/>
    /// <seealso cref="cIMAPClient.SetServer(string, int, bool)"/>
    /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
    /// 
    public enum eTLSRequirement
    {
        /** <summary>Don't care whether TLS is active or not</summary> */
        indifferent,

        /** <summary>TLS must be active</summary> */
        required,

        /** <summary>TLS must not be active</summary> */
        disallowed
    }
}