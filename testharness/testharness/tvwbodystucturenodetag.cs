using System;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness
{
    public class cTVWBodyStructureNodeTag
    {
        public readonly cMessage Message;
        public readonly cBodyPart BodyPart;
        public readonly cSection Section;

        public cTVWBodyStructureNodeTag(cMessage pMessage)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
            BodyPart = null;
            Section = null;
        }

        public cTVWBodyStructureNodeTag(cMessage pMessage, cBodyPart pBodyPart)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
            BodyPart = pBodyPart ?? throw new ArgumentNullException(nameof(pBodyPart));
            Section = null;
        }

        public cTVWBodyStructureNodeTag(cMessage pMessage, cSection pSection)
        {
            Message = pMessage ?? throw new ArgumentNullException(nameof(pMessage));
            BodyPart = null;
            Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
        }
    }
}