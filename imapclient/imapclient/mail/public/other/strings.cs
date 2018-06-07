using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.mailclient
{
    /// <summary>
    /// A read-only collection of strings.
    /// </summary>
    public class cStrings : ReadOnlyCollection<string>
    {
        public static readonly cStrings Empty = new cStrings(new string[] { });

        internal cStrings(IList<string> pStrings) : base(pStrings) { }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStrings));
            foreach (var lString in this) lBuilder.Append(lString);
            return lBuilder.ToString();
        }
    }
}