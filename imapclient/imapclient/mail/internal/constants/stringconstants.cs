﻿using System;

namespace work.bacome.mailclient
{
    internal static class kInvalidOperationExceptionMessage
    {
        public const string AlreadyConnected = "already connected";
        public const string AlreadyEnabled = "already enabled";
        public const string AlreadyChallenged = "already challenged";
        public const string AlreadyEmitted = "already emitted";
        public const string AlreadySet = "already set";

        public const string NotUnconnected = "not unconnected";
        public const string NotConnecting = "not connecting";
        public const string NotConnected = "not connected";
        public const string NotUnauthenticated = "not unauthenticated";
        public const string NotAuthenticated = "not authenticated";
        public const string NotEnabled = "not enabled";
        public const string NotSelected = "not selected";
        public const string NotSelectedForUpdate = "not selected for update";
        public const string MailboxNotSelected = "mailbox not selected";

        public const string NotPopulatedWithData = "not populated with data";

        public const string NoMailboxHierarchy = "no mailbox hierarchy";
        public const string CondStoreNotInUse = "condstore not in use";
    }

    internal static class kArgumentOutOfRangeExceptionMessage
    {
        public const string IsInvalid = "is invalid";
        public const string MailboxMustBeSelected = "mailbox must be selected";

        public const string ContainsNulls = "contains nulls";
        public const string ContainsMixedMessageCaches = "contains mixed message caches";
        public const string ContainsMixedUIDValidities = "contains mixed uidvalidities";
        public const string HasNoContent = "has no content";
    }

    internal static class kMailMessageFormExceptionMessage
    {
        public const string MixedEncodings = "mixed encodings";
        public const string StreamNotSeekable = "stream not seekable";
        public const string MessageDataStreamUnknownLength = "message data stream unknown length";
        public const string ReplyToNotSupported = "reply-to not supported";
    }
}