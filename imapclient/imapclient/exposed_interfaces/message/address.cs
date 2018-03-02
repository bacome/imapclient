using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents an address on an email; either an individual <see cref="cEmailAddress"/> or a <see cref="cGroupAddress"/>.
    /// </summary>
    /// <seealso cref="cAddresses"/>
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
    /// <seealso cref="cMessage.BCC"/>
    /// <seealso cref="cMessage.CC"/>
    /// <seealso cref="cMessage.From"/>
    /// <seealso cref="cMessage.ReplyTo"/>
    /// <seealso cref="cMessage.Sender"/>
    /// <seealso cref="cMessage.To"/>
    /// <seealso cref="cEnvelope.BCC"/>
    /// <seealso cref="cEnvelope.CC"/>
    /// <seealso cref="cEnvelope.From"/>
    /// <seealso cref="cEnvelope.ReplyTo"/>
    /// <seealso cref="cEnvelope.Sender"/>
    /// <seealso cref="cEnvelope.To"/>
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

        internal cAddresses(string pSortString, string pDisplaySortString, IList<cAddress> pAddresses) : base(pAddresses)
        {
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
        /// <summary>
        /// The display name for the address, may be <see langword="null"/>.
        /// </summary>
        new public readonly cCulturedString DisplayName;

        /// <summary>
        /// The address (local-part@domain) with the domain as sent.
        /// </summary>
        public readonly string Address;

        /// <summary>
        /// The address (local-part@domain) with a punycode decoded domain.
        /// </summary>
        public readonly string DisplayAddress;

        internal cEmailAddress(cCulturedString pDisplayName, string pAddress, string pDisplayAddress) : base(pDisplayName ?? new cCulturedString(pDisplayAddress))
        {
            DisplayName = pDisplayName;
            Address = pAddress ?? throw new ArgumentNullException(nameof(pAddress));
            DisplayAddress = pDisplayAddress ?? throw new ArgumentNullException(nameof(pDisplayAddress));
        }

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cEmailAddress)}({DisplayName},{Address},{DisplayAddress})";
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