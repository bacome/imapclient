using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    public class cAppendFeedbackItem
    {
        public readonly cUID UID;
        public readonly cCommandResult Failure;

        internal cAppendFeedbackItem(cUID pUID)
        {
            UID = pUID;
            Failure = null;
        }

        internal cAppendFeedbackItem(cCommandResult pFailure)
        {
            Failure = pFailure ?? throw new ArgumentNullException(nameof(pFailure));
            UID = null;
        }

        public override string ToString() => $"{nameof(cAppendFeedbackItem)}({UID},{Failure})";
    }

    public class cAppendFeedback : IReadOnlyList<cAppendFeedbackItem>
    {
        public readonly int AppendedCount = 0;
        public readonly int FailedCount = 0;

        private readonly ReadOnlyCollection<cAppendFeedbackItem> mItems;

        internal cAppendFeedback(IEnumerable<cAppendFeedbackBatch> pBatches)
        {
            if (pBatches == null) throw new ArgumentNullException(nameof(pBatches));

            var lItems = new List<cAppendFeedbackItem>();

            foreach (var lBatch in pBatches)
            {
                if (lBatch == null) throw new ArgumentOutOfRangeException(nameof(pBatches));

                if (lBatch is cAppendFeedbackSuccessfulBatch lSuccess)
                {
                    AppendedCount += lSuccess.UIDs.Count;
                    foreach (var lUID in lSuccess.UIDs) lItems.Add(new cAppendFeedbackItem(new cUID(lSuccess.UIDValidity, lUID)));
                }
                else if (lBatch is cAppendFeedbackFailedBatch lFailed)
                {
                    FailedCount += lFailed.Count;
                    for (int i = 0; i < lFailed.Count; i++) lItems.Add(new cAppendFeedbackItem(lFailed.Result));
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

    internal abstract class cAppendFeedbackBatch { }

    internal class cAppendFeedbackFailedBatch : cAppendFeedbackBatch
    {
        public readonly cCommandResult Result;
        public readonly int Count;

        public cAppendFeedbackFailedBatch(cCommandResult pResult, int pCount)
        {
            Result = pResult ?? throw new ArgumentNullException(nameof(pResult));
            if (pCount < 1) throw new ArgumentOutOfRangeException(nameof(pCount));
            Count = pCount;
        }

        public override string ToString() => $"{nameof(cAppendFeedbackFailedBatch)}({Result},{Count})";
    }

    internal class cAppendFeedbackSuccessfulBatch : cAppendFeedbackBatch
    {
        public readonly uint UIDValidity;
        public readonly cUIntList UIDs;

        public cAppendFeedbackSuccessfulBatch(uint pUIDValidity, cUIntList pUIDs)
        {
            UIDValidity = pUIDValidity;
            UIDs = pUIDs ?? throw new ArgumentNullException(nameof(pUIDs));
        }

        public override string ToString() => $"{nameof(cAppendFeedbackSuccessfulBatch)}({UIDValidity},{UIDs})";
    }
}