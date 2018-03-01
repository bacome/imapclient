using System;
using work.bacome.imapclient.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP server.
    /// </summary>
    /// <seealso cref="cIMAPClient.Server"/>
    public class cServer : IEquatable<cServer>
    {
        /**<summary>The host name of the server.</summary>*/
        public readonly string Host;
        /**<summary>The port number to connect to.</summary>*/
        public readonly int Port;
        /**<summary>Indicates whether the host requires that TLS be established immediately upon connect.</summary>*/
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
        /// <param name="pSSL">Indicates whether the host requires that TLS be established immediately upon connect.</param>
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
        /// <param name="pSSL">Indicates whether the host requires that TLS be established immediately upon connect.</param>
        public cServer(string pHost, int pPort, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (pPort < 1 || pPort > 65536) throw new ArgumentOutOfRangeException(nameof(pPort));
            Host = pHost;
            Port = pPort;
            SSL = pSSL;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cServer pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cServer;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + Host.GetHashCode();
                lHash = lHash * 23 + Port.GetHashCode();
                lHash = lHash * 23 + SSL.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cServer)}({Host},{Port},{SSL})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cServer pA, cServer pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Host == pB.Host && pA.Port == pB.Port && pA.SSL == pB.SSL;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cServer pA, cServer pB) => !(pA == pB);
    }
}