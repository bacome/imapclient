using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// An IMAP server to use during <see cref="cIMAPClient.Connect"/>. See <see cref="cIMAPClient.Server"/>.
    /// </summary>
    public class cServer
    {
        /**<summary>The host name.</summary>*/
        public readonly string Host;
        /**<summary>The port number.</summary>*/
        public readonly int Port;
        /**<summary>Indicates if SSL should be used.</summary>*/
        public readonly bool SSL;

        /// <summary>
        /// Port set to 143 and SSL set to false.
        /// </summary>
        /// <param name="pHost">The host name.</param>
        public cServer(string pHost)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            Host = pHost;
            Port = 143;
            SSL = false;
        }

        /// <summary>
        /// Port set to 143 if SSL is false, otherwise set to 993.
        /// </summary>
        /// <param name="pHost">The host name.</param>
        /// <param name="pSSL">Indicates if SSL should be used.</param>
        public cServer(string pHost, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            Host = pHost;
            if (pSSL) Port = 993; else Port = 143;
            SSL = pSSL;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pHost">The host name.</param>
        /// <param name="pPort">The port number.</param>
        /// <param name="pSSL">Indicates if SSL should be used.</param>
        public cServer(string pHost, int pPort, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (pPort < 1 || pPort > 65536) throw new ArgumentOutOfRangeException(nameof(pPort));
            Host = pHost;
            Port = pPort;
            SSL = pSSL;
        }

        public override string ToString() => $"{nameof(cServer)}({Host}:{Port},{nameof(SSL)}={SSL})";
    }
}