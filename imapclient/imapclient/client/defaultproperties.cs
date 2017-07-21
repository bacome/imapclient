using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private fMailboxProperties mDefaultMailboxProperties = 0;
        private fMessageProperties mDefaultMessageProperties = 0;

        public fMailboxProperties DefaultMailboxProperties
        {
            get => mDefaultMailboxProperties;

            set
            {
                if ((value & fMailboxProperties.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mDefaultMailboxProperties = value;
            }
        }

        public fMessageProperties DefaultMessageProperties
        {
            get => mDefaultMessageProperties;

            set
            {
                if ((value & fMessageProperties.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mDefaultMessageProperties = value;
            }
        }

        private fMailboxProperties ZDefaultMailboxPropertiesAdd(fMailboxProperties pProperties)
        {
            if ((pProperties & fMailboxProperties.clientdefault) == 0) return pProperties;
            return pProperties | mDefaultMailboxProperties;
        }

        private fMessageProperties ZDefaultMessagePropertiesAdd(fMessageProperties pProperties)
        {
            if ((pProperties & fMessageProperties.clientdefault) == 0) return pProperties;
            return pProperties | mDefaultMessageProperties;
        }
    }
}