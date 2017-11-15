using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// The decoding required for message data.
    /// </summary>
    /// <seealso cref="cAttachment.DecodingRequired"/>
    /// <seealso cref="cMessage.Fetch(cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cMailbox.UIDFetch(cUID, cSection, eDecodingRequired, System.IO.Stream, cBodyFetchConfiguration)"/>
    /// <seealso cref="cSinglePartBody.DecodingRequired"/>
    public enum eDecodingRequired
    {
        /** <summary>The decoding required is unknown.</summary> */
        unknown,

        /** <summary>No decoding is required.</summary> */
        none,

        /** <summary>Quoted-printable decoding is required.</summary> */
        quotedprintable,

        /** <summary>BASE64 decoding is required.</summary> */
        base64
    }
}