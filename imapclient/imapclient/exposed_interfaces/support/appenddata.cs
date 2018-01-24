using System;
using System.Collections.Generic;
using System.Text;

namespace work.bacome.imapclient.support
{
    public abstract class cLiteralAppendDataPartBase : cAppendDataPart
    {
        internal cLiteralAppendDataPartBase() { }
        internal abstract IList<byte> GetBytes(Encoding pEncoding);
    }

    public abstract class cHeaderFieldValuePart
    {
        internal abstract void GetBytes(cHeaderFieldBytes pBytes);
    }
}