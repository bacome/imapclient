using System;
using System.IO;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an IMAP message attachment.
    /// </summary>
    /// <remarks>
    /// Instances of this class are only valid whilst the mailbox that they are in remains selected. 
    /// Re-selecting the mailbox will not bring instances back to life.
    /// Instances of this class are only valid whilst the containing mailbox has the same UIDValidity.
    /// </remarks>
    public class cIMAPAttachment : cMailAttachment, IEquatable<cIMAPAttachment>
    {
        /**<summary>The client that the instance was created by.</summary>*/
        public readonly cIMAPClient Client;
        /**<summary>The message that the attachment belongs to.</summary>*/
        public readonly iMessageHandle MessageHandle;

        internal cIMAPAttachment(cIMAPClient pClient, iMessageHandle pMessageHandle, cSinglePartBody pPart) : base(pPart)
        {
            Client = pClient ?? throw new ArgumentNullException(nameof(pClient));
            MessageHandle = pMessageHandle ?? throw new ArgumentNullException(nameof(pMessageHandle));
        }

        public bool IsValid => ReferenceEquals(Client.SelectedMailboxDetails?.MessageCache, MessageHandle.MessageCache);

        /// <summary>
        /// Gets the number of bytes that will have to come over the network from the server to save the attachment.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This may be smaller than <see cref="PartSizeInBytes"/> if <see cref="DecodingRequired"/>) isn't <see cref="eDecodingRequired.none"/> and <see cref="cIMAPCapabilities.Binary"/> is in use.
        /// The size may have to be fetched from the server, but once fetched it will be cached.
        /// </remarks>
        public uint FetchSizeInBytes() => Client.FetchSizeInBytes(MessageHandle, Part);

        /// <summary>
        /// Asynchronously gets the number of bytes that will have to come over the network from the server to save the attachment
        /// </summary>
        /// <inheritdoc cref="FetchSizeInBytes" select="returns|remarks"/>
        public Task<uint> FetchSizeInBytesAsync() => Client.FetchSizeInBytesAsync(MessageHandle, Part);

        /// <summary>
        /// Gets the size in bytes of the decoded attachment if the server is capable of calculating it.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This can only be calculated if <see cref="cIMAPCapabilities.Binary"/> is in use.
        /// The size may have to be fetched from the server, but once fetched it will be cached.
        /// </remarks>
        public uint? DecodedSizeInBytes() => Client.DecodedSizeInBytes(MessageHandle, Part);

        /// <summary>
        /// Asynchronously gets the size in bytes of the decoded attachment if the server is capable of calculating it.
        /// </summary>
        /// <inheritdoc cref="DecodedSizeInBytes" select="returns|remarks"/>
        public Task<uint?> DecodedSizeInBytesAsync() => Client.DecodedSizeInBytesAsync(MessageHandle, Part);

        /// <summary>
        /// Saves the attachment to the specified path.
        /// </summary>
        /// <param name="pPath"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        public override void SaveAs(string pPath, cAttachmentSaveConfiguration pConfiguration = null)
        {








                cFetchConfiguration lConfiguration;

            if (pConfiguration == null) lConfiguration = null;
            else
            {
                if (pConfiguration.SetMaximum != null)
                {
                    var lFetchSizeInBytes = Client.FetchSizeInBytes(MessageHandle, Part);
                    Client.InvokeActionLong(pConfiguration.SetMaximum, lFetchSizeInBytes);
                }

                lConfiguration = new cFetchConfiguration(pConfiguration.Timeout, pConfiguration.CancellationToken, pConfiguration.Increment, pConfiguration.WriteConfiguration);
            }

            using (FileStream lStream = new FileStream(pPath, FileMode.Create))
            {
                Client.Fetch(MessageHandle, Part.Section, Part.DecodingRequired, lStream, lConfiguration);
            }

            if (Part.Disposition?.CreationDateTime != null) File.SetCreationTime(pPath, Part.Disposition.CreationDateTime.Value.ToLocalTime());
            if (Part.Disposition?.ModificationDateTime != null) File.SetLastWriteTime(pPath, Part.Disposition.ModificationDateTime.Value.ToLocalTime());
            if (Part.Disposition?.ReadDateTime != null) File.SetLastAccessTime(pPath, Part.Disposition.ReadDateTime.Value.ToLocalTime());
        }

        /// <summary>
        /// Asynchronously saves the attachment to the specified path.
        /// </summary>
        /// <param name="pPath"></param>
        /// <param name="pConfiguration">Operation specific timeout, cancellation token and progress callbacks.</param>
        /// <returns></returns>
        public override async Task SaveAsAsync(string pPath, cAttachmentSaveConfiguration pConfiguration = null)
        {
            cFetchConfiguration lConfiguration;

            if (pConfiguration == null) lConfiguration = null;
            else
            {
                if (pConfiguration.SetMaximum != null)
                {
                    var lFetchSizeInBytes = await Client.FetchSizeInBytesAsync(MessageHandle, Part).ConfigureAwait(false);
                    Client.InvokeActionLong(pConfiguration.SetMaximum, lFetchSizeInBytes);
                }

                lConfiguration = new cFetchConfiguration(pConfiguration.Timeout, pConfiguration.CancellationToken, pConfiguration.Increment, pConfiguration.WriteConfiguration);
            }

            using (FileStream lStream = new FileStream(pPath, FileMode.Create))
            {
                await Client.FetchAsync(MessageHandle, Part.Section, Part.DecodingRequired, lStream, lConfiguration).ConfigureAwait(false);
            }

            if (Part.Disposition?.CreationDateTime != null) File.SetCreationTime(pPath, Part.Disposition.CreationDateTime.Value.ToLocalTime());
            if (Part.Disposition?.ModificationDateTime != null) File.SetLastWriteTime(pPath, Part.Disposition.ModificationDateTime.Value.ToLocalTime());
            if (Part.Disposition?.ReadDateTime != null) File.SetLastAccessTime(pPath, Part.Disposition.ReadDateTime.Value.ToLocalTime());
        }

        private async Task ZSaveAsAsync(string pPath, cAttachmentSaveConfiguration pConfiguration)
        {
            using (var lMessageDataStream = new cIMAPMessageDataStream(this))
            using (var lFileStream = new FileStream(pPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var lBuffer = new byte[cMailClient.LocalStreamBufferSize];

                var lSetMaximum = pConfiguration?.SetMaximum;
                bool lProgressIsInFetchedBytes = false;

                while (true)
                {
                    var lBytesRead = await lMessageDataStream.ReadAsync(lBuffer, 0, lBuffer.Length).ConfigureAwait(false);

                    if (lBytesRead == 0)
                    {
                        ;?; // might have to call increment if the last few bytes fetched didn't generate any data
                        break;
                    }

                    if (lSetMaximum != null)
                    {
                        var lDataIsCached = lMessageDataStream.DataIsCached;

                        if (lDataIsCached == null) throw new cInternalErrorException(lContext);

                        if (lMessageDataStream.DataIsCached.Value) Client.InvokeActionLong(lSetMaximum, lMessageDataStream.Length, lContext);
                        else
                        {
                            var lFetchSizeInBytes = await Client.FetchSizeInBytesAsync(MessageHandle, Part).ConfigureAwait(false);
                            Client.InvokeActionLong(lSetMaximum, lFetchSizeInBytes);
                            lProgressIsInFetchedBytes = true;
                        }

                        lSetMaximum = null;
                    }

                    await lFileStream.WriteAsync(lBuffer, 0, lBytesRead).ConfigureAwait(false);

                    ;?; // calculate the increment based on this position and the last position


                    if (lProgressIsInFetchedBytes) Client.InvokeActionInt(pConfiguration?.Increment, lMessageDataStream.FetchedBytesPosition);
                    else Client.invokeactionint(pConfiguration?.Increment, lBytesRead);
                }
            }

            if (Part.Disposition?.CreationDateTime != null) File.SetCreationTime(pPath, Part.Disposition.CreationDateTime.Value.ToLocalTime());
            if (Part.Disposition?.ModificationDateTime != null) File.SetLastWriteTime(pPath, Part.Disposition.ModificationDateTime.Value.ToLocalTime());
            if (Part.Disposition?.ReadDateTime != null) File.SetLastAccessTime(pPath, Part.Disposition.ReadDateTime.Value.ToLocalTime());
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cIMAPAttachment pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cIMAPAttachment;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode() 
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + MessageHandle.GetHashCode();
                lHash = lHash * 23 + Part.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cIMAPAttachment)}({MessageHandle},{Part.Section})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator ==(cIMAPAttachment pA, cIMAPAttachment pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Client.Equals(pB.Client) && pA.MessageHandle.Equals(pB.MessageHandle) && pA.Part.Equals(pB.Part);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality(cAPIDocumentationTemplate, cAPIDocumentationTemplate)"/>
        public static bool operator !=(cIMAPAttachment pA, cIMAPAttachment pB) => !(pA == pB);
    }
}