﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mail;
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
        /// <summary>
        /// The RFC 5256 sort string for the collection of addresses.
        /// </summary>
        public readonly string SortString;

        /// <summary>
        /// The RFC 5957 display sort string for the collection of addresses.
        /// </summary>
        public readonly string DisplaySortString;

        private readonly ReadOnlyCollection<cAddress> mAddresses;

        internal cAddresses(List<cAddress> pAddresses)
        {
            mAddresses = pAddresses.AsReadOnly();
            ZSortStrings(out SortString, out DisplaySortString);
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
            ZSortStrings(out SortString, out DisplaySortString);
        }

        private void ZSortStrings(out string rSortString, out string rDisplaySortString)
        {
            if (mAddresses.Count == 0)
            {
                rSortString = string.Empty;
                rDisplaySortString = string.Empty;
                return;
            }

            if (mAddresses[0] is cGroupAddress lGroup)
            {
                rSortString = lGroup.DisplayName;
                rDisplaySortString = rSortString;
                return;
            }

            if (mAddresses[0] is cEmailAddress lEMail)
            {
                rSortString = lEMail.LocalPart;

                if (lEMail.DisplayName != null) rDisplaySortString = lEMail.DisplayName;
                else rDisplaySortString = lEMail.LocalPart + "@" + lEMail.Domain;

                return;
            }

            throw new cInternalErrorException(nameof(cAddresses), nameof(ZSortStrings));
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
    public sealed class cEmailAddress : cAddress, IEquatable<cEmailAddress>, IComparable<cEmailAddress>
    {
        public readonly string LocalPart;
        public readonly string Domain;
        private readonly string mQuotedLocalPart;

        internal cEmailAddress(string pLocalPart, string pDomain, cCulturedString pDisplayName) : base(pDisplayName)
        {
            LocalPart = pLocalPart ?? throw new ArgumentNullException(nameof(pLocalPart));
            Domain = pDomain ?? throw new ArgumentNullException(nameof(pDomain));

            if (cValidation.IsDotAtomText(LocalPart)) mQuotedLocalPart = LocalPart;
            else mQuotedLocalPart = cTools.Enquote(LocalPart);
        }

        public cEmailAddress(string pLocalPart, string pDomain, string pDisplayName = null) : base(ZDisplayName(pDisplayName))
        {
            if (pLocalPart == null) throw new ArgumentNullException(nameof(pLocalPart));
            if (!cValidation.TryParseLocalPart(pLocalPart, out LocalPart)) throw new ArgumentOutOfRangeException(nameof(pLocalPart));

            if (pDomain == null) throw new ArgumentNullException(nameof(pDomain));
            if (!cValidation.TryParseDomain(pDomain, out Domain)) throw new ArgumentOutOfRangeException(nameof(pDomain));

            if (cValidation.IsDotAtomText(LocalPart)) mQuotedLocalPart = LocalPart;
            else mQuotedLocalPart = cTools.Enquote(LocalPart);
        }

        public cEmailAddress(MailAddress pAddress) : base(ZDisplayName(pAddress))
        {
            if (pAddress == null) throw new ArgumentNullException(nameof(pAddress));
            if (pAddress.User == null || pAddress.Host == null) throw new cAddressFormException(pAddress);

            if (!cValidation.TryParseLocalPart(pAddress.User, out LocalPart)) throw new cAddressFormException(pAddress);
            if (!cValidation.TryParseDomain(pAddress.Host, out Domain)) throw new cAddressFormException(pAddress);

            if (cValidation.IsDotAtomText(LocalPart)) mQuotedLocalPart = LocalPart;
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

        public override string DisplayText
        {
            get
            {
                if (DisplayName != null) return DisplayName;
                return DisplayAddress;
            }
        }

        public string Address => $"{mQuotedLocalPart}@{Domain}";
        public string DisplayAddress => $"{mQuotedLocalPart}@{cTools.GetDisplayHost(Domain)}";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cEmailAddress pObject) => this == pObject;

        public int CompareTo(cEmailAddress pOther)
        {
            if (pOther == null) return 1;
            int lCompareTo = LocalPart.CompareTo(pOther.LocalPart);
            if (lCompareTo == 0) return Domain.CompareTo(pOther.Domain);
            return lCompareTo;
        }

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

        /* TODO: remove
        public static bool TryConstruct(string pLocalPart, string pDomain, string pDisplayName, out cEmailAddress rEmailAddress)
        {
            // NOTE: just because the address is valid by this routine doesn't mean it can be included in a header
            //  if the local part is UTF8 and the client doesn't support UTF8 then the address will be unusable

            if (pLocalPart == null) throw new ArgumentNullException(nameof(pLocalPart));
            if (pDomain == null) throw new ArgumentNullException(nameof(Domain));

            if (!cValidation.TryParseLocalPart(pLocalPart, out var lLocalPart) || !cValidation.TryParseDomain(pDomain, out var lDomain)) { rEmailAddress = null; return false; }

            cCulturedString lDisplayName;

            if (pDisplayName == null) lDisplayName = null;
            else
            {
                if (!cCharset.WSPVChar.ContainsAll(pDisplayName)) { rEmailAddress = null; return false; }
                lDisplayName = new cCulturedString(pDisplayName);
            }

            rEmailAddress = new cEmailAddress(lLocalPart, lDomain, lDisplayName);
            return true;
        } */

        internal static bool TryConstruct(MailAddress pAddress, out cEmailAddress rEmailAddress)
        {
            // NOTE: just because the address is valid by this routine doesn't mean it can be included in a header
            //  if the local part is UTF8 and the client doesn't support UTF8 then the address will be unusable

            if (pAddress == null) throw new ArgumentNullException(nameof(pAddress));

            if (pAddress.User == null || pAddress.Host == null) { rEmailAddress = null; return false; }
            if (!cValidation.TryParseLocalPart(pAddress.User, out var lLocalPart) || !cValidation.TryParseDomain(pAddress.Host, out var lDomain)) { rEmailAddress = null; return false; }

            cCulturedString lDisplayName;

            if (pAddress.DisplayName == null) lDisplayName = null;
            else
            {
                if (!cCharset.WSPVChar.ContainsAll(pAddress.DisplayName)) { rEmailAddress = null; return false; }
                lDisplayName = new cCulturedString(pAddress.DisplayName);
            }

            rEmailAddress = new cEmailAddress(lLocalPart, lDomain, lDisplayName);
            return true;
        }

        public static implicit operator MailAddress(cEmailAddress pAddress)
        {
            if (pAddress == null) return null;
            return new MailAddress(pAddress.Address, pAddress.DisplayName?.ToString());
        }
    }

    /// <summary>
    /// Represents a named group of email addresses.
    /// </summary>
    /// <seealso cref="cAddresses"/>
    public sealed class cGroupAddress : cAddress, IEquatable<cGroupAddress>
    {
        private readonly string mDisplayName;

        /// <summary>
        /// The sorted collection of group members. May be empty.
        /// </summary>
        public readonly ReadOnlyCollection<cEmailAddress> EmailAddresses;

        internal cGroupAddress(cCulturedString pDisplayName, List<cEmailAddress> pAddresses) : base(pDisplayName)
        {
            if (pDisplayName == null) throw new ArgumentNullException(nameof(pDisplayName));
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));
            pAddresses.Sort(); // for the hashcode and == implementations
            mDisplayName = pDisplayName.ToString();
            EmailAddresses = pAddresses.AsReadOnly();
        }

        public cGroupAddress(string pDisplayName, IEnumerable<cEmailAddress> pAddresses) : base(ZDisplayName(pDisplayName))
        {
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            var lEmailAddresses = new List<cEmailAddress>();

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lEmailAddresses.Add(lAddress);
            }

            lEmailAddresses.Sort(); // for the hashcode and == implementations

            mDisplayName = pDisplayName;
            EmailAddresses = lEmailAddresses.AsReadOnly();
        }

        public cGroupAddress(string pDisplayName, IEnumerable<MailAddress> pAddresses) : base(ZDisplayName(pDisplayName))
        {
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            var lEmailAddresses = new List<cEmailAddress>();

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lEmailAddresses.Add(new cEmailAddress(lAddress));
            }

            lEmailAddresses.Sort(); // for the hashcode and == implementations

            mDisplayName = pDisplayName;
            EmailAddresses = lEmailAddresses.AsReadOnly();
        }

        private static cCulturedString ZDisplayName(string pDisplayName)
        {
            if (pDisplayName == null) throw new ArgumentNullException(nameof(pDisplayName));
            if (!cCharset.WSPVChar.ContainsAll(pDisplayName)) throw new ArgumentOutOfRangeException(nameof(pDisplayName));
            return new cCulturedString(pDisplayName);
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
                lHash = lHash * 23 + mDisplayName.GetHashCode();
                foreach (var lEmailAddress in EmailAddresses) lHash = lHash * 23 + lEmailAddress.GetHashCode();
                return lHash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cGroupAddress));
            lBuilder.Append(DisplayName);
            foreach (var lEmailAddress in EmailAddresses) lBuilder.Append(lEmailAddress);
            return lBuilder.ToString();
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cGroupAddress pA, cGroupAddress pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            if (pA.mDisplayName != pB.mDisplayName) return false;

            if (pA.EmailAddresses.Count != pB.EmailAddresses.Count) return false;
            for (int i = 0; i < pA.EmailAddresses.Count; i++) if (pA.EmailAddresses[i] != pB.EmailAddresses[i]) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cGroupAddress pA, cGroupAddress pB) => !(pA == pB);
    }
}