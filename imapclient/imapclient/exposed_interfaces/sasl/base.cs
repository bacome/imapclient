using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    public abstract class cSASL
    {
        // the base class for SASL implementations
        //  based on rfc 4422

        // returns a string containing the SASL mechanism name
        public abstract string MechanismName { get; }

        // returns any TLS requirement that these credentials have
        public abstract eTLSRequirement TLSRequirement { get; }

        // gets an object that can do the authentication
        //  it will be disposed once the authentication is complete and any security object is obtained
        //
        public abstract cSASLAuthentication GetAuthentication();

        // provides a reference to the last authentication object issued by the GetAuthentication method
        //  if set the object referenced will almost certainly already be disposed
        //   this property is cleared each time a connect is attempted, therefore if it is null this indicates that this SASL wasn't tried in the last connect
        //    useful if the mechanism's authentication object can provide feedback on why it failed (see XOAuth2 for an example of this)
        //
        public cSASLAuthentication LastAuthentication { get; set; }
    }

    public abstract class cSASLAuthentication : IDisposable
    {
        // implements the challenge/response
        //  should return null to abort the exchange
        //   may throw to abort the exchange
        //   if the parameter is NULL then generate an initial response; if there is no initial response for this mechanism return NULL
        //    (if the initial response is empty then return a zero length collection)
        //
        public abstract IList<byte> GetResponse(IList<byte> pChallenge);

        // gets an object that implements the security layer - should return null if no security layer was negotiated
        //  called each time an authentication is successful
        //   the returned object will be disposed when the connection is closed
        //
        public abstract cSASLSecurity GetSecurity();

        // implement disposable in a way that the sub classes' dispose can be done
        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool pDisposing) { }
    }

    public abstract class cSASLSecurity : IDisposable
    {
        // returns a decoded buffer if possible otherwise null
        //  the method should cache the unused bytes from the input buffer
        //   must throw if there is a problem
        //
        public abstract byte[] Decode(byte[] pBuffer);

        // outputs an encoded buffer
        //  must throw if there is a problem
        //   if null is returned this will be treated as terminal
        //
        public abstract byte[] Encode(byte[] pBuffer);

        // implement disposable in a way that the sub classes' dispose can be done
        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool pDisposing) { }
    }
}