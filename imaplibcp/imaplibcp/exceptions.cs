using System;
using System.Net.Mail;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Thrown when an address has a format that is invalid in the context in which it needs to be used.
    /// </summary>
    public class cAddressFormException : cMailException
    {
        /// <summary>
        /// The mail address concerned. May be <see langword="null"/>.
        /// </summary>
        public readonly MailAddress MailAddress;

        /// <summary>
        /// The address concerned. May be <see langword="null"/>.
        /// </summary>
        public readonly cAddress Address;
        
        /// <summary>
        /// Initialises a new instance with the specified address.
        /// </summary>
        /// <param name="pMailAddress"></param>
        public cAddressFormException(MailAddress pMailAddress)
        {
            MailAddress = pMailAddress;
        }

        /// <summary>
        /// Initialises a new instance with the specified address.
        /// </summary>
        /// <param name="pAddress"></param>
        public cAddressFormException(cAddress pAddress)
        {
            Address = pAddress;
        }
    }



    /*
    ;?; // list

    /// <summary>
    /// Thrown when a <see cref="MailMessage"/> instance has a form that is not supported by the library.
    /// </summary>
    /// <remarks>
    /// The library only supports messages with the header and subject encoding the same.  ()Note that address encodings are ignored (the subject encoding is used). one <see cref="Encoding"/> per mail message.
    /// The library supports seekable streams.
    /// The library supports <see cref="cMessageDataStream"/> instances where;
    /// <see cref="cMessageDataStream.HasKnownLength"/> is <see langword="true"/>.
    /// <see cref="cMessageDataStream"/> must not reference the <see cref="cIMAPClient"/> instance that the message is being appended through
    /// </remarks>
    /// */

    /*
    internal class cTestsException : Exception
    {
        internal cTestsException() { }
        internal cTestsException(string pMessage) : base(pMessage) { }
        internal cTestsException(string pMessage, Exception pInner) : base(pMessage, pInner) { }
        internal cTestsException(string pMessage, cTrace.cContext pContext) : base(pMessage) => pContext.TraceError(pMessage);
        internal cTestsException(string pMessage, Exception pInner, cTrace.cContext pContext) : base(pMessage, pInner) => pContext.TraceError("{0}\n{1}", pMessage, pInner);
    } */
}