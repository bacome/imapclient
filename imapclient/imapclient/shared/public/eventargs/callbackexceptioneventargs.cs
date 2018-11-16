using System;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Carries exceptions raised by external code.
    /// </summary>
    /// <seealso cref="cMailClient.CallbackException"/>
    public class cCallbackExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// The exception that was raised.
        /// </summary>
        public readonly Exception Exception;

        internal cCallbackExceptionEventArgs(Exception pException)
        {
            Exception = pException;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(cCallbackExceptionEventArgs)}({Exception})";
        }
    }
}
