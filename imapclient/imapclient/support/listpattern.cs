using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private class cListPattern
        {
            public readonly string ListMailbox;
            public readonly char? Delimiter;
            public readonly cMailboxNamePattern MailboxNamePattern;

            public cListPattern(string pListMailbox, char? pDelimiter, cMailboxNamePattern pMailboxnamePattern)
            {
                ListMailbox = pListMailbox ?? throw new ArgumentNullException(nameof(pListMailbox));
                Delimiter = pDelimiter;
                MailboxNamePattern = pMailboxnamePattern ?? throw new ArgumentNullException(nameof(pMailboxnamePattern));
            }

            public override string ToString() => $"{nameof(cListPattern)}({ListMailbox},{Delimiter},{MailboxNamePattern})";
        }

        private class cListPatterns : List<cListPattern>
        {
            public cListPatterns() { }

            public override string ToString()
            {
                var lBuilder = new cListBuilder(nameof(cListPatterns));
                foreach (var lPattern in this) lBuilder.Append(lPattern);
                return lBuilder.ToString();
            }
        }
    }
}