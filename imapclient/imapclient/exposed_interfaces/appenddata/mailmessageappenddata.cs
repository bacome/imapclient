using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mail;
using System.Text;

namespace work.bacome.imapclient
{
    public sealed class cMailMessageAppendData : cMultiPartAppendDataBase, IDisposable
    {
        private readonly ReadOnlyCollection<cAppendDataPart> mParts;
        private readonly TempFileCollection mTempFileCollection = new TempFileCollection(); // the conversion may require the use of temporary files for quoted-printable encoding and for streams that can't seek 

        public cMailMessageAppendData(MailMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null) : base(pFlags, pReceived, pEncoding)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            List<cAppendDataPart> lParts = new List<cAppendDataPart>();

            bool lHasContent = false;

            // todo ... convert pMessage to lParts
            throw new NotImplementedException();
            // if can't convert ... throw new ArgumentOutOfRangeException(nameof(pMessage)); 

            if (!lHasContent) throw new ArgumentOutOfRangeException(nameof(pMessage));

            mParts = lParts.AsReadOnly();
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