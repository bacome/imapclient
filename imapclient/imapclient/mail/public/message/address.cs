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
        ;?; // raw display name and string display name


        // note that the string is the "phrase ised" string from the culturedstring cTools.ToPhrase() removes wsp beginning and end and converts embedded to one space

            // and all comparisons are to the phrase -ised string

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
            else mQuotedLocalPart = ZAddQuoting(pLocalPart);
        }

        public cEmailAddress(string pLocalPart, string pDomain, string pDisplayName = null) : base(ZDisplayName(pDisplayName))
        {
            LocalPart = pLocalPart ?? throw new ArgumentNullException(nameof(pLocalPart));
            if (!cCharset.WSPVChar.ContainsAll(pLocalPart)) throw new ArgumentOutOfRangeException(nameof(pLocalPart));
            Domain = pDomain ?? throw new ArgumentNullException(nameof(pDomain));
            if (!cTools.IsDomain(pDomain)) throw new ArgumentOutOfRangeException(nameof(pDomain));
            if (cTools.IsDotAtom(pLocalPart)) mQuotedLocalPart = pLocalPart;
            else mQuotedLocalPart = ZAddQuoting(pLocalPart);
        }

        public cEmailAddress(MailAddress pMailAddress) : base(ZDisplayName(pMailAddress))
        {
            if (pMailAddress == null) throw new ArgumentNullException(nameof(pMailAddress));
            if (pMailAddress.User == null) throw new cAddressFormException(pMailAddress);
            LocalPart = ZRemoveQuoting(pMailAddress.User);
            if (!cCharset.WSPVChar.ContainsAll(LocalPart)) throw new cAddressFormException(pMailAddress);
            if (pMailAddress.Host == null) throw new cAddressFormException(pMailAddress);
            if (!cTools.IsDomain(pMailAddress.Host)) throw new cAddressFormException(pMailAddress);
            Domain = pMailAddress.Host;
            if (cTools.IsDotAtom(LocalPart)) mQuotedLocalPart = LocalPart;
            else mQuotedLocalPart = ZAddQuoting(LocalPart);
        }

        private static cCulturedString ZDisplayName(string pDisplayName)
        {
            if (string.IsNullOrWhiteSpace(pDisplayName)) return null;
            if (!cTools.IsValidHeaderFieldText(pDisplayName)) throw new ArgumentOutOfRangeException(nameof(pDisplayName));
            return new cCulturedString(pDisplayName);
        }

        private static cCulturedString ZDisplayName(MailAddress pMailAddress)
        {
            if (pMailAddress == null) throw new ArgumentNullException(nameof(pMailAddress));
            if (string.IsNullOrWhiteSpace(pMailAddress.DisplayName)) return null;
            if (!cTools.IsValidHeaderFieldText(pMailAddress.DisplayName)) throw new cAddressFormException(pMailAddress);
            return new cCulturedString(pMailAddress.DisplayName);
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

        private string ZAddQuoting(string pString)
        {

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

        public static implicit operator MailAddress(cEmailAddress pEmailAddress)
        {
            if (pEmailAddress == null) return null;
            return new MailAddress(pEmailAddress.Address, pEmailAddress.DisplayName);
        }

        public static implicit operator cEmailAddress(MailAddress pMailAddress)
        {
            if (pMailAddress == null) return null;
            return new cEmailAddress(pMailAddress);
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

        internal cGroupAddress(cCulturedString pDisplayName, List<cEmailAddress> pEmailAddresses) : base(pDisplayName)
        {
            if (pDisplayName == null) throw new ArgumentNullException(nameof(pDisplayName));
            if (pEmailAddresses == null) throw new ArgumentNullException(nameof(pEmailAddresses));
            EmailAddresses = pEmailAddresses.AsReadOnly();
        }

        public cGroupAddress(string pDisplayName, IEnumerable<cEmailAddress> pEmailAddresses) : base(ZDisplayName(pDisplayName))
        {
            var lEmailAddresses = new List<cEmailAddress>();

            foreach (var lEmailAddress in pEmailAddresses)
            {
                if (lEmailAddress == null) throw new ArgumentOutOfRangeException(nameof(pEmailAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lEmailAddresses.Add(lEmailAddress);
            }

            EmailAddresses = lEmailAddresses.AsReadOnly();
        }

        public cGroupAddress(string pDisplayName, params cEmailAddress[] pEmailAddresses) : this(pDisplayName, pEmailAddresses as IEnumerable<cEmailAddress>) { }

        public cGroupAddress(string pDisplayName, params MailAddress[] pMailAddresses) : this(pDisplayName, cTools. (pAddresses)) { }

        public cGroupAddress(string pDisplayName, IEnumerable<MailAddress> pAddresses) : this(pDisplayName, ZEmailAddresses(pAddresses)) { }

        private static cCulturedString ZDisplayName(string pDisplayName)
        {
            if (pDisplayName == null) throw new ArgumentNullException(nameof(pDisplayName));
            if (string.IsNullOrWhiteSpace(pDisplayName)) throw new ArgumentOutOfRangeException(nameof(pDisplayName));
            if (!cTools.IsValidHeaderFieldText(pDisplayName)) throw new ArgumentOutOfRangeException(nameof(pDisplayName));
            return new cCulturedString(pDisplayName);
        }

        private static List<cEmailAddress> ZEmailAddresses(IEnumerable<MailAddress> pAddresses)
        {
            ;?;
        }

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

            if (pA.DisplayName != pB.DisplayName) return false;
            if (pA.Addresses.Count != pB.Addresses.Count) return false;
            for (int i = 0; i < pA.Addresses.Count; i++) if (pA.Addresses[i] != pB.Addresses[i]) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cGroupAddress pA, cGroupAddress pB) => !(pA == pB);
    }
}