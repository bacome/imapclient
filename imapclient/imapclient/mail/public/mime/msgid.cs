using System;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    public class cMsgId : IEquatable<cMsgId>
    {
        public readonly string IdLeft;
        public readonly string IdRight;

        internal cMsgId(string pIdLeft, string pIdRight, bool pValid)
        {
            IdLeft = pIdLeft ?? throw new ArgumentNullException(nameof(pIdLeft));
            IdRight = pIdRight ?? throw new ArgumentNullException(nameof(pIdRight));
        }

        public cMsgId(string pIdLeft, string pIdRight)
        {
            IdLeft = pIdLeft ?? throw new ArgumentNullException(nameof(pIdLeft));
            IdRight = pIdRight ?? throw new ArgumentNullException(nameof(pIdRight));
            if (!cValidation.IsDotAtom(pIdLeft)) throw new ArgumentOutOfRangeException(nameof(pIdLeft));
            if (!cValidation.IsDotAtom(pIdRight) && !cValidation.IsNoFoldLiteral(pIdRight)) throw new ArgumentOutOfRangeException(nameof(pIdRight));
        }

        public string MessageId => $"<{IdLeft}@{IdRight}>";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cMsgId pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cMsgId;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + IdLeft.GetHashCode();
                lHash = lHash * 23 + IdRight.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cMsgId)}({IdLeft},{IdRight})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cMsgId pA, cMsgId pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.IdLeft == pB.IdLeft && pA.IdRight == pB.IdRight;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cMsgId pA, cMsgId pB) => !(pA == pB);
    }
}