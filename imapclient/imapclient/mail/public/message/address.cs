using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mail;

namespace work.bacome.mailclient
{
    ;?; // need to dfine == etc also

    /// <summary>
    /// Represents an address on an email; either an individual <see cref="cEmailAddress"/> or a <see cref="cGroupAddress"/>.
    /// </summary>
    public abstract class cAddress
    {
        /// <summary>
        /// A display name for the address. 
        /// For an <see cref="cEmailAddress"/> without a display name, this is the <see cref="cEmailAddress.DisplayAddress"/>.
        /// </summary>
        public readonly cCulturedString DisplayName;

        internal cAddress(cCulturedString pDisplayName)
        {
            DisplayName = pDisplayName ?? throw new ArgumentNullException(nameof(pDisplayName));
        }
    }

    /// <summary>
    /// An immutable collection of email addresses.
    /// </summary>
    public class cAddresses : ReadOnlyCollection<cAddress>
    {
        /// <summary>
        /// The RFC 5256 sort string for the collection of addresses.
        /// </summary>
        public readonly string SortString;

        /// <summary>
        /// The RFC 5957 display sort string for the collection of addresses.
        /// </summary>
        public readonly string DisplaySortString;

        ;?; // the calculation of the sort and displaysort strings should be here

        public cAddresses(IEnumerable<cAddress> pAddresses) : base(pAddresses)
        {
            if (pAddresses.Count)


            SortString = pSortString ?? throw new ArgumentNullException(nameof(pSortString));
            DisplaySortString = pDisplaySortString ?? throw new ArgumentNullException(nameof(pDisplaySortString));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cAddresses));
            lBuilder.Append(SortString);
            lBuilder.Append(DisplaySortString);
            foreach (var lAddress in this) lBuilder.Append(lAddress);
            return lBuilder.ToString();
        }
    }

    /// <summary>
    /// Represents an individual email address.
    /// </summary>
    /// <seealso cref="cAddresses"/>
    public class cEmailAddress : cAddress
    {
        public readonly string LocalPart;
        public readonly string Domain;

        /// <summary>
        /// The display name for the address, may be <see langword="null"/>.
        /// </summary>
        new public readonly cCulturedString DisplayName;

        ;?; // display name has to be a phrase which means not empty string? and not WSP
        public cEmailAddress(string pLocalPart, string pDomain, string pDisplayName = null) : base(new cCulturedString(pDisplayName ?? ZDisplayAddress(pLocalPart, pDomain)))
        {
            LocalPart = pLocalPart ?? throw new ArgumentNullException(nameof(pLocalPart));
            Domain = pDomain ?? throw new ArgumentNullException(nameof(pDomain));
            if (pDisplayName == null) DisplayName = null;
            else DisplayName = base.DisplayName;
        }

        internal cEmailAddress(string pLocalPart, string pDomain, cCulturedString pDisplayName) : base(pDisplayName ?? new cCulturedString(ZDisplayAddress(pLocalPart, pDomain)))
        {
            LocalPart = pLocalPart ?? throw new ArgumentNullException(nameof(pLocalPart));
            Domain = pDomain ?? throw new ArgumentNullException(nameof(pDomain));
            DisplayName = pDisplayName;
        }

        private static string ZDisplayAddress(string pLocalPart, string pDomain) => $"{pLocalPart}@{cTools.GetDisplayHost(pDomain)}";

        public string Address => $"{LocalPart}@{Domain}";
        public string DisplayAddress => ZDisplayAddress(LocalPart, Domain);
        public MailAddress MailAddress => new MailAddress(ZDisplayAddress(LocalPart, Domain), DisplayName);

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cEmailAddress)}({LocalPart},{Domain},{DisplayName})";

        public static implicit operator MailAddress(cEmailAddress pEmailAddress)
        {
            if (pEmailAddress == null) throw new ArgumentNullException(nameof(pEmailAddress));
            return new MailAddress(ZDisplayAddress(pEmailAddress.LocalPart, pEmailAddress.Domain), pEmailAddress.DisplayName);
        }

        public static implicit operator cEmailAddress(MailAddress pMailAddress)
        {
            if (pMailAddress == null) throw new ArgumentNullException(nameof(pMailAddress));
            return new cEmailAddress(pMailAddress.User, pMailAddress.Host, pMailAddress.DisplayName);
        }
    }

    /// <summary>
    /// Represents a named group of email addresses.
    /// </summary>
    /// <seealso cref="cAddresses"/>
    public class cGroupAddress : cAddress
    {
        /// <summary>
        /// The collection of group members. May be empty.
        /// </summary>
        public readonly ReadOnlyCollection<cEmailAddress> Addresses;

        internal cGroupAddress(cCulturedString pDisplayName, IList<cEmailAddress> pAddresses) : base(pDisplayName)
        {
            Addresses = new ReadOnlyCollection<cEmailAddress>(pAddresses);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cGroupAddress));
            lBuilder.Append(DisplayName);
            foreach (var lAddress in Addresses) lBuilder.Append(lAddress);
            return lBuilder.ToString();
        }
    }
}