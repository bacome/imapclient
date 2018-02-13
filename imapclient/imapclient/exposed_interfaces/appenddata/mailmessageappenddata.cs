using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net.Mail;
using System.Text;

namespace work.bacome.imapclient
{
    public sealed class cMailMessageAppendData : cMultiPartAppendDataBase, IDisposable
    {
        private readonly string mBoundaryBase;
        private readonly ReadOnlyCollection<cAppendDataPart> mParts;
        private readonly TempFileCollection mTempFileCollection = new TempFileCollection(); // the conversion may require the use of temporary files for quoted-printable encoding and for streams that can't seek 

        public cMailMessageAppendData(MailMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null) : base(pFlags, pReceived, pEncoding)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            mBoundaryBase = Guid.NewGuid().ToString();

            List<cAppendDataPart> lParts = new List<cAppendDataPart>();

            if (pMessage.Bcc.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("bcc", pMessage.Bcc));
            if (pMessage.CC.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("cc", pMessage.CC));
            if (pMessage.From != null) lParts.Add(new cHeaderFieldAppendDataPart("from", pMessage.From));
            lParts.Add(new cHeaderFieldAppendDataPart("importance", pMessage.Priority.ToString()));
            if (pMessage.ReplyToList.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("reply-to", pMessage.ReplyToList));
            if (pMessage.Sender != null) lParts.Add(new cHeaderFieldAppendDataPart("sender", pMessage.Sender));
            if (pMessage.Subject != null) lParts.Add(new cHeaderFieldAppendDataPart("subject", pMessage.Subject));
            if (pMessage.To.Count != 0) lParts.Add(new cHeaderFieldAppendDataPart("to", pMessage.To));

            var lDate = pMessage.Headers["date"];
            if (lDate != null) lParts.Add(new cHeaderFieldAppendDataPart("date", lDate));

            ZAdd(pMessage.Headers, lParts);

            // mime headers

            lParts.Add(new cHeaderFieldAppendDataPart("mime-version", "1.0"));

            string lBoundary0 = ZBoundary(0);

            lParts.Add(
                new cHeaderFieldAppendDataPart(
                    "content-type",
                    new cHeaderFieldValuePart[] 
                    {
                        "multipart/mixed",
                        new cHeaderFieldMIMEParameter("boundary", lBoundary0)
                    }));

            lParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit", "8bit"));

            // blank line

            lParts.Add("\r\nThis is a multi-part message in MIME format.\r\n");

            // text part
            //


            // MORE TO DO ... ;?;



















            // todo ... convert pMessage to lParts
            throw new NotImplementedException();
            // if can't convert ... throw new ArgumentOutOfRangeException(nameof(pMessage)); 

            mParts = lParts.AsReadOnly();
        }

        private void ZAdd(NameValueCollection pHeaders, List<cAppendDataPart> pParts)
        {
            for (int i = 0; i < pHeaders.Count; i++)
            {
                var lName = pHeaders.GetKey(i);
                var lValues = pHeaders.GetValues(i);

                // this is the list from mailmessage.headers documentation
                if (!lName.Equals("bcc", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("cc", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("content-id", StringComparison.InvariantCultureIgnoreCase) &&
                    !lName.Equals("content-location", StringComparison.InvariantCultureIgnoreCase) &&
                    !lName.Equals("content-transfer-encoding", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("content-type", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("date", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("from", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("importance", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("mime-version", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("priority", StringComparison.InvariantCultureIgnoreCase) && // -
                    !lName.Equals("reply-to", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("sender", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("subject", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("to", StringComparison.InvariantCultureIgnoreCase) && // k
                    !lName.Equals("x-priority", StringComparison.InvariantCultureIgnoreCase) // -
                    )
                {
                    foreach (var lValue in lValues) pParts.Add(new cHeaderFieldAppendDataPart(lName, lValue));
                }
            }
        }

        private string ZBoundary()
        {
            ;?;
        }

        public override ReadOnlyCollection<cAppendDataPart> Parts => mParts;

        public void Dispose()
        {
            // it should be noted that if the files are still in use by the library this will cause a problem
            //  (this could happen if the append is cancelled)
            mTempFileCollection.Delete();
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cMailMessageAppendData));
            lBuilder.Append(Flags);
            lBuilder.Append(Received);
            if (Encoding != null) lBuilder.Append(Encoding.WebName);
            foreach (var lPart in Parts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }
}