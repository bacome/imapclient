using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    public enum eAppendFeedbackType { appended, failed, notattempted }

    public class cAppendFeedbackItem
    {
        public readonly eAppendFeedbackType Type;
        public readonly cUID AppendedMessageUID;
        public readonly cCommandResult FailedResult;
        public readonly fCapabilities FailedTryIgnore;

        internal cAppendFeedbackItem(bool pAppended)
        {
            if (pAppended) Type = eAppendFeedbackType.appended;
            else Type = eAppendFeedbackType.notattempted;

            AppendedMessageUID = null;
            FailedResult = null;
            FailedTryIgnore = 0;
        }

        internal cAppendFeedbackItem(cUID pAppendedMessageUID)
        {
            Type = eAppendFeedbackType.appended;
            AppendedMessageUID = pAppendedMessageUID ?? throw new ArgumentNullException(nameof(pAppendedMessageUID));
            FailedResult = null;
            FailedTryIgnore = 0;
        }

        internal cAppendFeedbackItem(cCommandResult pFailedResult, fCapabilities pFailedTryIgnore)
        {
            Type = eAppendFeedbackType.failed;
            AppendedMessageUID = null;
            FailedResult = pFailedResult ?? throw new ArgumentNullException(nameof(pFailedResult));
            FailedTryIgnore = pFailedTryIgnore;
        }

        public override string ToString() => $"{nameof(cAppendFeedbackItem)}({Type},{AppendedMessageUID},{FailedResult},{FailedTryIgnore})";
    }

    public class cAppendFeedback : IReadOnlyList<cAppendFeedbackItem>
    {
        public readonly int AppendedCount = 0;
        public readonly int FailedCount = 0;
        public readonly int NotAttemptedCount = 0;
        private readonly ReadOnlyCollection<cAppendFeedbackItem> mItems;

        internal cAppendFeedback()
        {
            mItems = new List<cAppendFeedbackItem>().AsReadOnly();
        }

        internal cAppendFeedback(List<cAppendFeedbackItem> pItems)
        {
            if (pItems == null) throw new ArgumentNullException(nameof(pItems));

            foreach (var lItem in pItems)
            {
                switch (lItem.Type)
                {
                    case eAppendFeedbackType.appended:

                        AppendedCount++;
                        break;

                    case eAppendFeedbackType.failed:

                        FailedCount++;
                        break;

                    case eAppendFeedbackType.notattempted:

                        NotAttemptedCount++;
                        break;

                    default:

                        throw new cInternalErrorException();
                }
            }
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Indexer(int)"/>
        public cAppendFeedbackItem this[int i] => mItems[i];

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mItems.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<cAppendFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        /// <inheritdoc />
        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cAppendFeedback));
            lBuilder.Append(AppendedCount);
            lBuilder.Append(FailedCount);
            lBuilder.Append(NotAttemptedCount);
            foreach (var lItem in mItems) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }
    }
}