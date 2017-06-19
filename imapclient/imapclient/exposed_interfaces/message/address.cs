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

        public class cEmail : cAddress
        {
            public readonly string Address; // raw address - not converted 
            public readonly string DisplayAddress; // display address - host name converted from punycode (rfc 3492) [currently not implemented] // TODO

            public cEmail(cCulturedString pDisplayName, string pAddress, string pDisplayAddress) : base(pDisplayName)
            {
                Address = pAddress;
                DisplayAddress = pDisplayAddress;
            }

            public override string ToString() => $"{nameof(cEmail)}({DisplayName},{Address},{DisplayAddress})";
        }

        public class cEmails : ReadOnlyCollection<cEmail>
        {
            public cEmails(IList<cEmail> pEmails) : base(pEmails) { }

            public override string ToString()
            {
                var lBuilder = new cListBuilder(nameof(cEmails));
                foreach (var lEmail in this) lBuilder.Append(lEmail);
                return lBuilder.ToString();
            }
        }

        public class cGroup : cAddress
        {
            public readonly cEmails Emails;

            public cGroup(cCulturedString pDisplayName, IList<cEmail> pEmails) : base(pDisplayName)
            {
                Emails = new cEmails(pEmails);
            }

            public override string ToString() => $"{nameof(cGroup)}({DisplayName},{Emails})";
        }
    }
}