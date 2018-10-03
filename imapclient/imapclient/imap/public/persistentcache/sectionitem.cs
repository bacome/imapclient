using System;
using System.IO;

namespace work.bacome.imapclient
{
    public abstract class cSectionItem
    {
        public readonly Stream ReadWriteStream;

        public cSectionItem(Stream pReadWriteStream)
        {
            ReadWriteStream = pReadWriteStream ?? throw new ArgumentNullException(nameof(pReadWriteStream));
            ;?; // check the stream
        }

        public abstract bool TryGetReadStream(out Stream rStream);
    }
}
