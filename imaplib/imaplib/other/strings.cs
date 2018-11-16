using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A read-only collection of strings.
    /// </summary>
    [Serializable]
    public class cStrings : ReadOnlyCollection<string>
    {
        /// <summary>
        /// An empty collection of strings.
        /// </summary>
        public static readonly cStrings Empty = new cStrings(new string[] { });

        public cStrings(IList<string> pStrings) : base(pStrings) { }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStrings));
            foreach (var lString in this) lBuilder.Append(lString);
            return lBuilder.ToString();
        }
    }
}