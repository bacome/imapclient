using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents the decoding required for message data.
    /// </summary>
    /// <seealso cref="cAttachment.DecodingRequired"/>
    /// <seealso cref="cMessage.Fetch(cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cMailbox.UIDFetch(cUID, cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cSinglePartBody.DecodingRequired"/>
    public enum eDecodingRequired
    {
        /** <summary>Decoding is required, but it is not <see cref="quotedprintable"/> or <see cref="base64"/> decoding.</summary> */
        other,

        /** <summary>No decoding is required.</summary> */
        none,

        /** <summary>Quoted-printable decoding is required.</summary> */
        quotedprintable,

        /** <summary>BASE64 decoding is required.</summary> */
        base64
    }
}