﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// A read-only collection of strings.
    /// </summary>
    public class cStrings : ReadOnlyCollection<string>
    {
        /// <summary>
        /// Initialises a new instance with the specified list.
        /// </summary>
        /// <param name="pStrings"></param>
        public cStrings(IList<string> pStrings) : base(pStrings) { }

        /// <summary>
        /// Determines whether two instances contain the same strings in the same order.
        /// </summary>
        /// <param name="pObject"></param>
        /// <returns></returns>
        public override bool Equals(object pObject) => this == pObject as cStrings;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (string lString in this) if (lString == null) lHash = lHash * 23; else lHash = lHash * 23 + lString.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cStrings));
            foreach (var lString in this) lBuilder.Append(lString);
            return lBuilder.ToString();
        }

        /// <summary>
        /// Determines whether two instances contain the same strings in the same order.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator ==(cStrings pA, cStrings pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            if (pA.Count != pB.Count) return false;
            for (int i = 0; i < pA.Count; i++) if (pA[i] != pB[i]) return false;
            return true;
        }

        /// <summary>
        /// Determines whether two instances contain different strings or have them in a different order.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        public static bool operator !=(cStrings pA, cStrings pB) => !(pA == pB);
    }
}