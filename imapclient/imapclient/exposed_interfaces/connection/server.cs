﻿using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP service.
    /// </summary>
    /// <seealso cref="cIMAPClient.Server"/>
    public class cServer
    {
        /**<summary>The host name of the server.</summary>*/
        public readonly string Host;
        /**<summary>The port number of the service.</summary>*/
        public readonly int Port;
        /**<summary>Indicates whether the service requires that TLS be established immediately upon TCP connect.</summary>*/
        public readonly bool SSL;

        /// <summary>
        /// Initialises a new instance with the specified host name.
        /// </summary>
        /// <param name="pHost"></param>
        /// <remarks>
        /// The port number is set to 143 and SSL set to <see langword="false"/>.
        /// </remarks>
        public cServer(string pHost)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            Host = pHost;
            Port = 143;
            SSL = false;
        }

        /// <summary>
        /// Initialises a new instance with the specified host name and SSL setting.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pSSL">Indicates whether the service requires that TLS be established immediately upon TCP connect.</param>
        /// <remarks>
        /// The port number is set to 143 if <paramref name="pSSL"/> is <see langword="false"/>, otherwise the port number is set to 993.
        /// </remarks>
        public cServer(string pHost, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            Host = pHost;
            if (pSSL) Port = 993; else Port = 143;
            SSL = pSSL;
        }

        /// <summary>
        /// Initialises a new instance with the specified host name, port number and SSL setting.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pPort"></param>
        /// <param name="pSSL">Indicates whether the service requires that TLS be established immediately upon TCP connect.</param>
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