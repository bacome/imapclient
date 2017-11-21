using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains parameters that control what a <see cref="cIMAPClient"/> instance does while idle.
    /// </summary>
    /// <remarks>
    /// <para>Idling refers to the process of inviting the server to send unprompted (unprompted by external code) updates to the client with the aim of keeping the client in synch with the server.</para>
    /// <para>A <see cref="cIMAPClient"/> instance can only idle while it <see cref="cIMAPClient.IsConnected"/>.</para>
    /// <para>Idling starts after the configured length of quiet time on the underlying connection has passed (see <see cref="StartDelay"/>).</para>
    /// <para>
    /// If <see cref="cCapabilities.Idle"/> is in use then the RFC 2177 IDLE command is used.
    /// The IDLE command has to be restarted periodically to avoid the connection being closed due to inactivity - RFC 2177 recommends at least once every 29 minutes (see <see cref="IdleRestartInterval"/>).
    /// </para>
    /// <para>
    /// If <see cref="cCapabilities.Idle"/> is not in use then the library does a periodic poll of the server using IMAP CHECK and/ or NOOP (see <see cref="PollInterval"/>).
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
    public class cIdleConfiguration
    {
        /**<summary>The length of the quiet time that must pass before idling starts, in milliseconds.</summary>*/
        public readonly int StartDelay;
        /**<summary>The interval between RFC 2177 IDLE commands, in milliseconds.</summary>*/
        public readonly int IdleRestartInterval;
        /**<summary>The interval between polling commands, in milliseconds.</summary>*/
        public readonly int PollInterval;

        /// <summary>
        /// Initialises a new instance with the specified delay, restart and poll intervals.
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

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cIdleConfiguration)}({StartDelay},{IdleRestartInterval},{PollInterval})";
    }
}