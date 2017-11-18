using System;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP server.
    /// </summary>
    /// <seealso cref="cIMAPClient.Server"/>
    public class cServer
    {
        /**<summary>The host name.</summary>*/
        public readonly string Host;
        /**<summary>The port number.</summary>*/
        public readonly int Port;
        /**<summary>Indicates whether TLS should be established immediately upon TCP connect.</summary>*/
        public readonly bool SSL;

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pHost"></param>
        /// <remarks>
        /// The port is set to 143 and SSL set to <see langword="false"/>.
        /// </remarks>
        public cServer(string pHost)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            Host = pHost;
            Port = 143;
            SSL = false;
        }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pSSL">Indicates whether TLS should be established immediately upon TCP connect.</param>
        /// <remarks>
        /// The port set to 143 if <paramref name="pSSL"/> is <see langword="false"/>, otherwise the port is set to 993.
        /// </remarks>
        public cServer(string pHost, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            Host = pHost;
            if (pSSL) Port = 993; else Port = 143;
            SSL = pSSL;
        }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pPort"></param>
        /// <param name="pSSL">Indicates whether TLS should be established immediately upon TCP connect.</param>
        public cServer(string pHost, int pPort, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (pPort < 1 || pPort > 65536) throw new ArgumentOutOfRangeException(nameof(pPort));
            Host = pHost;
            Port = pPort;
            SSL = pSSL;
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cServer)}({Host}:{Port},{nameof(SSL)}={SSL})";
    }
}