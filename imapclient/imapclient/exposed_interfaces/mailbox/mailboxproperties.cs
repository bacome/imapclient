using System;

namespace work.bacome.imapclient
{
    public interface iMailboxProperties
    {
        cMessageFlags Flags { get; }
        cMessageFlags PermanentFlags { get; }

        int Messages { get; }
        int? Recent { get; }
        uint? UIDNext { get; }
        uint? UIDValidity { get; }
        int? Unseen { get; }

        bool Selected { get; }
        bool SelectedForUpdate { get; }
        bool AccessReadOnly { get; }
    }
}