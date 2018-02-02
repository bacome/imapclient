using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    internal class cHeaderFieldValueBuilder : List<cHeaderFieldValuePart>
    {
        public cHeaderFieldValueBuilder() : base() { }

        public void AddAddrSpec(string pLocalPart, string pDomain)
        {
            // local part can be a dot-atom or a quoted string
            //  if UTF8 is in localpart utf8 better be on - I won't check as there is nothing I can do with the UTF8 if utf8 isn't on

        }

        public void AddNameAddr(string pDisplayName, string pLocalPart, string pDomain)
        {

        }

        public void BeginGroup(string pDisplayName)
        {
            ;?;
            ;?; // begins mailbox list
        }

        public void EndGroup()
        {
            ;?; // ends mailbox list
        }

        public void BeginMailboxList()
        {
            ;?; // groups aren't allowed
        }

        public void EndMailboxList()
        {

        }

        public void BeginAddressList()
        {
            // groups are allowed
        }

        public void EndAddressList()
        {

        }

        public void AddMsgId(string pIDLeft, string pIDRight)
        {

        }

        public void BeginPhraseList()
        {

        }

        public void AddPhrase(string pText)
        {

        }

        public void EndPhraseList()
        {

        }

        //;?; // helper methods here ...
    }


    public class cMailboxListHeaderFieldValueBuilder : IEnumerable<cHeaderFieldValuePart>
    {
        // must have one member
    }

    public class cGroupHeaderFieldValueBuilder : IEnumerable<cHeaderFieldValuePart>
    {
        // need have no members

        private string mDisplayName;

        public void AddAddrSpec(string pLocalPart, string pDomain)
        {

        }

        public void AddNameAddr(string pDisplayName, string pLocalPart, string pDomain)
        {

        }
    }

    public class cAddressListHeaderFieldValueBuilder : IEnumerable<cHeaderFieldValuePart>
    {
        // must have one member (but that member could be an empty group

        //  begin group/ end group : if no end group then when enumerating it should spit the dummy => not enumerable: special sub-class
    }


}