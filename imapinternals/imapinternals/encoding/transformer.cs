using System;
using System.Collections.Generic;

namespace work.bacome.imapinternals
{
    public interface iTransformer
    {
        byte[] Transform(IList<byte> pInputBytes, int pOffset, int pCount);
        int GetTransformedLength(IList<byte> pInputBytes, int pOffset, int pCount);
        int BufferedInputByteCount { get; }
    }
}