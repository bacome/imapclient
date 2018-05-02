using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public sealed class cAccountSpecificSectionCache : cSectionCache, IDisposable
    {
        private bool mDisposed = false;

        private readonly object mLock = new object();

        public readonly cAccountId AccountId;
        public readonly string Directory;
        public readonly int FileCountBudget;
        public readonly long ByteCountBudget;
        public readonly int WaitAfterTrim;

        private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource();
        private readonly cReleaser mBackgroundReleaser;
        private readonly Task mBackgroundTask = null;

        private int mFileCount = 0;
        private long mByteCount = 0;
        private bool mWorthTryingTrim = false;

        ;?;
        public cAccountSpecificSectionCache(cAccountId pAccountId, string pDirectory, int pFileCountBudget, long pByteCountBudget, int pWaitAfterTrim, cBatchSizerConfiguration pWriteConfiguration) : base(pWriteConfiguration, false)
        {
            AccountId = pAccountId ?? throw new ArgumentNullException(nameof(pAccountId));
            Directory = pDirectory ?? throw new ArgumentNullException(nameof(pDirectory));

            string lInstanceName = $"{nameof(cAccountSpecificSectionCache)}({pAccountId},{pDirectory})";

            var lContext = cMailClient.Trace.NewRoot(lInstanceName);

            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));
            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pWaitAfterTrim < 0) throw new ArgumentOutOfRangeException(nameof(pWaitAfterTrim));

            FileCountBudget = pFileCountBudget;
            ByteCountBudget = pByteCountBudget;
            WaitAfterTrim = pWaitAfterTrim;

            DirectoryInfo lDI = new DirectoryInfo(Directory);

            if (!lDI.Exists) throw new cUnexpectedSectionCacheItemFormat(pDirectory, "not exists");

            int lFileCount = 0;
            bool lAccountFile = false;
            var lSCIs = new List<string>();

            foreach (var lFSI in lDI.EnumerateFileSystemInfos())
            {
                lFileCount++;

                if ((lFSI.Attributes & FileAttributes.Directory) != 0) throw new cUnexpectedSectionCacheItemFormat(pDirectory, "contains directories");

                if (lFSI.Name == "account")
                {
                    string lHost;
                    string lCredentialId;

                    using (var lStream = new FileStream(lFSI.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        lHost = ZReadString(lStream, lFSI.FullName);
                        lCredentialId = ZReadString(lStream, lFSI.FullName);
                        if (lStream.ReadByte() != -1) throw new cUnexpectedSectionCacheItemFormat(lFSI.FullName);
                    }

                    if (lHost != pAccountId.Host || lCredentialId != pAccountId.CredentialId.ToString()) throw new cUnexpectedSectionCacheItemFormat(pDirectory, "wrong account");

                    lAccountFile = true;
                }
                else if (lFSI.Name.EndsWith(".sck"))
                {
                    // read it and validate

                    string lMailboxPath;
                    string lDelimiter;
                    string lUIDValidity;
                    string lUID;
                    string lPart;
                    string lTestPart;
                    string lNameCount;
                    List<string> lNames = new List<string>();
                    string lDecodingRequired;

                    using (var lStream = new FileStream(lFSI.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        ZReadString(lFSI, lStream, out lMailboxPath);
                        ZReadString(lFSI, lStream, out lCredentialId);
                        ZCheckAtEnd(lFSI);


                    }


                    cMailboxName lMailboxName;

                    try
                    {
                            = new cMailboxName(lMailboxNamePath, lMailboxNameDelimiter);
                    }
                    catch (Exception e)
                    {
                        throw new cUnexpectedSectionCacheItemFormat();
                    }



                    cSectionCachePersistentKey lKey = new cSectionCachePersistentKey(pAccountId, lMailboxName...);

                    // build dictionary pk -> filename
                }
                else if (lFSI.Name.EndsWith(".sci")) lSCIs.Add(lFSI.Name.Substring(1, lFSI.Name.Length - 4));
                else throw new cUnexpectedSectionCacheItemFormat(pDirectory, "contains unrecognised file types");
            }

            if (lFileCount == 0)
            {
                using (var lStream = new FileStream(Directory + "\\account", FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    ZWriteString(lStream, pAccountId.Host);
                    ZWriteString(lStream, pAccountId.CredentialId.ToString());
                }
            }
            else if (!lAccountFile) throw new cUnexpectedSectionCacheItemFormat(pDirectory, "no account file");

            // delete all sci s with no sck (these are npks left over i'd guess)
            // (delete all sck s with no sci) // maybe scks with no scis could be ignored at open time [probs yes]

            mBackgroundReleaser = new cReleaser(lInstanceName, mBackgroundCancellationTokenSource.Token);
            mBackgroundTask = ZBackgroundTaskAsync(lContext);
        }

        private void ZWriteString(Stream pStream, string pString)
        {
            var lBuffer = Encoding.UTF8.GetBytes(pString.Length.ToString() + ":" + pString);
            pStream.Write(lBuffer, 0, lBuffer.Length);
        }

        private string ZReadString(Stream pStream, string pFileName)
        {
            // read length

            int lLength = 0;
            bool lReadAByte = false;

            while (true)
            {
                var lByte = pStream.ReadByte();
                if (lByte == 58) break;
                if (lByte < 48 || lByte > 57) throw new cUnexpectedSectionCacheItemFormat(pFileName);
                lReadAByte = true;
                lLength = lLength * 10 + (lByte - 48);
            }

            if (!lReadAByte) throw new cUnexpectedSectionCacheItemFormat(pFileName);

            if (lLength == 0) return string.Empty;

            byte[] lBytes = new byte[lLength];

            for (int i = 0; i < lLength; i++)
            {
                var lByte = pStream.ReadByte();
                if (lByte == -1) throw new cUnexpectedSectionCacheItemFormat(pFileName);
                lBytes[i] = (byte)lByte;
            }

            return Encoding.UTF8.GetString(lBytes);
        }

        private 

        private eMultiPartBodySubTypeCode ZInitialise()
        {


            // load a list of all filenames that look like pk files

            // delete all files that look like npk files


        }


        protected override cItem GetNewItem(cTrace.cContext pParentContext) => new cAccountSpecificItem(this);




    }
}