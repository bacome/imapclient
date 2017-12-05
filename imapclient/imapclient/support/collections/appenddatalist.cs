using System;
using System.Collections.Generic;

namespace work.bacome.imapclient
{
    internal class cAppendDataList : List<cAppendData>
    {
        public cAppendDataList() { }
        public cAppendDataList(IEnumerable<cAppendData> pMessages) : base(pMessages) { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cAppendDataList));
            foreach (var lMessage in this) lBuilder.Append(lMessage);
            return lBuilder.ToString();
        }

        public static cAppendDataList FromMessage(cAppendData pMessage)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            var lResult = new cAppendDataList();
            lResult.Add(pMessage);
            return lResult;
        }

        public static cAppendDataList FromMessages(IEnumerable<cAppendData> pMessages)
        {
            if (pMessages == null) throw new ArgumentNullException(nameof(pMessages));

            var lResult = new cAppendDataList();

            foreach (var lMessage in pMessages)
            {
                if (lMessage == null) throw new ArgumentOutOfRangeException(nameof(pMessages), "contains nulls");
                lResult.Add(lMessage);
            }

            return lResult;
        }
    }
}