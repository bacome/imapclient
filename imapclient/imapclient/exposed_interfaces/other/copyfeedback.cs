using System;
using System.Collections;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains feedback on one message in a copy operation if the server provides an RFC 4315 UIDCOPY response.
    /// </summary>
    /// <seealso cref="cCopyFeedback"/>
    public class cCopyFeedbackItem
    {
        /**<summary>The UID of the source message.</summary>*/
        public readonly cUID Source;
        /**<summary>The UID of the copied message.</summary>*/
        public readonly cUID Destination;

        internal cCopyFeedbackItem(cUID pSource, cUID pDestination)
        {
            Source = pSource;
            Destination = pDestination;
        }

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString() => $"{nameof(cCopyFeedbackItem)}({Source},{Destination})";
    }

    /// <summary>
    /// Contains feedback on copy operations if the server provides provides an RFC 4315 UIDCOPY response.
    /// </summary>
    /// <seealso cref="cMailbox.Copy(IEnumerable{cMessage})"/>
    /// <seealso cref="cMailbox.UIDCopy(IEnumerable{cUID}, cMailbox)"/>
    public class cCopyFeedback : IReadOnlyList<cCopyFeedbackItem>
    {
        private List<cCopyFeedbackItem> mItems = new List<cCopyFeedbackItem>();

        internal cCopyFeedback(uint pSourceUIDValidity, cUIntList pSourceUIDs, uint pDestinationUIDValidity, cUIntList pDestinationUIDs)
        {
            for (int i = 0; i < pSourceUIDs.Count; i++)
                mItems.Add(
                    new cCopyFeedbackItem(
                        new cUID(pSourceUIDValidity, pSourceUIDs[i]),
                        new cUID(pDestinationUIDValidity, pDestinationUIDs[i])));
        }

        /// <summary>
        /// Gets one item of feedback.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public cCopyFeedbackItem this[int i] => mItems[i];
        /**<summary>Gets the number of items of feedback in the instance.</summary>*/
        public int Count => mItems.Count;
        /**<summary>Returns an enumerator that iterates through the items of feedback.</summary>*/
        public IEnumerator<cCopyFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        /**<summary>Returns a string that represents the instance.</summary>*/
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCopyFeedback));
            foreach (var lItem in mItems) lBuilder.Append($"{lItem.Source}->{lItem.Destination}");
            return lBuilder.ToString();
        }
    }
}