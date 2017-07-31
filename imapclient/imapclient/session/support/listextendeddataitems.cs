using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    private partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cListExtendedItems : List<cListExtendedItem>
            {
                public cListExtendedItems() { }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cListExtendedItems));
                    foreach (var lItem in this) lBuilder.Append(lItem);
                    return lBuilder.ToString();
                }
            }

            private class cListExtendedItem
            {
                public readonly string Tag;
                public readonly cExtendedValue Value; // can be null if the value is ()

                public cListExtendedItem(string pTag, cExtendedValue pValue)
                {
                    Tag = pTag;
                    Value = pValue;
                }

                public override string ToString() => $"{nameof(cListExtendedItem)}({Tag},{Value})";
            }
        }
    }
}
