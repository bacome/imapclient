using System;
using System.Collections.Generic;

namespace work.bacome.imapclient.support
{
    internal class cSequenceSets : List<cSequenceSet>
    {
        public cSequenceSets() { }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cSequenceSets));
            foreach (var lItem in this) lBuilder.Append(lItem);
            return lBuilder.ToString();
        }
    }
}