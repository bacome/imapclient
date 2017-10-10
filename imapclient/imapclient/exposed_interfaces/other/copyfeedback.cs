using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cCopyFeedbackItem
    {
        public readonly cUID Source;
        public readonly cUID Destination;

        public cCopyFeedbackItem(cUID pSource, cUID pDestination)
        {
            Source = pSource;
            Destination = pDestination;
        }

        public override string ToString() => $"{nameof(cCopyFeedbackItem)}({Source},{Destination})";
    }

    public class cCopyFeedback : List<cCopyFeedbackItem>
    {
        private cCopyFeedback(IEnumerable<cCopyFeedbackItem> pItems) : base(pItems) { }

        public static bool TryConstruct(uint pSourceUIDValidity, cSequenceSet pSourceUIDs, uint pDestinationUIDValidity, cSequenceSet pDestinationUIDs, out cCopyFeedback rResult)
        {
            if (pSourceUIDValidity == 0) { rResult = null; return false; }
            if (pSourceUIDs == null) { rResult = null; return false; }
            if (pSourceUIDs.Count == 0) { rResult = null; return false; }
            if (!cUIntList.TryConstruct(pSourceUIDs, -1, false, out var lSourceUIDs)) { rResult = null; return false; }

            if (pDestinationUIDValidity == 0) { rResult = null; return false; }
            if (pDestinationUIDs == null) { rResult = null; return false; }
            if (pDestinationUIDs.Count == 0) { rResult = null; return false; }
            if (!cUIntList.TryConstruct(pDestinationUIDs, -1, false, out var lDestinationUIDs)) { rResult = null; return false; }

            if (lSourceUIDs.Count != lDestinationUIDs.Count) { rResult = null; return false; }

            List<cCopyFeedbackItem> lItems = new List<cCopyFeedbackItem>();

            for (int i = 0; i < lSourceUIDs.Count; i++) lItems.Add(new cCopyFeedbackItem(new cUID(pSourceUIDValidity, lSourceUIDs[i]), new cUID(pDestinationUIDValidity, lDestinationUIDs[i])));

            rResult = new cCopyFeedback(lItems);
            return true;
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCopyFeedback));

            if (Count > 0)
            {

            }

            foreach (var l)






        }
    }

}