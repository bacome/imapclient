using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace work.bacome.mailclient
{
    internal class cMailMessageList : List<MailMessage>
    {
        public cMailMessageList() { }
        public cMailMessageList(IEnumerable<MailMessage> pMessages) : base(pMessages) { }

        public override string ToString() => $"{nameof(cMailMessageList)}({Count})";

        public static cMailMessageList FromMessage(MailMessage pMailMessage)
        {
            if (pMailMessage == null) throw new ArgumentNullException(nameof(pMailMessage));
            var lResult = new cMailMessageList();
            lResult.Add(pMailMessage);
            return lResult;
        }

        public static cMailMessageList FromMessages(IEnumerable<MailMessage> pMailMessages)
        {
            if (pMailMessages == null) throw new ArgumentNullException(nameof(pMailMessages));

            var lResult = new cMailMessageList();

            foreach (var lMailMessage in pMailMessages)
            {
                if (lMailMessage == null) throw new ArgumentOutOfRangeException(nameof(pMailMessages), kArgumentOutOfRangeExceptionMessage.ContainsNulls);
                lResult.Add(lMailMessage);
            }

            return lResult;
        }
    }
}