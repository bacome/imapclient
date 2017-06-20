using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public abstract class cAddress
    {
        public readonly cCulturedString DisplayName;
        public cAddress(cCulturedString pDisplayName) { DisplayName = pDisplayName; }
    }

    public class cEmailAddress : cAddress
    {
        public readonly string Address; // raw address - not converted 
        public readonly string DisplayAddress; // display address - host name converted from punycode (rfc 3492) [currently not implemented] // TODO

        public cEmailAddress(cCulturedString pDisplayName, string pAddress, string pDisplayAddress) : base(pDisplayName)
        {
            Address = pAddress;
            DisplayAddress = pDisplayAddress;
        }

        public override string ToString() => $"{nameof(cEmailAddress)}({DisplayName},{Address},{DisplayAddress})";
    }

    public class cGroupAddress : cAddress
    {
        public readonly ReadOnlyCollection<cEmailAddress> Addresses;

        public cGroupAddress(cCulturedString pDisplayName, IList<cEmailAddress> pAddresses) : base(pDisplayName)
        {
            Addresses = new ReadOnlyCollection<cEmailAddress>(pAddresses);
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cGroupAddress));
            lBuilder.Append(DisplayName);
            foreach (var lAddress in Addresses) lBuilder.Append(lAddress);
            return lBuilder.ToString();
        }
    }
}