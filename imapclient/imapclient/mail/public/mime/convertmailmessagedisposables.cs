using System;
using System.Collections.Generic;
using System.IO;
using work.bacome.imapclient;

namespace work.bacome.mailclient
{
    public sealed class cConvertMailMessageDisposables : IDisposable
    {
        private readonly List<string> mTempFileNames = new List<string>();
        private readonly List<Stream> mStreams = new List<Stream>();

        public cConvertMailMessageDisposables() { }

        internal string GetTempFileName()
        {
            string lTempFileName = Path.GetTempFileName();
            mTempFileNames.Add(lTempFileName);
            return lTempFileName;
        }

        internal cIMAPMessageDataStream GetMessageDataStream(cIMAPMessageDataStream pStream)
        {
            cIMAPMessageDataStream lMessageDataStream = new cIMAPMessageDataStream(pStream);
            mStreams.Add(lMessageDataStream);
            return lMessageDataStream;
        }

        internal MemoryStream GetMemoryStream(byte[] pBuffer)
        {
            MemoryStream lMemoryStream = new MemoryStream(pBuffer);
            mStreams.Add(lMemoryStream);
            return lMemoryStream;
        }

        public void Dispose()
        {
            foreach (var lTempFileName in mTempFileNames)
            {
                try { File.Delete(lTempFileName); }
                catch { }
            }

            foreach (var lStream in mStreams)
            {
                try { lStream.Dispose(); }
                catch { }
            }
        }
    }
}