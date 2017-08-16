using System;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private struct sCommandDetails
            {
                public readonly cCommandTag Tag;
                public readonly ReadOnlyCollection<cCommandPart> Parts;
                public readonly cCommandDisposables Disposables;
                public readonly cReferences References;
                public readonly cCommandHook Hook;

                public sCommandDetails(cCommandTag pTag, ReadOnlyCollection<cCommandPart> pParts, cCommandDisposables pDisposables, cReferences pReferences, cCommandHook pHook)
                {
                    Tag = pTag ?? throw new ArgumentNullException(nameof(pTag));
                    Parts = pParts ?? throw new ArgumentNullException(nameof(pParts));
                    Disposables = pDisposables ?? throw new ArgumentNullException(nameof(pDisposables));
                    References = pReferences ?? throw new ArgumentNullException(nameof(pReferences));
                    Hook = pHook ?? throw new ArgumentNullException(nameof(pHook));
                }

                public override string ToString() => $"{nameof(sCommandDetails)}({Tag})";
            }
        }
    }
}