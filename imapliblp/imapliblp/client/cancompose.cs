using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Defines a mechanism for determining if a value can be used when composing an RFC 5322 header field value.
    /// </summary>
    public interface iCanComposeHeaderFieldValue
    {
        /// <summary>
        /// Returns <see langword="true"/> if the value can be used when composing an RFC 5322 header field value.
        /// </summary>
        /// <param name="pUTF8HeadersAllowed"></param>
        /// <returns></returns>
        bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed);
    }
}