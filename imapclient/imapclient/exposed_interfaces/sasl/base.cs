using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a SASL (RFC 4422) authentication mechanism.
    /// </summary>
    /// <remarks>
    /// This class, along with <see cref="cSASLAuthentication"/> and <see cref="cSASLSecurity"/>, can be used as base classes for externally implemented SASL mechanisms.
    /// </remarks>
    public abstract class cSASL
    {
        /// <summary>
        /// Gets the SASL mechanism name.
        /// </summary>
        public abstract string MechanismName { get; }

        /// <summary>
        /// Gets the TSL requirement for the IMAP AUTHENTICATE command to be used with the details contained in this instance.
        /// </summary>
        public abstract eTLSRequirement TLSRequirement { get; }

        /// <summary>
        /// Returns an object that can participate in the IMAP AUTHENTICATION process.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If authentication is successful the library will use <see cref="cSASLAuthentication.GetSecurity"/> to get an object that implements any security layer negotiated as part of the authentication.
        /// <see cref="cSASLAuthentication.GetSecurity"/> must return null if no security layer was negotiated.
        /// The <see cref="cSASLAuthentication"/> object will be disposed once authentication is complete (upon either of success or failure).
        /// Any <see cref="cSASLSecurity"/> object obtained will be disposed when the underlying network connection closes.
        /// </remarks>
        public abstract cSASLAuthentication GetAuthentication();

        /// <summary>
        /// Gets a reference to the <see cref="cSASLAuthentication"/> object used in the last <see cref="cIMAPClient.Connect"/> attempt. May be <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// This will return <see langword="null"/> if the mechanism was not attempted.
        /// This property exists for mechanisms that have out of band error reporting (e.g. XOAUTH2) and provides a way for the out of band errors to be passed back to external code.
        /// <note type="note">Any object returned will almost certainly have been disposed.</note>
        /// </remarks>
        public cSASLAuthentication LastAuthentication { get; internal set; }
    }

    /// <summary>
    /// Represents a SASL authentication negotiator.
    /// </summary>
    /// <remarks>
    /// This class, along with <see cref="cSASL"/> and <see cref="cSASLSecurity"/>, can be used as base classes for externally implemented SASL mechanisms.
    /// Instances will be disposed once authentication is complete (upon either successful or unsuccessful authentication).
    /// </remarks>
    public abstract class cSASLAuthentication : IDisposable
    {
        /// <summary>
        /// Gets the client response to a server challenge.
        /// </summary>
        /// <param name="pChallenge"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// The library passes the BASE64 decoded challenge to this method and BASE64 encodes the returned response before sending it to the server.
        /// If the authentication exchange should be cancelled this method should return <see langword="null"/>; the library will then gracefully cancel the exchange with the server.
        /// </para>
        /// <para>
        /// If <see cref="cCapabilities.SASL_IR"/> is in use then the first challenge passed to this method will be <see langword="null"/>. 
        /// If the mechanism supports an initial response then the initial response should be returned, otherwise <see langword="null"/> should be returned. 
        /// If the initial response is 'empty' then a zero length response should be returned, not <see langword="null"/>.
        /// </para>
        /// </remarks>
        public abstract IList<byte> GetResponse(IList<byte> pChallenge);

        /// <summary>
        /// Gets an object that implements a SASL security layer.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This will only be called (and will always be called) after a successful authentication exchange.
        /// If no security layer was negotiated through the authentication exchange, <see langword="null"/> must be returned.
        /// If an object is returned it will be disposed when the connection closes.
        /// </remarks>
        public abstract cSASLSecurity GetSecurity();

        // implement disposable in a way that the sub classes' dispose can be done
        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool pDisposing) { }
    }

    /// <summary>
    /// Represents a SASL security layer.
    /// </summary>
    /// <remarks>
    /// This class, along with <see cref="cSASL"/> and <see cref="cSASLAuthentication"/>, can be used as base classes for externally implemented SASL mechanisms.
    /// Instances will be disposed when the connection closes.
    /// </remarks>
    public abstract class cSASLSecurity : IDisposable
    {
        /// <summary>
        /// Decodes data received from the server.
        /// </summary>
        /// <param name="pBuffer">The data received from the server.</param>
        /// <returns>A buffer of decoded data or <see langword="null"/> if decoding cannot be completed until more input arrives.</returns>
        /// <remarks>
        /// Input buffers of encoded bytes are delivered to this method as they arrive.
        /// Any bytes that cannot be decoded due to there being an 'uneven number' of bytes must be buffered.
        /// If there is a decoding error then this method must throw: this will immediately terminate the connection to the server.
        /// </remarks>
        public abstract byte[] Decode(byte[] pBuffer);

        /// <summary>
        /// Encodes client data for sending to the server.
        /// </summary>
        /// <param name="pBuffer">The un-encoded data to be sent</param>
        /// <returns>The encoded data to be sent.</returns>
        /// <remarks>
        /// If there is a problem encoding the data this method should return <see langword="null"/> (or it may throw).
        /// </remarks>
        public abstract byte[] Encode(byte[] pBuffer);

        // implement disposable in a way that the sub classes' dispose can be done
        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool pDisposing) { }
    }
}