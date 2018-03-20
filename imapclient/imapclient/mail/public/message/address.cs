using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mail;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    /// <summary>
    /// Represents an address on an email; either an individual <see cref="cEmailAddress"/> or a <see cref="cGroupAddress"/>.
    /// </summary>
    public abstract class cAddress : IEquatable<cAddress>
    {
        /// <summary>
        /// The display name for the address, may be <see langword="null"/>.
        /// </summary>
        public readonly cCulturedString DisplayName;

        internal cAddress(cCulturedString pDisplayName)
        {
            DisplayName = pDisplayName;
        }

        /// <summary>
        /// Display text for the address. 
        /// Will be the <see cref="DisplayName"/> except for an <see cref="cEmailAddress"/> without a display name when it will be the <see cref="cEmailAddress.DisplayAddress"/>.
        /// </summary>
        public virtual string DisplayText => DisplayName;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public abstract bool Equals(cAddress pObject);

        /// <inheritdoc />
        public abstract override bool Equals(object pObject);

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public abstract override int GetHashCode();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cAddress pA, cAddress pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.Equals(pB);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cAddress pA, cAddress pB) => !(pA == pB);
    }

    /// <summary>
    /// An immutable collection of addresses.
    /// </summary>
    public class cAddresses : IReadOnlyList<cAddress>, IEquatable<cAddresses>
    {
        private readonly ReadOnlyCollection<cAddress> mAddresses;

        internal cAddresses(List<cAddress> pAddresses)
        {
            mAddresses = pAddresses.AsReadOnly();
        }

        public cAddresses(IEnumerable<cAddress> pAddresses)
        {
            var lAddresses = new List<cAddress>();

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lAddresses.Add(lAddress);
            }

            mAddresses = lAddresses.AsReadOnly();
        }

        /// <summary>
        /// The RFC 5256 sort string for the collection of addresses.
        /// </summary>
        public string SortString
        {
            get
            {
                if (mAddresses.Count == 0) return string.Empty;
                if (mAddresses[0] is cGroupAddress lGroup) return lGroup.DisplayName;
                if (mAddresses[0] is cEmailAddress lEMail) return lEMail.LocalPart;
                throw new cInternalErrorException($"{nameof(cAddresses)}.{nameof(SortString)}");
            }
        }

        /// <summary>
        /// The RFC 5957 display sort string for the collection of addresses.
        /// </summary>
        public string DisplaySortString
        {
            get
            {
                if (mAddresses.Count == 0) return string.Empty;

                if (mAddresses[0] is cGroupAddress lGroup) return lGroup.DisplayName;

                if (mAddresses[0] is cEmailAddress lEMail)
                {
                    if (lEMail.DisplayName != null) return lEMail.DisplayName;
                    return lEMail.Address;
                }

                throw new cInternalErrorException($"{nameof(cAddresses)}.{nameof(DisplaySortString)}");
            }
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Count"/>
        public int Count => mAddresses.Count;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetEnumerator"/>
        public IEnumerator<cAddress> GetEnumerator() => mAddresses.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => mAddresses.GetEnumerator();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Indexer(int)"/>
        public cAddress this[int i] => mAddresses[i];

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cAddresses pObject) => this == pObject;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cAddresses;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                foreach (var lAddress in mAddresses) lHash = lHash * 23 + lAddress.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cAddresses));
            foreach (var lAddress in this) lBuilder.Append(lAddress);
            return lBuilder.ToString();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cAddresses pA, cAddresses pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            if (pA.mAddresses.Count != pB.mAddresses.Count) return false;
            for (int i = 0; i < pA.mAddresses.Count; i++) if (!pA.mAddresses[i].Equals(pB.mAddresses[i])) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cAddresses pA, cAddresses pB) => !(pA == pB);
    }

    /// <summary>
    /// Represents an individual email address.
    /// </summary>
    /// <seealso cref="cAddresses"/>
    public sealed class cEmailAddress : cAddress, IEquatable<cEmailAddress>
    {
        public readonly string LocalPart;
        public readonly string Domain;
        private readonly string mQuotedLocalPart;

        internal cEmailAddress(string pLocalPart, string pDomain, cCulturedString pDisplayName) : base(pDisplayName)
        {
            LocalPart = pLocalPart ?? throw new ArgumentNullException(nameof(pLocalPart));
            Domain = pDomain ?? throw new ArgumentNullException(nameof(pDomain));
            if (cTools.IsDotAtom(pLocalPart)) mQuotedLocalPart = pLocalPart;
            else mQuotedLocalPart = cTools.Enquote(pLocalPart);
        }

        public cEmailAddress(string pLocalPart, string pDomain, string pDisplayName = null) : base(ZDisplayName(pDisplayName))
        {
            LocalPart = pLocalPart ?? throw new ArgumentNullException(nameof(pLocalPart));
            if (!cCharset.WSPVChar.ContainsAll(pLocalPart)) throw new ArgumentOutOfRangeException(nameof(pLocalPart));
            Domain = pDomain ?? throw new ArgumentNullException(nameof(pDomain));
            if (!cTools.IsDomain(pDomain)) throw new ArgumentOutOfRangeException(nameof(pDomain));
            if (cTools.IsDotAtom(pLocalPart)) mQuotedLocalPart = pLocalPart;
            else mQuotedLocalPart = cTools.Enquote(pLocalPart);
        }

        public cEmailAddress(MailAddress pAddress) : base(ZDisplayName(pAddress))
        {
            if (pAddress == null) throw new ArgumentNullException(nameof(pAddress));
            if (pAddress.User == null) throw new cAddressFormException(pAddress);
            LocalPart = ZRemoveQuoting(pAddress.User);
            if (!cCharset.WSPVChar.ContainsAll(LocalPart)) throw new cAddressFormException(pAddress);
            if (pAddress.Host == null) throw new cAddressFormException(pAddress);
            if (!cTools.IsDomain(pAddress.Host)) throw new cAddressFormException(pAddress);
            Domain = pAddress.Host;
            if (cTools.IsDotAtom(LocalPart)) mQuotedLocalPart = LocalPart;
            else mQuotedLocalPart = cTools.Enquote(LocalPart);
        }

        private static cCulturedString ZDisplayName(string pDisplayName)
        {
            if (pDisplayName == null) return null;
            if (!cCharset.WSPVChar.ContainsAll(pDisplayName)) throw new ArgumentOutOfRangeException(nameof(pDisplayName));
            return new cCulturedString(pDisplayName);
        }

        private static cCulturedString ZDisplayName(MailAddress pAddress)
        {
            if (pAddress == null) throw new ArgumentNullException(nameof(pAddress));
            if (!cCharset.WSPVChar.ContainsAll(pAddress.DisplayName)) throw new cAddressFormException(pAddress);
            return new cCulturedString(pAddress.DisplayName);
        }

        private string ZRemoveQuoting(string pString)
        {
            if (pString == null) return null;

            bool lDidNothing = true;
            bool lInQuotedString = false;
            bool lJustHadABackslash = false;
            var lBuilder = new StringBuilder();

            foreach (var lChar in pString)
            {
                if (lJustHadABackslash)
                {
                    lBuilder.Append(lChar);
                    lJustHadABackslash = false;
                    continue;
                }

                if (lChar == '"')
                {
                    lInQuotedString = !lInQuotedString;
                    lDidNothing = false;
                    continue;
                }

                if (lInQuotedString && lChar == '\\')
                {
                    lJustHadABackslash = true;
                    continue;
                }

                lBuilder.Append(lChar);
            }

            if (lInQuotedString || lDidNothing) return pString;

            return lBuilder.ToString();
        }

        public override string DisplayText
        {
            get
            {
                if (DisplayName != null) return DisplayName.ToString();
                return DisplayAddress;
            }
        }

        public string Address => $"{mQuotedLocalPart}@{Domain}";
        public string DisplayAddress => $"{mQuotedLocalPart}@{cTools.GetDisplayHost(Domain)}";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cEmailAddress pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public override bool Equals(cAddress pObject) => this == pObject as cEmailAddress;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cEmailAddress;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + LocalPart.GetHashCode();
                lHash = lHash * 23 + Domain.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cEmailAddress)}({LocalPart},{Domain},{DisplayName})";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cEmailAddress pA, cEmailAddress pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;
            return pA.LocalPart == pB.LocalPart && pA.Domain == pB.Domain;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cEmailAddress pA, cEmailAddress pB) => !(pA == pB);

        public static implicit operator MailAddress(cEmailAddress pAddress)
        {
            if (pAddress == null) return null;
            return new MailAddress(pAddress.Address, pAddress.DisplayName?.ToString());
        }

        public static implicit operator cEmailAddress(MailAddress pAddress)
        {
            if (pAddress == null) return null;
            return new cEmailAddress(pAddress);
        }
    }

    /// <summary>
    /// Represents a named group of email addresses.
    /// </summary>
    /// <seealso cref="cAddresses"/>
    public sealed class cGroupAddress : cAddress, IEquatable<cGroupAddress>
    {
        /// <summary>
        /// The collection of group members. May be empty.
        /// </summary>
        public readonly ReadOnlyCollection<cEmailAddress> EmailAddresses;

        internal cGroupAddress(cCulturedString pDisplayName, List<cEmailAddress> pAddresses) : base(pDisplayName)
        {
            if (pDisplayName == null) throw new ArgumentNullException(nameof(pDisplayName));
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));
            EmailAddresses = pAddresses.AsReadOnly();
            ;?; // need to sort the addresses => emailaddress needs to be comparable
        }

        public cGroupAddress(string pDisplayName, IEnumerable<cEmailAddress> pAddresses) : base(ZDisplayName(pDisplayName))
        {
            var lEmailAddresses = new List<cEmailAddress>();

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lEmailAddresses.Add(lAddress);
            }

            ;?; // need to sort the addresses => emailaddress needs to be comparable
            EmailAddresses = lEmailAddresses.AsReadOnly();
        }

        private static cCulturedString ZDisplayName(string pDisplayName)
        {
            if (pDisplayName == null) throw new ArgumentNullException(nameof(pDisplayName));
            if (!cCharset.WSPVChar.ContainsAll(pDisplayName)) throw new ArgumentOutOfRangeException(nameof(pDisplayName));
            return new cCulturedString(pDisplayName);
        }

        public cGroupAddress(string pDisplayName, params cEmailAddress[] pAddresses) : this(pDisplayName, pAddresses as IEnumerable<cEmailAddress>) { }

        public cGroupAddress(string pDisplayName, params MailAddress[] pAddresses) : this(pDisplayName, cTools.MailAddressesToEmailAddresses(pAddresses)) { }

        public cGroupAddress(string pDisplayName, IEnumerable<MailAddress> pAddresses) : this(pDisplayName, cTools.MailAddressesToEmailAddresses(pAddresses)) { }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cGroupAddress pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public override bool Equals(cAddress pObject) => this == pObject as cGroupAddress;

        /// <inheritdoc />
        public override bool Equals(object pObject) => this == pObject as cGroupAddress;

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + DisplayName.GetHashCode();

                ;?; // the order isn't important
                foreach (var lAddress in Addresses) lHash = lHash * 23 + lAddress.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cGroupAddress));
            lBuilder.Append(DisplayName);
            foreach (var lAddress in Addresses) lBuilder.Append(lAddress);
            return lBuilder.ToString();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cGroupAddress pA, cGroupAddress pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            if (pA.DisplayName?.ToString() != pB.DisplayName?.ToString()) return false;


            if (pA.Addresses.Count != pB.Addresses.Count) return false;

            ;?; // the order isn't important
            for (int i = 0; i < pA.Addresses.Count; i++) if (pA.Addresses[i] != pB.Addresses[i]) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cGroupAddress pA, cGroupAddress pB) => !(pA == pB);
    }
}