using System;
using System.Collections;
using System.Collections.Generic;
using work.bacome.apidocumentation;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains feedback on one message in a copy operation.
    /// </summary>
    /// <seealso cref="cCopyFeedback"/>
    public class cCopyFeedbackItem
    {
        /**<summary>The UID of the source message.</summary>*/
        public readonly cUID SourceUID;
        /**<summary>The UID of the newly created message.</summary>*/
        public readonly cUID CreatedUID;

        internal cCopyFeedbackItem(cUID pSourceUID, cUID pCreatedUID)
        {
            SourceUID = pSourceUID;
            CreatedUID = pCreatedUID;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cCopyFeedbackItem)}({SourceUID},{CreatedUID})";
    }

    /// <summary>
    /// Contains feedback on copy operations.
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

        /// <summary>
        /// Gets one item.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public cCopyFeedbackItem this[int i] => mItems[i];

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mItems.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<cCopyFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCopyFeedback));
            foreach (var lItem in mItems) lBuilder.Append($"{lItem.SourceUID}->{lItem.CreatedUID}");
            return lBuilder.ToString();
        }
    }
}