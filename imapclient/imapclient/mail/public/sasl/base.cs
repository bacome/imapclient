﻿using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents an object that can participate in the library's SASL authentication process.
    /// </summary>
    /// <seealso cref="cSASLAuthentication"/>
    /// <seealso cref="cSASLSecurity"/>
    public abstract class cSASL
    {
        /// <summary>
        /// The SASL mechanism name.
        /// </summary>
        public readonly string MechanismName;

        /// <summary>
        /// The TSL requirement for the contained details to be used.
        /// </summary>
        public readonly eTLSRequirement TLSRequirement;

        /// <summary>
        /// Initialises a new instance with the specified mechanism name and TLS requirement.
        /// </summary>
        /// <param name="pMechanismName"></param>
        /// <param name="pTLSRequirement"></param>
        protected cSASL(string pMechanismName, eTLSRequirement pTLSRequirement)
        {
            MechanismName = pMechanismName;
            TLSRequirement = pTLSRequirement;
        }

        /// <summary>
        /// Returns an object that can participate in the library's SASL authentication process.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// If authentication is successful the library will use <see cref="cSASLAuthentication.GetSecurity"/> to get an object that implements any security layer negotiated as part of the authentication.
        /// <see cref="cSASLAuthentication.GetSecurity"/> must return <see langword="null"/> if no security layer was negotiated.
        /// The <see cref="cSASLAuthentication"/> object will be disposed once authentication is complete (upon either of success or failure).
        /// Any <see cref="cSASLSecurity"/> object obtained will be disposed when the underlying network connection closes.
        /// </remarks>
        public abstract cSASLAuthentication GetAuthentication();

        /// <summary>
        /// Gets an object that identifies the credentials.
        /// </summary>
        public abstract object CredentialId { get; }
    }

    /// <summary>
    /// Represents an object that can participate in the library's SASL authentication process.
    /// </summary>
    /// <remarks>
    /// Instances will be disposed once authentication is complete (upon either of success or failure).
    /// </remarks>
    /// <seealso cref="cSASL"/>
    /// <seealso cref="cSASLSecurity"/>
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
        /// If the authentication exchange should be cancelled then this method should return <see langword="null"/>; the library will then gracefully cancel the exchange with the server.
        /// </para>
        /// <para>
        /// If SASL initial response is in use then the first challenge passed to this method will be <see langword="null"/>. 
        /// If the mechanism supports an initial response then the initial response should be returned, otherwise <see langword="null"/> should be returned. 
        /// If the initial response is 'empty' then a zero length response should be returned, not <see langword="null"/>.
        /// </para>
        /// </remarks>
        public abstract IList<byte> GetResponse(IList<byte> pChallenge);

        /// <summary>
        /// Returns an object that can participate in the library's SASL security layer process.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This will only be called (and will always be called) after a successful authentication exchange.
        /// If no security layer was negotiated through the authentication exchange, <see langword="null"/> must be returned.
        /// If an object is returned it will be disposed when the connection closes.
        /// </remarks>
        public abstract cSASLSecurity GetSecurity();

        /**<summary></summary>*/
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /**<summary></summary>*/
        protected virtual void Dispose(bool pDisposing) { }
    }

    /// <summary>
    /// Represents an object that can participate in the library's SASL security layer process.
    /// </summary>
    /// <remarks>
    /// Instances will be disposed when the connection closes.
    /// </remarks>
    /// <seealso cref="cSASL"/>
    /// <seealso cref="cSASLAuthentication"/>
    public abstract class cSASLSecurity : IDisposable
    {
        /// <summary>
        /// Decodes data received from the server.
        /// </summary>
        /// <param name="pBuffer"></param>
        /// <returns>A buffer of decoded data or <see langword="null"/> if decoding cannot be completed until more input arrives.</returns>
        /// <remarks>
        /// Input buffers of encoded bytes are delivered to this method as they arrive.
        /// Any bytes that cannot be decoded due to there being an 'uneven' number of bytes must be buffered by the implementation.
        /// If there is a decoding error then this method must throw: this will immediately terminate the connection to the server.
        /// </remarks>
        public abstract byte[] Decode(byte[] pBuffer);

        /// <summary>
        /// Encodes client data for sending to the server.
        /// </summary>
        /// <param name="pBuffer"></param>
        /// <returns></returns>
        /// <remarks>
        /// If there is a problem encoding the data this method should return <see langword="null"/> (or it may throw), this will immediately terminate the connection to the server.
        /// </remarks>
        public abstract byte[] Encode(byte[] pBuffer);

        // implement disposable in a way that the sub classes' dispose can be done
        /**<summary></summary>*/
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /**<summary></summary>*/
        protected virtual void Dispose(bool pDisposing) { }
    }
}