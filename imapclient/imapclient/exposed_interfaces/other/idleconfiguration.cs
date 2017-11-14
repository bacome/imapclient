using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains parameters that control what a <see cref="cIMAPClient"/> instance does while idle.
    /// </summary>
    /// <remarks>
    /// <para>Idling refers to the process of inviting the server to send unprompted (by external code) updates to the client with the aim of keeping the client in synch with the server.</para>
    /// <para>A <see cref="cIMAPClient"/> instance can only idle while it <see cref="cIMAPClient.IsConnected"/>.</para>
    /// <para>Idling starts after a defined length of quiet time (see <see cref="StartDelay"/>) on the underlying IMAP protocol connection.</para>
    /// <para>
    /// If <see cref="cCapabilities.Idle"/> is in use then the IDLE command is used.
    /// The IDLE command has to be restarted periodically (see <see cref="IdleRestartInterval"/>) to avoid the connection being closed due to inactivity - the RFC recommends at least once every 29 minutes.
    /// </para>
    /// <para>
    /// If <see cref="cCapabilities.Idle"/> is not in use then the library drops back to a periodic (see <see cref="PollInterval"/>) poll of the server using IMAP CHECK and/ or NOOP.
    /// </para>
    /// <para>
    /// All the parameters that control idling have defaults specified in the contructor (see <see cref="cIdleConfiguration(int, int, int)"/>). All values are specified in milliseconds.
    /// </para>
    /// </remarks>
    /// <seealso cref="cIMAPClient.IdleConfiguration"/>
    public class cIdleConfiguration
    {
        /**<summary>The length of quiet time required before idling starts, in milliseconds.</summary>*/
        public readonly int StartDelay;
        /**<summary>The interval between RFC 2177 IDLE commands, in milliseconds.</summary>*/
        public readonly int IdleRestartInterval;
        /**<summary>The interval between polling commands, in milliseconds.</summary>*/
        public readonly int PollInterval;

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="pStartDelay">The length of quiet time required before the idling starts, in milliseconds.</param>
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

        /**<summary>Returns a string that represents the configuration.</summary>*/
        public override string ToString() => $"{nameof(cIdleConfiguration)}({StartDelay},{IdleRestartInterval},{PollInterval})";
    }
}