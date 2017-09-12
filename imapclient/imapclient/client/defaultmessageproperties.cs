using System;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private cMessageProperties mDefaultMessageProperties = cMessageProperties.None;

        public cMessageProperties DefaultMessageProperties
        {
            get => mDefaultMessageProperties;
            set => mDefaultMessageProperties = value ?? throw new ArgumentNullException();
        }
    }
}