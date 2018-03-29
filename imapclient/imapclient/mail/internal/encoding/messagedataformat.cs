using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal static class cMessageDataFormat
    {
        public static fMessageDataFormat ParseMessage(Stream pStream)
        {
            // examines the stream parsing for format
            //  the hardest one is utf8headers: I have to inspect the headers stopping at the blank line, but remembering the boundaries and adding any cte I find (if I find a cte=binary/8bit this is turned on)
            //  then I have to read forward to any cached boundary and
            //   if this is an end-boundary then remove the boundary from the set,
            //   if this is a boundary read the headers again (til the blank line, adding any boundaries) 

            // doesn't seem that hard



            pStream.Position = 0;


        }
    }
}