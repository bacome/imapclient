using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    private partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cMailboxCache
            {
                private class cExtendedItems : List<cExtendedItem>
                {
                    public cExtendedItems() { }

                    public override string ToString()
                    {
                        cListBuilder lBuilder = new cListBuilder(nameof(cExtendedItems));
                        foreach (var lItem in this) lBuilder.Append(lItem);
                        return lBuilder.ToString();
                    }
                }

                private class cExtendedItem
                {
                    public readonly string Tag;
                    public readonly cExtendedValue Value; // can be null if the value is ()

                    public cExtendedItem(string pTag, cExtendedValue pValue)
                    {
                        Tag = pTag;
                        Value = pValue;
                    }

                    public override string ToString() => $"{nameof(cExtendedItem)}({Tag},{Value})";
                }
            }
        }
    }
}
