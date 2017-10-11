﻿using System;
using System.Collections;
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

    public class cCopyFeedback : IReadOnlyList<cCopyFeedbackItem>
    {
        private List<cCopyFeedbackItem> mItems = new List<cCopyFeedbackItem>();

        public cCopyFeedback(uint pSourceUIDValidity, cUIntList pSourceUIDs, uint pDestinationUIDValidity, cUIntList pDestinationUIDs)
        {
            for (int i = 0; i < pSourceUIDs.Count; i++)
                mItems.Add(
                    new cCopyFeedbackItem(
                        new cUID(pSourceUIDValidity, pSourceUIDs[i]),
                        new cUID(pDestinationUIDValidity, pDestinationUIDs[i])));
        }

        public cCopyFeedbackItem this[int i] => mItems[i];
        public int Count => mItems.Count;
        public IEnumerator<cCopyFeedbackItem> GetEnumerator() => mItems.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cCopyFeedback));
            foreach (var lItem in mItems) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }
    }
}