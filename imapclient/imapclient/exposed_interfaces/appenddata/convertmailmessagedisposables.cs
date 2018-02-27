using System;
using System.Collections.Generic;
using System.IO;

namespace work.bacome.imapclient
{
    public sealed class cConvertMailMessageDisposables : IDisposable
    {
        private readonly List<string> mTempFileNames = new List<string>();
        private readonly List<cMessageDataStream> mMessageDataStreams = new List<cMessageDataStream>();

        public cConvertMailMessageDisposables() { }

        internal string GetTempFileName()
        {
            string lTempFileName = Path.GetTempFileName();
            mTempFileNames.Add(lTempFileName);
            return lTempFileName;
        }

        internal cMessageDataStream CloneMessageDataStream(cMessageDataStream pStream)
        {
            cMessageDataStream lMessageDataStream = new cMessageDataStream(pStream);
            mMessageDataStreams.Add(lMessageDataStream);
            return lMessageDataStream;
        }

        public void Dispose()
        {
            foreach (var lTempFileName in mTempFileNames)
            {
                try { File.Delete(lTempFileName); }
                catch { }
            }

            foreach (var lMessageDataStream in mMessageDataStreams)
            {
                try { lMessageDataStream.Dispose(); }
                catch { }
            }
        }
    }
}