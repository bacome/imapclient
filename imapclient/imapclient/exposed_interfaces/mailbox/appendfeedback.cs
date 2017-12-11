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

        internal cAppendFeedbackItem(cUID pUID)
        {
            UID = pUID;
            Exception = null;
        }

        internal cAppendFeedbackItem(Exception pException)
        {
            Exception = pException ?? throw new ArgumentNullException(nameof(pException));
            UID = null;
        }

        public override string ToString() => $"{nameof(cAppendFeedbackItem)}({UID},{Exception})";
    }

    public class cAppendFeedback : IReadOnlyList<cAppendFeedbackItem>
    {
        public readonly int AppendedCount = 0;
        public readonly int FailedCount = 0;

        private readonly ReadOnlyCollection<cAppendFeedbackItem> mItems;

        internal cAppendFeedback(IEnumerable<cAppendBatchFeedback> pBatches)
        {
            if (pBatches == null) throw new ArgumentNullException(nameof(pBatches));

            var lItems = new List<cAppendFeedbackItem>();

            foreach (var lBatch in pBatches)
            {
                if (lBatch == null) throw new ArgumentOutOfRangeException(nameof(pBatches));

                ;?;
                if (lBatch is cAppendSuccessfulBatchFeedback lSuccess)
                {
                    AppendedCount += lSuccess.UIDs.Count;
                    foreach (var lUID in lSuccess.UIDs) lItems.Add(new cAppendFeedbackItem(new cUID(lSuccess.UIDValidity, lUID)));
                }
                else if (lBatch is cAppendFailedBatchFeedback lFailed)
                {
                    FailedCount += lFailed.Count;
                    for (int i = 0; i < lFailed.Count; i++) lItems.Add(new cAppendFeedbackItem(lFailed.Exception));
                }
                else throw new cInternalErrorException();
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

    internal class cAppendFailedBatchFeedback : cAppendBatchFeedback
    {
        public readonly Exception Exception;
        public readonly int Count;

        public cAppendFailedBatchFeedback(Exception pException, int pCount)
        {
            Exception = pException ?? throw new ArgumentNullException(nameof(pException));
            if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
            Count = pCount;
        }

        public override string ToString() => $"{nameof(cAppendFailedBatchFeedback)}({Exception},{Count})";
    }

    internal class cAppendSuccessfulBatchFeedback : cAppendBatchFeedback
    {
        public readonly uint UIDValidity;
        public readonly cUIntList UIDs;

        ;?; // this'll need another constructor with just a count


        public cAppendSuccessfulBatchFeedback(uint pUIDValidity, cUIntList pUIDs)
        {
            UIDValidity = pUIDValidity;
            UIDs = pUIDs ?? throw new ArgumentNullException(nameof(pUIDs));
        }

        public override string ToString() => $"{nameof(cAppendSuccessfulBatchFeedback)}({UIDValidity},{UIDs})";
    }
}