using System;
using System.Text;

namespace work.bacome.imapclient.support
{
    class cListBuilder
    {
        private StringBuilder mBuilder;
        private bool mFirst = true;
        private char mRight;

        public cListBuilder(string pListName, char pLeft = '(', char pRight = ')')
        {
            mBuilder = new StringBuilder(pListName);
            mBuilder.Append(pLeft);
            mRight = pRight;
        }

        public void Append(object pObject)
        {
            ZAppendSeparator();
            if (pObject != null) mBuilder.Append(pObject.ToString());
        }

        public void Append(string pName, object pObject)
        {
            ZAppendSeparator();
            mBuilder.Append(pName);
            mBuilder.Append('=');
            if (pObject != null) mBuilder.Append(pObject.ToString());
        }

        public override string ToString()
        {
            return mBuilder.ToString() + mRight;
        }

        private void ZAppendSeparator()
        {
            if (mFirst) mFirst = false;
            else mBuilder.Append(",");
        }
    }
}
