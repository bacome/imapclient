using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private fMessageProperties mDefaultMessageProperties = 0;

        public fMessageProperties DefaultMessageProperties
        {
            get => mDefaultMessageProperties;

            set
            {
                if ((value & fMessageProperties.clientdefault) != 0) throw new ArgumentOutOfRangeException(); // default can't include the default
                mDefaultMessageProperties = value;
            }
        }

        private fMessageProperties ZDefaultMessagePropertiesAdd(fMessageProperties pProperties)
        {
            if ((pProperties & fMessageProperties.clientdefault) == 0) return pProperties;
            return pProperties | mDefaultMessageProperties;
        }
    }
}