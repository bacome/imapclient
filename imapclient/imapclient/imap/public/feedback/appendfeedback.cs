using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public enum eAppendFeedbackType { succeeded, failedwithresult, failedwithexception, resultunknown, notattempted }

    public class cAppendFeedbackItem
    {
        public readonly eAppendFeedbackType Type;
        public readonly cUID UID;
        public readonly cIMAPCommandResult Result;
        public readonly fIMAPCapabilities TryIgnoring;
        public readonly Exception Exception;

        internal cAppendFeedbackItem(bool pSucceeded)
        {
            if (pSucceeded) Type = eAppendFeedbackType.succeeded;
            else Type = eAppendFeedbackType.notattempted;

            UID = null;
            Result = null;
            TryIgnoring = 0;
            Exception = null;
        }

        internal cAppendFeedbackItem(cUID pUID)
        {
            Type = eAppendFeedbackType.succeeded;
            UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
            Result = null;
            TryIgnoring = 0;
            Exception = null;
        }

        internal cAppendFeedbackItem(cIMAPCommandResult pResult, fIMAPCapabilities pTryIgnoring)
        {
            Type = eAppendFeedbackType.failedwithresult;
            UID = null;
            Result = pResult ?? throw new ArgumentNullException(nameof(pResult));
            TryIgnoring = pTryIgnoring;
            Exception = null;
        }

        internal cAppendFeedbackItem(Exception pException)
        {
            if (pException is cCommandResultUnknownException lResultUnknown)
            {
                Type = eAppendFeedbackType.resultunknown;
                UID = null;
                Result = null;
                TryIgnoring = 0;
                Exception = lResultUnknown.InnerException;
            }
            else
            {
                Type = eAppendFeedbackType.failedwithexception;
                UID = null;
                Result = null;
                TryIgnoring = 0;
                Exception = pException;
            }
        }

        public override string ToString() => $"{nameof(cAppendFeedbackItem)}({Type},{UID},{Result},{TryIgnoring},{Exception})";
    }

    public class cAppendFeedback : IReadOnlyList<cAppendFeedbackItem>
    {
        public readonly int SucceededCount = 0;
        public readonly int FailedWithResultCount = 0;
        public readonly int FailedWithExceptionCount = 0;
        public readonly int ResultUnknownCount = 0;
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
                    case eAppendFeedbackType.succeeded:

                        SucceededCount++;
                        break;

                    case eAppendFeedbackType.failedwithresult:

                        FailedWithResultCount++;
                        break;

                    case eAppendFeedbackType.failedwithexception:

                        FailedWithExceptionCount++;
                        break;

                    case eAppendFeedbackType.resultunknown:

                        ResultUnknownCount++;
                        break;

                    case eAppendFeedbackType.notattempted:

                        NotAttemptedCount++;
                        break;

                    default:

                        throw new cInternalErrorException(nameof(cAppendFeedback));
                }
            }

            mItems = pItems.AsReadOnly();
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
            lBuilder.Append(SucceededCount);
            lBuilder.Append(FailedWithResultCount);
            lBuilder.Append(FailedWithExceptionCount);
            lBuilder.Append(ResultUnknownCount);
            lBuilder.Append(NotAttemptedCount);
            foreach (var lItem in mItems) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }
    }
}