using System;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains parameters that control the <see cref="cIMAPClient"/> idle feature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The aim of idling is to keep the client in synch with the server, in particular the client's data about the currently selected mailbox.
    /// </para>
    /// <para>
    /// A <see cref="cIMAPClient"/> instance can only idle while it <see cref="cIMAPClient.IsConnected"/>.
    /// </para>
    /// <para>
    /// Idling starts after a configured length of quiet time on the underlying connection (see <see cref="StartDelay"/>).
    /// </para>
    /// <para>
    /// If <see cref="cIMAPCapabilities.Idle"/> is in use then the RFC 2177 IDLE command is used.
    /// The IDLE command has to be restarted periodically to avoid the connection being closed due to inactivity - RFC 2177 recommends at least once every 29 minutes (see <see cref="IdleRestartInterval"/>).
    /// </para>
    /// <para>
    /// If <see cref="cIMAPCapabilities.Idle"/> is not in use then the library does a periodic poll of the server using IMAP CHECK and/ or NOOP (see <see cref="PollInterval"/>).
    /// </para>
    /// <para>
    /// The default values are;
    /// <list type="bullet">
    /// <item><term><see cref="StartDelay"/></term><description>2s</description></item>
    /// <item><term><see cref="IdleRestartInterval"/></term><description>20 minutes</description></item>
    /// <item><term><see cref="PollInterval"/></term><description>60s</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="cIMAPClient.IdleConfiguration"/>
    public class cIdleConfiguration : IEquatable<cIdleConfiguration>
    {
        /**<summary>The length of the quiet time that must pass before idling starts, in milliseconds.</summary>*/
        public readonly int StartDelay;
        /**<summary>The interval between RFC 2177 IDLE commands, in milliseconds.</summary>*/
        public readonly int IdleRestartInterval;
        /**<summary>The interval between polling commands, in milliseconds.</summary>*/
        public readonly int PollInterval;

        /// <summary>
        /// Initialises a new instance with the specified start delay, restart interval and poll interval.
        /// </summary>
        /// <param name="pStartDelay">The length of the quiet time that must pass before idling starts, in milliseconds.</param>
        /// <param name="pIdleRestartInterval">The interval between RFC 2177 IDLE commands, in milliseconds.</param>
        /// <param name="pPollInterval">The interval between polling commands, in milliseconds.</param>
        public cIdleConfiguration(int pStartDelay = 2000, int pIdleRestartInterval = 1200000, int pPollInterval = 60000)
        {
            if (pStartDelay < 0) throw new ArgumentOutOfRangeException(nameof(pStartDelay));
            if (pIdleRestartInterval < 1000) throw new ArgumentOutOfRangeException(nameof(pIdleRestartInterval));
            if (pPollInterval < 1000) throw new ArgumentOutOfRangeException(nameof(pPollInterval));

            StartDelay = pStartDelay;
            IdleRestartInterval = pIdleRestartInterval;
            PollInterval = pPollInterval;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cIdleConfiguration pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cIdleConfiguration;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + StartDelay.GetHashCode();
                lHash = lHash * 23 + IdleRestartInterval.GetHashCode();
                lHash = lHash * 23 + PollInterval.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cIdleConfiguration)}({StartDelay},{IdleRestartInterval},{PollInterval})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cIdleConfiguration pA, cIdleConfiguration pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.StartDelay == pB.StartDelay && pA.IdleRestartInterval == pB.IdleRestartInterval && pA.PollInterval == pB.PollInterval;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cIdleConfiguration pA, cIdleConfiguration pB) => !(pA == pB);
    }
}