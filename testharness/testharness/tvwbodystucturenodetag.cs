using System;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness
{
    public class cTVWBodyStructureNodeTag
    {
        public readonly cBodyPart BodyPart;
        public readonly bool Header; 

        public cTVWBodyStructureNodeTag(cBodyPart pBodyPart, bool pHeader)
        {
            BodyPart = pBodyPart;
            Header = pHeader;
        }
    }
}