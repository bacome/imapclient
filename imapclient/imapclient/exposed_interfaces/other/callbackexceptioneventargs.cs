using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries exceptions raised by external code.
    /// </summary>
    /// <seealso cref="cIMAPClient.CallbackException"/>
    public class cCallbackExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// The exception raised.
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
