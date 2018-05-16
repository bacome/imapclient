using System;
using System.Collections.Generic;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /*
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

        public static cAppendDataList FromData(cAppendData pData)
        {
            if (pData == null) throw new ArgumentNullException(nameof(pData));
            var lResult = new cAppendDataList();
            lResult.Add(pData);
            return lResult;
        }

        public static cAppendDataList FromData(IEnumerable<cAppendData> pData)
        {
            if (pData == null) throw new ArgumentNullException(nameof(pData));

            var lResult = new cAppendDataList();

            foreach (var lItem in pData)
            {
                if (lItem == null) throw new ArgumentOutOfRangeException(nameof(pData), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lResult.Add(lItem);
            }

            return lResult;
        }
    }
    */
}