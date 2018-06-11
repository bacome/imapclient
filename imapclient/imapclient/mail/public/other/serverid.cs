using System;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents a mail service.
    /// </summary>
    public class cServiceId : IEquatable<cServiceId>
    {
        /**<summary>The host name of the server.</summary>*/
        public readonly string Host;
        /**<summary>The port number to connect to.</summary>*/
        public readonly int Port;
        /**<summary>Indicates whether the service requires that TLS be established immediately upon connect.</summary>*/
        public readonly bool SSL;

        /// <summary>
        /// Initialises a new instance with the specified host name, port number and SSL setting.
        /// </summary>
        /// <param name="pHost"></param>
        /// <param name="pPort"></param>
        /// <param name="pSSL">Indicates whether the service requires that TLS be established immediately upon connect.</param>
        public cServiceId(string pHost, int pPort, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (pPort < 1 || pPort > 65536) throw new ArgumentOutOfRangeException(nameof(pPort));
            Host = pHost;
            Port = pPort;
            SSL = pSSL;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cServiceId pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cServiceId;

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
        public override string ToString() => $"{nameof(cServiceId)}({Host},{Port},{SSL})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cServiceId pA, cServiceId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Host == pB.Host && pA.Port == pB.Port && pA.SSL == pB.SSL;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cServiceId pA, cServiceId pB) => !(pA == pB);
    }
}