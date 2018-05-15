using System;
using System.IO;
using System.Text;

namespace work.bacome.imapclient
{
    internal class cNewFileGenerator
    {
        ;?; // moving
        private static readonly char[] kChars =
            new char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
                'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
                'U', 'V', 'W', 'X', 'Y', 'Z'
            };

        private readonly Random mRandom = new Random();
        private readonly string mDirectory;
        private readonly string mExtension;
        private readonly int mTries;

        public cNewFileGenerator(string pDirectory, string pExtension, int pTries = 1000)
        {
            mDirectory = Path.GetFullPath(pDirectory);
            mExtension = pExtension;
            mTries = pTries;
        }

        public cFile GetNewFile()
        {
            int lTries = 0;
            string lRandomFileName;
            string lFullName;
            Stream lStream;

            while (true)
            {
                try
                {
                    lRandomFileName = ZRandomFileName();
                    lFullName = mDirectory + "\\" + lRandomFileName + "." + mExtension;
                    lStream = new FileStream(lFullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                    break;
                }
                catch (IOException) { }

                if (++lTries == mTries) throw new IOException();
            }

            var lFileInfo = new FileInfo(lFullName);

            return new cFile(mDirectory, lRandomFileName, mExtension, lFullName, lStream, lFileInfo.CreationTimeUtc);
        }


        public class cFile
        {
            public readonly string Directory;
            public readonly string FileName; // without directory and without extension
            public readonly string Extension;
            public readonly string FullName;
            public readonly Stream Stream; // read write stream
            public readonly DateTime CreationTimeUTC;

            public cFile(string pDirectory, string pFileName, string pExtension, string pFullName, Stream pStream, DateTime pCreationTimeUTC)
            {
                Directory = pDirectory;
                FileName = pFileName;
                Extension = pExtension;
                FullName = pFullName;
                Stream = pStream;
                CreationTimeUTC = pCreationTimeUTC;
            }
        }
    }
}
