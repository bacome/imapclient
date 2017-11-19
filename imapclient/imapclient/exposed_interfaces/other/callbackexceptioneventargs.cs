using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Carries exceptions raised in callbacks and event handlers by external code.
    /// </summary>
    /// <seealso cref="cIMAPClient.CallbackException"/>
    public class cCallbackExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// The exception raised by external code.
        /// </summary>
        public readonly Exception Exception;

        internal cCallbackExceptionEventArgs(Exception pException)
        {
            Exception = pException;
        }

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString()
        {
            return $"{nameof(cCallbackExceptionEventArgs)}({Exception})";
        }
    }
}
