using System;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents the decoding required for message data.
    /// </summary>
    public enum eDecodingRequired
    {
        /** <summary>Decoding is required, but it is not <see cref="quotedprintable"/> or <see cref="base64"/> decoding.</summary> */
        other,

        /** <summary>No decoding is required.</summary> */
        none,

        /** <summary>Quoted-printable decoding is required.</summary> */
        quotedprintable,

        /** <summary>Base64 decoding is required.</summary> */
        base64
    }
}