using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public sealed class cMailMessageAppendData : cMultiPartAppendDataBase
    {
        private static readonly cBatchSizerConfiguration kMemoryStreamReadConfiguration = new cBatchSizerConfiguration(10000, 10000, 1, 10000); // 10k chunks

        private readonly ReadOnlyCollection<cAppendDataPart> mParts;
        private readonly TempFileCollection mTempFileCollection; // the conversion may require the use of temporary files for quoted-printable encoding and for streams that can't seek 

        private cMailMessageAppendData(cStorableFlags pFlags, DateTime? pReceived, Encoding pEncoding, ReadOnlyCollection<cAppendDataPart> pParts, TempFileCollection pTempFileCollection) : base(pFlags, pReceived, pEncoding)
        {
            mParts = pParts;
            mTempFileCollection = pTempFileCollection;
        }

        public cMailMessageAppendData(MailMessage pMessage, cStorableFlags pFlags = null, DateTime? pReceived = null, Encoding pEncoding = null) : base(pFlags, pReceived, ZEncoding(pMessage, pEncoding))
        {
            mTempFileCollection = new TempFileCollection();

            try
            {
                mParts = ZConvertAsync(pMessage, mTempFileCollection, new cConfiguration(false, -1, CancellationToken.None, null)).Result;
            }
            catch
            {
                mTempFileCollection.Delete();
                throw;
            }
        }

        public override ReadOnlyCollection<cAppendDataPart> Parts => mParts;

        protected override void Dispose(bool pDisposing)
        {
            if (pDisposing)
            {
                // it should be noted that if the files are still in use by the library this will cause a problem
                //  (this could happen if the append is cancelled)
                if (mTempFileCollection != null) mTempFileCollection.Delete();
            }
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

        public static async Task<cMailMessageAppendData> ConstructAsync(MailMessage pMessage, cStorableFlags pFlags, DateTime? pReceived, Encoding pEncoding, int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrement = null)
        {
            var lEncoding = ZEncoding(pMessage, pEncoding);

            ReadOnlyCollection<cAppendDataPart> lParts;
            TempFileCollection lTempFileCollection = new TempFileCollection();

            try
            {
                lParts = await ZConvertAsync(pMessage, lTempFileCollection, new cConfiguration(true, pTimeout, pCancellationToken, pIncrement)).ConfigureAwait(false);
            }
            catch
            {
                lTempFileCollection.Delete();
                throw;
            }

            return new cMailMessageAppendData(pFlags, pReceived, lEncoding, lParts, lTempFileCollection);
        }

        private static Encoding ZEncoding(MailMessage pMessage, Encoding pEncoding)
        {
            // while it is possible to specify an encoding when creating an address, it is not possible to find out what the specified encoding was (MailAddress has no 'Encoding' property)
            //  otherwise I'd check the address encodings also

            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));

            Encoding lEncoding = pMessage.HeadersEncoding ?? pMessage.SubjectEncoding ?? pEncoding;

            if (pMessage.HeadersEncoding != null && !pMessage.HeadersEncoding.Equals(lEncoding) ||
                pMessage.SubjectEncoding != null && !pMessage.SubjectEncoding.Equals(lEncoding) ||
                pEncoding != null && !pEncoding.Equals(lEncoding)) throw new ArgumentOutOfRangeException(nameof(pMessage), kArgumentOutOfRangeExceptionMessage.ContainsMixedEncodings);

            return lEncoding;
        }

        private class cConfiguration
        {
            public readonly bool Async;
            private readonly int mTimeout;
            private readonly Stopwatch mStopwatch;
            public readonly CancellationToken CancellationToken;
            public readonly Action<int> Increment;

            public cConfiguration(bool pAsync, int pTimeout, CancellationToken pCancellationToken, Action<int> pIncrement)
            {
                Async = pAsync;
                if (pTimeout < -1) throw new ArgumentOutOfRangeException(nameof(pTimeout));
                mTimeout = pTimeout;
                if (pTimeout == -1) mStopwatch = null;
                else mStopwatch = Stopwatch.StartNew();
                CancellationToken = pCancellationToken;
                Increment = pIncrement;
            }

            public int Timeout
            {
                get
                {
                    if (mStopwatch == null) return System.Threading.Timeout.Infinite;
                    long lElapsed = mStopwatch.ElapsedMilliseconds;
                    if (mTimeout > lElapsed) return (int)(mTimeout - lElapsed);
                    return 0;
                }
            }

            public override string ToString()
            {
                if (mStopwatch == null) return $"{nameof(cConfiguration)}({Async},{CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled})";
                return $"{nameof(cConfiguration)}({Async},{mStopwatch.ElapsedMilliseconds}/{mTimeout},{CancellationToken.IsCancellationRequested}/{CancellationToken.CanBeCanceled})";
            }
        }

        private static async Task<ReadOnlyCollection<cAppendDataPart>> ZConvertAsync(MailMessage pMessage, TempFileCollection pTempFileCollection, cConfiguration pConfiguration)
        {
            if (pMessage == null) throw new ArgumentNullException(nameof(pMessage));
            if (pTempFileCollection == null) throw new ArgumentNullException(nameof(pTempFileCollection));

            var lParts = new List<cAppendDataPart>();

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

            ZConvertCustomHeaders(pMessage.Headers, lParts);

            // mime headers

            string lBoundaryBase = Guid.NewGuid().ToString();

            lParts.Add(new cHeaderFieldAppendDataPart("mime-version", "1.0"));

            lParts.Add(
                new cHeaderFieldAppendDataPart(
                    "content-type",
                    new cHeaderFieldValuePart[]
                    {
                        "multipart/mixed",
                        new cHeaderFieldMIMEParameter("boundary", ZConvertBoundary(0, lBoundaryBase, out var lDelimiter, out var lCloseDelimiter))
                    }));

            lParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "7bit", "8bit"));

            // blank line and preamble
            lParts.Add("\r\nThis is a multi-part message in MIME format.");

            // opening delimiter
            lParts.Add(lDelimiter);

            if (pMessage.AlternateViews.Count > 0)
            {
                // multipart/alternative
                throw new NotImplementedException();
            }
            else await ZConvertPlainTextAsync(pMessage, lParts, pConfiguration).ConfigureAwait(false);

            // attachments

            if (pMessage.Attachments.Count > 0) throw new NotImplementedException();

            /*
            foreach (var lAttachment in pMessage.Attachments)
            {
                ;?;
            } */

            // close-delimiter
            lParts.Add(lCloseDelimiter);

            // done
            return lParts.AsReadOnly();
        }

        private static void ZConvertCustomHeaders(NameValueCollection pHeaders, List<cAppendDataPart> pParts)
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

        private static string ZConvertBoundary(int pPart, string pBoundaryBase, out cLiteralAppendDataPart rDelimiter, out cLiteralAppendDataPart rCloseDelimiter)
        {
            string lBoundary = $"=_{pPart}_{pBoundaryBase}";
            rDelimiter = new cLiteralAppendDataPart("\r\n--" + lBoundary + "\r\n");
            rCloseDelimiter = new cLiteralAppendDataPart("\r\n--" + lBoundary + "--\r\n");
            return lBoundary;
        }

        private static async Task ZConvertPlainTextAsync(MailMessage pMessage, List<cAppendDataPart> pParts, cConfiguration pConfiguration)
        {
            Encoding lEncoding = pMessage.BodyEncoding ?? Encoding.ASCII;

            pParts.Add(
                new cHeaderFieldAppendDataPart(
                    "content-type",
                    new cHeaderFieldValuePart[]
                    {
                        "text/plain",
                        new cHeaderFieldMIMEParameter("charset", lEncoding.WebName)
                    }));

            bool lBase64;

            using (MemoryStream lInput = new MemoryStream(lEncoding.GetBytes(pMessage.Body)), lOutput = new MemoryStream())
            {
                if (pMessage.BodyTransferEncoding == TransferEncoding.Base64) lBase64 = true;
                else
                {
                    int lQuotedPrintableLength;
                    if (pConfiguration.Async) lQuotedPrintableLength = await cQuotedPrintableEncoder.EncodeAsync(lInput, lOutput, pConfiguration.Timeout, pConfiguration.CancellationToken, pConfiguration.Increment).ConfigureAwait(false);
                    else lQuotedPrintableLength = cQuotedPrintableEncoder.Encode(lInput, lOutput, pConfiguration.Timeout, pConfiguration.Increment);

                    if (pMessage.BodyTransferEncoding == TransferEncoding.QuotedPrintable) lBase64 = false;
                    else
                    {
                        var lBase64Length = cBase64Encoder.EncodedLength(lInput.Length);

                        if (lBase64Length < lQuotedPrintableLength)
                        {
                            lBase64 = true;

                            lInput.Position = 0;
                            lOutput.Position = 0;
                            lOutput.SetLength(lBase64Length);
                        }
                        else lBase64 = false;
                    }
                }

                if (lBase64)
                {
                    pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "base64"));

                    using (var lBase64Encoder = new cBase64Encoder(lInput, kMemoryStreamReadConfiguration))
                    {
                        if (pConfiguration.Async) await lBase64Encoder.CopyToAsync(lOutput).ConfigureAwait(false);
                        else lBase64Encoder.CopyTo(lOutput);
                    }
                }
                else pParts.Add(new cHeaderFieldAppendDataPart("content-transfer-encoding", "quoted-printable"));

                pParts.Add(cAppendDataPart.CRLF);
                pParts.Add(new cLiteralAppendDataPart(new cBytes(lOutput.ToArray())));
            }
        }
    }
}