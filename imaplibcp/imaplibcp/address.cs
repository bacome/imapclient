using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mail;
using System.Runtime.Serialization;
using work.bacome.imapinternals;
using work.bacome.imapsupport;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an address on an email; either an individual <see cref="cEmailAddress"/> or a <see cref="cGroupAddress"/>.
    /// </summary>
    [Serializable]
    public abstract class cAddress : iCanComposeHeaderFieldValue, IEquatable<cAddress>
    {
        /// <summary>
        /// The display name for the address, may be <see langword="null"/> for a <see cref="cEmailAddress"/>.
        /// </summary>
        public readonly cCulturedString DisplayName;

        internal cAddress(cCulturedString pDisplayName)
        {
            DisplayName = pDisplayName;
        }

        /// <summary>
        /// The display text for the address. 
        /// Will be the <see cref="DisplayName"/> except for an <see cref="cEmailAddress"/> without a display name when it will be the <see cref="cEmailAddress.Address"/>.
        /// </summary>
        public virtual string DisplayText => DisplayName;

        /// <inheritdoc cref="iCanComposeHeaderFieldValue.CanComposeHeaderFieldValue(bool)"/>
        public abstract bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed);

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public abstract bool Equals(cAddress pObject);

        /// <inheritdoc />
        public abstract override bool Equals(object pObject);

        /// <inheritdoc cref="cAPIDocumentationTemplate.GetHashCode"/>
        public abstract override int GetHashCode();

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equality"/>
        public static bool operator ==(cAddress pA, cAddress pB)
        {
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.Equals(pB);
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cAddress pA, cAddress pB) => !(pA == pB);
    }

    /// <summary>
    /// An immutable collection of addresses.
    /// </summary>
    [Serializable]
    public class cAddresses : iCanComposeHeaderFieldValue, IReadOnlyList<cAddress>, IEquatable<cAddresses>
    {
        [NonSerialized]
        private string mSortString;

        [NonSerialized]
        private string mDisplaySortString;

        private readonly ReadOnlyCollection<cAddress> mAddresses;

        /// <summary>
        /// Initialises a new instance with the specified addresses.
        /// </summary>
        /// <param name="pAddresses"></param>
        public cAddresses(IEnumerable<cAddress> pAddresses)
        {
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            var lAddresses = new List<cAddress>();

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lAddresses.Add(lAddress);
            }

            mAddresses = lAddresses.AsReadOnly();
            ZFinishConstruct();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (mAddresses == null) throw new cDeserialiseException(nameof(cAddresses), nameof(mAddresses), kDeserialiseExceptionMessage.IsNull);
            foreach (var lAddress in mAddresses) if (lAddress == null) throw new cDeserialiseException(nameof(cAddresses), nameof(mAddresses), kDeserialiseExceptionMessage.ContainsNulls);
            ZFinishConstruct();
        }

        private void ZFinishConstruct()
        {
            if (mAddresses.Count == 0)
            {
                mSortString = string.Empty;
                mDisplaySortString = string.Empty;
                return;
            }

            if (mAddresses[0] is cGroupAddress lGroup)
            {
                mSortString = lGroup.DisplayName;
                mDisplaySortString = mSortString;
                return;
            }

            if (mAddresses[0] is cEmailAddress lEMail)
            {
                mSortString = lEMail.LocalPart;

                if (lEMail.DisplayName != null) mDisplaySortString = lEMail.DisplayName;
                else mDisplaySortString = lEMail.LocalPart + "@" + lEMail.Domain;

                return;
            }

            throw new cInternalErrorException(nameof(cAddresses), nameof(ZFinishConstruct));
        }

        /// <inheritdoc cref="iCanComposeHeaderFieldValue.CanComposeHeaderFieldValue(bool)"/>
        public virtual bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed)
        {
            foreach (var lAddress in mAddresses) if (!lAddress.CanComposeHeaderFieldValue(pUTF8HeadersAllowed)) return false;
            return true;
        }

        /// <summary>
        /// The RFC 5256 sort string for the collection of addresses.
        /// </summary>
        public string SortString => mSortString;

        /// <summary>
        /// The RFC 5957 display sort string for the collection of addresses.
        /// </summary>
        public string DisplaySortString => mDisplaySortString;

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
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
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
    [Serializable]
    public sealed class cEmailAddress : cAddress, IEquatable<cEmailAddress>, IComparable<cEmailAddress>
    {
        /// <summary>
        /// The local part of the address.
        /// </summary>
        public readonly string LocalPart;

        /// <summary>
        /// The domain of the address.
        /// </summary>
        public readonly string Domain;

        [NonSerialized]
        private string mQuotedLocalPart;

        private cEmailAddress(string pLocalPart, string pDomain, cCulturedString pDisplayName) : base(pDisplayName)
        {
            LocalPart = pLocalPart ?? throw new ArgumentNullException(nameof(pLocalPart));
            Domain = pDomain ?? throw new ArgumentNullException(nameof(pDomain));
            ZFinishConstruct();
        }

        /// <summary>
        /// Initialises a new instance with the specified values which do not have to be valid for use when composing an RFC 5322 header field value.
        /// </summary>
        /// <remarks>
        /// Intended for internal use by the library, this constructor accepts values that have been pre-processed by IMAP.
        /// </remarks>
        /// <param name="pMailboxBytes"></param>
        /// <param name="pHostBytes"></param>
        /// <param name="pNameBytes"></param>
        public cEmailAddress(IList<byte> pMailboxBytes, IList<byte> pHostBytes, IList<byte> pNameBytes) : base(ZDisplayName(pNameBytes))
        {
            LocalPart = cTools.UTF8BytesToString(pMailboxBytes);
            Domain = cTools.UTF8BytesToString(pHostBytes);
            ZFinishConstruct();
        }

        /// <summary>
        /// Initialises a new instance with the specified values which must be valid for use when composing an RFC 5322 header field value.
        /// </summary>
        /// <param name="pLocalPart"></param>
        /// <param name="pDomain"></param>
        /// <param name="pDisplayName"></param>
        public cEmailAddress(string pLocalPart, string pDomain, string pDisplayName = null) : base(ZDisplayName(pDisplayName))
        {
            if (pLocalPart == null) throw new ArgumentNullException(nameof(pLocalPart));
            if (!cMailValidation.TryParseLocalPart(pLocalPart, out LocalPart)) throw new ArgumentOutOfRangeException(nameof(pLocalPart));

            if (pDomain == null) throw new ArgumentNullException(nameof(pDomain));
            if (!cMailValidation.TryParseDomain(pDomain, out Domain)) throw new ArgumentOutOfRangeException(nameof(pDomain));

            ZFinishConstruct();
        }

        /// <summary>
        /// Initialises a new instance with the specified value which must be valid for use when composing an RFC 5322 header field value.
        /// </summary>
        public cEmailAddress(MailAddress pAddress) : base(ZDisplayName(pAddress))
        {
            if (pAddress == null) throw new ArgumentNullException(nameof(pAddress));
            if (pAddress.User == null || pAddress.Host == null) throw new ArgumentOutOfRangeException(nameof(pAddress));

            if (!cMailValidation.TryParseLocalPart(pAddress.User, out LocalPart)) throw new ArgumentOutOfRangeException(nameof(pAddress));
            if (!cMailValidation.TryParseDomain(pAddress.Host, out Domain)) throw new ArgumentOutOfRangeException(nameof(pAddress));

            ZFinishConstruct();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (LocalPart == null) throw new cDeserialiseException(nameof(cEmailAddress), nameof(LocalPart), kDeserialiseExceptionMessage.IsNull);
            if (Domain == null) throw new cDeserialiseException(nameof(cEmailAddress), nameof(Domain), kDeserialiseExceptionMessage.IsNull);
            ZFinishConstruct();
        }

        private void ZFinishConstruct()
        {
            if (cMailValidation.IsDotAtomText(LocalPart)) mQuotedLocalPart = LocalPart;
            else mQuotedLocalPart = cTools.Enquote(LocalPart);
        }

        private static cCulturedString ZDisplayName(IList<byte> pNameBytes)
        {
            if (pNameBytes == null) return null;
            return new cCulturedString(pNameBytes);
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
            if (pAddress.DisplayName == null) return null;
            if (!cCharset.WSPVChar.ContainsAll(pAddress.DisplayName)) throw new ArgumentOutOfRangeException(nameof(pAddress));
            return new cCulturedString(pAddress.DisplayName);
        }

        /** <inheritdoc/> **/
        public override string DisplayText
        {
            get
            {
                if (DisplayName != null) return DisplayName;
                return Address;
            }
        }

        /** <inheritdoc/> **/
        public override bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed)
        {
            if (DisplayName != null && !DisplayName.CanComposeHeaderFieldValue(pUTF8HeadersAllowed)) return false; // display name can include encoded words, so as long as there are no illegal characters the value is fine
            if (!cMailValidation.TryParseLocalPart(mQuotedLocalPart, out var lLocalPart)) return false;
            if (!pUTF8HeadersAllowed && cTools.ContainsNonASCII(lLocalPart)) return false; // the local part can't include encoded words
            if (!cMailValidation.TryParseDomain(Domain, out _)) return false; // the domain can always be punycoded, so if it is valid there is no need to check for unicode
            return true;
        }

        /// <summary>
        /// Gets the address in the form localpart@domain.
        /// </summary>
        public string Address => $"{mQuotedLocalPart}@{Domain}";

        /// <inheritdoc cref="cAPIDocumentationTemplate.Equals(object)"/>
        public bool Equals(cEmailAddress pObject) => this == pObject;

        /// <inheritdoc cref="cAPIDocumentationTemplate.CompareTo"/>
        public int CompareTo(cEmailAddress pOther)
        {
            if (pOther == null) return 1;
            var lCompareTo = cTools.CompareToNullableFields(DisplayName, pOther.DisplayName);
            if (lCompareTo != 0) return lCompareTo;
            lCompareTo = DisplayName.CompareTo(pOther.DisplayName);
            if (lCompareTo != 0) return lCompareTo;
            lCompareTo = LocalPart.CompareTo(pOther.LocalPart);
            if (lCompareTo != 0) return lCompareTo;
            return Domain.CompareTo(pOther.Domain);
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
                if (DisplayName != null) lHash = lHash * 23 + DisplayName.GetHashCode();
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
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            return pA.DisplayName == pB.DisplayName && pA.LocalPart == pB.LocalPart && pA.Domain == pB.Domain;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cEmailAddress pA, cEmailAddress pB) => !(pA == pB);

        /// <summary>
        /// Constructs a new <see cref="cEmailAddress"/> from the specified <see cref="MailAddress"/> if possible, returning <see langword="true"/> if successful.
        /// </summary>
        /// <param name="pAddress">Must be valid for use when composing an RFC 5322 header field value.</param>
        /// <param name="rEmailAddress"></param>
        /// <returns></returns>
        public static bool TryConstruct(MailAddress pAddress, out cEmailAddress rEmailAddress)
        {
            if (pAddress == null) throw new ArgumentNullException(nameof(pAddress));

            if (pAddress.User == null || pAddress.Host == null) { rEmailAddress = null; return false; }
            if (!cMailValidation.TryParseLocalPart(pAddress.User, out var lLocalPart) || !cMailValidation.TryParseDomain(pAddress.Host, out var lDomain)) { rEmailAddress = null; return false; }

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

        /// <summary>
        /// Returns a new <see cref="MailAddress"/> initialised with values from the specified <see cref="cEmailAddress"/>.
        /// </summary>
        /// <param name="pAddress"></param>
        public static implicit operator MailAddress(cEmailAddress pAddress)
        {
            if (pAddress == null) return null;
            return new MailAddress(pAddress.Address, pAddress.DisplayName?.ToString());
        }
    }

    /// <summary>
    /// Represents a named group of email addresses.
    /// </summary>
    [Serializable]
    public sealed class cGroupAddress : cAddress, IEquatable<cGroupAddress>
    {
        /// <summary>
        /// The sorted collection of group members. May be empty.
        /// </summary>
        public readonly ReadOnlyCollection<cEmailAddress> EmailAddresses;

        /// <summary>
        /// Initialises a new instance with the specified values. Note that the content of the <paramref name="pDisplayName"/> is not checked in this constructor.
        /// </summary>
        /// <param name="pDisplayName"></param>
        /// <param name="pAddresses"></param>
        public cGroupAddress(cCulturedString pDisplayName, IEnumerable<cEmailAddress> pAddresses) : base(pDisplayName)
        {
            if (pDisplayName == null) throw new ArgumentNullException(nameof(pDisplayName));
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            var lEmailAddresses = new List<cEmailAddress>();

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lEmailAddresses.Add(lAddress);
            }

            lEmailAddresses.Sort(); // for the hashcode and == implementations

            EmailAddresses = lEmailAddresses.AsReadOnly();
        }

        /// <summary>
        /// Initialises a new instance with the specified values.
        /// </summary>
        /// <param name="pDisplayName">Must be valid for use when composing an RFC 5322 header field value.</param>
        /// <param name="pAddresses"></param>
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

            EmailAddresses = lEmailAddresses.AsReadOnly();
        }

        /// <summary>
        /// Initialises a new instance with the specified values.
        /// </summary>
        /// <param name="pDisplayName">Must be valid for use when composing an RFC 5322 header field value.</param>
        /// <param name="pAddresses">Each address must be valid for use when composing an RFC 5322 header field value.</param>
        public cGroupAddress(string pDisplayName, IEnumerable<MailAddress> pAddresses) : base(ZDisplayName(pDisplayName))
        {
            if (pAddresses == null) throw new ArgumentNullException(nameof(pAddresses));

            var lEmailAddresses = new List<cEmailAddress>();

            foreach (var lAddress in pAddresses)
            {
                if (lAddress == null) throw new ArgumentOutOfRangeException(nameof(pAddresses), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                if (!cEmailAddress.TryConstruct(lAddress, out var lEmailAddress)) throw new ArgumentOutOfRangeException(nameof(pAddresses), new cAddressFormException(lAddress));
                lEmailAddresses.Add(lEmailAddress);
            }

            lEmailAddresses.Sort(); // for the hashcode and == implementations

            EmailAddresses = lEmailAddresses.AsReadOnly();
        }

        [OnDeserialized]
        private void OnDeserialised(StreamingContext pSC)
        {
            if (DisplayName == null) throw new cDeserialiseException(nameof(cGroupAddress), nameof(DisplayName), kDeserialiseExceptionMessage.IsNull);
            if (EmailAddresses == null) throw new cDeserialiseException(nameof(cGroupAddress), nameof(EmailAddresses), kDeserialiseExceptionMessage.IsNull);

            cEmailAddress lLastEMailAddress = null;

            foreach (var lEMailAddress in EmailAddresses)
            {
                if (lEMailAddress == null) throw new cDeserialiseException(nameof(cGroupAddress), nameof(EmailAddresses), kDeserialiseExceptionMessage.ContainsNulls);
                if (lEMailAddress.CompareTo(lLastEMailAddress) == -1) throw new cDeserialiseException(nameof(cGroupAddress), nameof(EmailAddresses), kDeserialiseExceptionMessage.IncorrectSequence);
            }
        }

        private static cCulturedString ZDisplayName(string pDisplayName)
        {
            if (pDisplayName == null) throw new ArgumentNullException(nameof(pDisplayName));
            if (!cCharset.WSPVChar.ContainsAll(pDisplayName)) throw new ArgumentOutOfRangeException(nameof(pDisplayName));
            return new cCulturedString(pDisplayName);
        }

        /** <inheritdoc/> **/
        public override bool CanComposeHeaderFieldValue(bool pUTF8HeadersAllowed)
        {
            if (!DisplayName.CanComposeHeaderFieldValue(pUTF8HeadersAllowed)) return false; // display name can include encoded words, so as long as there are no illegal characters the value is fine
            foreach (var lEMailAddress in EmailAddresses) if (!lEMailAddress.CanComposeHeaderFieldValue(pUTF8HeadersAllowed)) return false;
            return true;
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
                if (DisplayName != null) lHash = lHash * 23 + DisplayName.GetHashCode();
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
            var lReferenceEquals = cTools.EqualsReferenceEquals(pA, pB);
            if (lReferenceEquals != null) return lReferenceEquals.Value;
            if (pA.DisplayName != pB.DisplayName) return false;
            if (pA.EmailAddresses.Count != pB.EmailAddresses.Count) return false;
            for (int i = 0; i < pA.EmailAddresses.Count; i++) if (pA.EmailAddresses[i] != pB.EmailAddresses[i]) return false;
            return true;
        }

        /// <inheritdoc cref="cAPIDocumentationTemplate.Inequality"/>
        public static bool operator !=(cGroupAddress pA, cGroupAddress pB) => !(pA == pB);
    }
}