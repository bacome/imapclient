using System;

namespace work.bacome.imapclient
{
    public class cCallbackExceptionEventArgs : EventArgs
    {
        public readonly Exception Exception;

        public cCallbackExceptionEventArgs(Exception pException)
        {
            Exception = pException;
        }

        public override string ToString()
        {
            return $"{nameof(cCallbackExceptionEventArgs)}({Exception})";
        }
    }
}
