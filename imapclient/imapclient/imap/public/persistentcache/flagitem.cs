using System;

namespace work.bacome.imapclient
{
    public interface iFlagItem
    {
        fMessageCacheAttributes Attributes { get; }
        cModSeqFlags ModSeqFlags { get; set; }
    }
}
