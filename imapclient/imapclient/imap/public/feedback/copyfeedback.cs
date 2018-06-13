using System;
using System.Collections;
using System.Collections.Generic;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains feedback on a copy operation, based on the RFC 4315 UIDCOPY response.
    /// </summary>
    /// <remarks>
    /// Provides a mapping from source UIDs to the UIDs created.
    /// </remarks>
    public class cCopyFeedback : IReadOnlyDictionary<cUID, cUID>
    {
        private Dictionary<cUID, cUID> mDictionary = new Dictionary<cUID, cUID>();

        internal cCopyFeedback(uint pSourceUIDValidity, cUIntList pSourceUIDs, uint pDestinationUIDValidity, cUIntList pCreatedUIDs)
        {
            if (pSourceUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pSourceUIDValidity));
            if (pSourceUIDs == null) throw new ArgumentNullException(nameof(pSourceUIDs));
            if (pSourceUIDs.Count == 0) throw new ArgumentOutOfRangeException(nameof(pSourceUIDs));
            if (pDestinationUIDValidity == 0) throw new ArgumentOutOfRangeException(nameof(pDestinationUIDValidity));
            if (pCreatedUIDs == null) throw new ArgumentNullException(nameof(pCreatedUIDs));
            if (pCreatedUIDs.Count != pSourceUIDs.Count) throw new ArgumentOutOfRangeException(nameof(pCreatedUIDs));

            for (int i = 0; i < pSourceUIDs.Count; i++) mDictionary[new cUID(pSourceUIDValidity, pSourceUIDs[i])] = new cUID(pDestinationUIDValidity, pCreatedUIDs[i]);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mDictionary.Count;

        /**<summary>Gets the UIDs that were created.</summary>*/
        public IEnumerable<cUID> Values => mDictionary.Values;
        /**<summary>Gets the source UIDs.</summary>*/
        public IEnumerable<cUID> Keys => mDictionary.Keys;

        /// <summary>
        /// Determines whether the specified UID was a source.
        /// </summary>
        /// <param name="pSourceUID"></param>
        /// <returns></returns>
        public bool ContainsKey(cUID pSourceUID) => mDictionary.ContainsKey(pSourceUID);

        /// <summary>
        /// Gets the created UID for the specified source UID.
        /// </summary>
        /// <param name="pSourceUID"></param>
        /// <param name="rCreatedUID"></param>
        /// <returns></returns>
        public bool TryGetValue(cUID pSourceUID, out cUID rCreatedUID) => mDictionary.TryGetValue(pSourceUID, out rCreatedUID);

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<KeyValuePair<cUID, cUID>> GetEnumerator() => mDictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mDictionary.GetEnumerator();

        /// <summary>
        /// Gets the created UID for the specified source UID.
        /// </summary>
        /// <param name="pKey"></param>
        /// <returns></returns>
        public cUID this[cUID pSourceUID] => mDictionary[pSourceUID];

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCopyFeedback));
            foreach (var lPair in mDictionary) lBuilder.Append($"{lPair.Key}->{lPair.Value}");
            return lBuilder.ToString();
        }
    }
}