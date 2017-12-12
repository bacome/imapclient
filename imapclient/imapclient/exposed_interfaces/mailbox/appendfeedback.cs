using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    public class cAppendFeedbackItem
    {
        public readonly cUID UID;
        public readonly Exception Exception;

        internal cAppendFeedbackItem()
        {
            UID = null;
            Exception = null;
        }

        internal cAppendFeedbackItem(cUID pUID)
        {
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Exception = null;
        }

        internal cAppendFeedbackItem(Exception pException)
        {
            UID = null;
            Exception = pException ?? throw new ArgumentNullException(nameof(pException));
        }

        public override string ToString() => $"{nameof(cAppendFeedbackItem)}({UID},{Exception})";
    }

    public class cAppendFeedback : IReadOnlyList<cAppendFeedbackItem>
    {
        public readonly int AppendedCount = 0;
        public readonly int FailedCount = 0;

        private readonly ReadOnlyCollection<cAppendFeedbackItem> mItems;

        internal cAppendFeedback()
        {
            mItems = new List<cAppendFeedbackItem>().AsReadOnly();
        }

        internal cAppendFeedback(IEnumerable<cAppendBatchFeedback> pBatches)
        {
            if (pBatches == null) throw new ArgumentNullException(nameof(pBatches));

            var lItems = new List<cAppendFeedbackItem>();

            foreach (var lBatch in pBatches)
            {
                if (lBatch == null) throw new ArgumentOutOfRangeException(nameof(pBatches));

                switch (lBatch)
                {
                    case cAppendSuccessfulBatchFeedback lSuccess:

                        AppendedCount += lSuccess.Count;
                        for (int i = 0; i < lSuccess.Count; i++) lItems.Add(new cAppendFeedbackItem());
                        break;

                    case cAppendSuccessfulBatchFeedbackRFC4315 lSuccessRFC4315:

                        AppendedCount += lSuccessRFC4315.UIDs.Count;
                        foreach (var lUID in lSuccessRFC4315.UIDs) lItems.Add(new cAppendFeedbackItem(new cUID(lSuccessRFC4315.UIDValidity, lUID)));
                        break;

                    case cAppendFailedBatchFeedback lFailed:

                        FailedCount += lFailed.Count;
                        for (int i = 0; i < lFailed.Count; i++) lItems.Add(new cAppendFeedbackItem(lFailed.Exception));
                        break;

                    default:

                        throw new cInternalErrorException();
                }
            }

            if (AppendedCount + FailedCount == 0) throw new ArgumentOutOfRangeException(nameof(pBatches));

            mItems = lItems.AsReadOnly();
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
            foreach (var lItem in mItems) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }
    }

    internal abstract class cAppendBatchFeedback { }

    internal class cAppendSuccessfulBatchFeedback : cAppendBatchFeedback
    {
        public readonly int Count;

        public cAppendSuccessfulBatchFeedback(int pCount)
        {
            if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
            Count = pCount;
        }

        public override string ToString() => $"{nameof(cAppendSuccessfulBatchFeedback)}({Count})";
    }

    internal class cAppendSuccessfulBatchFeedbackRFC4315 : cAppendBatchFeedback
    {
        public readonly uint UIDValidity;
        public readonly cUIntList UIDs;

        public cAppendSuccessfulBatchFeedbackRFC4315(uint pUIDValidity, cUIntList pUIDs)
        {
            UIDValidity = pUIDValidity;
            UIDs = pUIDs ?? throw new ArgumentNullException(nameof(pUIDs));
        }

        public override string ToString() => $"{nameof(cAppendSuccessfulBatchFeedbackRFC4315)}({UIDValidity},{UIDs})";
    }

    internal class cAppendFailedBatchFeedback : cAppendBatchFeedback
    {
        public readonly int Count;
        public readonly Exception Exception;

        public cAppendFailedBatchFeedback(int pCount, Exception pException)
        {
            if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
            Count = pCount;
            Exception = pException ?? throw new ArgumentNullException(nameof(pException));
        }

        public override string ToString() => $"{nameof(cAppendFailedBatchFeedback)}({Count},{Exception})";
    }
}