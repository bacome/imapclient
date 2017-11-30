using System;
using System.Collections;
using System.Collections.Generic;
using work.bacome.imapclient.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains feedback on one message in a copy operation, based on the RFC 4315 UIDCOPY response.
    /// </summary>
    /// <seealso cref="cCopyFeedback"/>
    public class cCopyFeedbackItem
    {
        /**<summary>The UID of the source message.</summary>*/
        public readonly cUID SourceMessageUID;
        /**<summary>The UID of the newly created message.</summary>*/
        public readonly cUID CreatedMessageUID;

        internal cCopyFeedbackItem(cUID pSourceMessageUID, cUID pCreatedMessageUID)
        {
            SourceMessageUID = pSourceMessageUID;
            CreatedMessageUID = pCreatedMessageUID;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{nameof(cCopyFeedbackItem)}({SourceMessageUID},{CreatedMessageUID})";
    }

    /// <summary>
    /// Contains feedback on a copy operation, based on the RFC 4315 UIDCOPY response.
    /// </summary>
    /// <seealso cref="cMailbox.Copy(IEnumerable{cMessage})"/>
    /// <seealso cref="cMailbox.UIDCopy(IEnumerable{cUID}, cMailbox)"/>
    public class cCopyFeedback : IReadOnlyList<cCopyFeedbackItem>
    {
        private List<cCopyFeedbackItem> mItems = new List<cCopyFeedbackItem>();

        internal cCopyFeedback(uint pSourceUIDValidity, cUIntList pSourceUIDs, uint pDestinationUIDValidity, cUIntList pCreatedUIDs)
        {
            for (int i = 0; i < pSourceUIDs.Count; i++)
                mItems.Add(
                    new cCopyFeedbackItem(
                        new cUID(pSourceUIDValidity, pSourceUIDs[i]),
                        new cUID(pDestinationUIDValidity, pCreatedUIDs[i])));
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Indexer(int)"/>
        public cCopyFeedbackItem this[int i] => mItems[i];

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mItems.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<cCopyFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        /// <inheritdoc/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCopyFeedback));
            foreach (var lItem in mItems) lBuilder.Append($"{lItem.SourceMessageUID}->{lItem.CreatedMessageUID}");
            return lBuilder.ToString();
        }
    }
}