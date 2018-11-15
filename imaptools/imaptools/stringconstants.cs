using System;

namespace work.bacome.imapinternals
{
    public static class kInvalidOperationExceptionMessage
    {
        public const string IsInvalid = "is invalid";

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

    public static class kArgumentOutOfRangeExceptionMessage
    {
        public const string IsInvalid = "is invalid";
        public const string MailboxMustBeSelected = "mailbox must be selected";

        public const string ContainsNulls = "contains nulls";
        public const string ContainsMixedMessageCaches = "contains mixed message caches";
        public const string ContainsMixedUIDValidities = "contains mixed uidvalidities";
        public const string HasNoContent = "has no content";
        public const string AdjacencyProblem = "adjacency problem";
    }

    public static class kMailMessageFormExceptionMessage
    {
        public const string StreamNotSeekable = "stream not seekable";
        public const string MessageDataStreamUnknownFormatAndLength = "message data stream unknown format and length";
    }

    public static class kUnexpectedIMAPServerActionMessage
    {
        public const string CountShouldOnlyGoUp = "count should only go up";
        public const string UnexpectedContinuationRequest = "unexpected continuation request";
        public const string OKAndError = "ok status response combined with error response code";
        public const string IdleCompletedBeforeDoneSent = "idle completed before done sent";
        public const string ResultsNotReceived = "results not received";
        public const string ByeNotReceived = "bye not received";
        public const string SelectResponseOrderProblem = "select response order problem";
        public const string VanishedWithNoUIDValidity = "vanished with no uidvalidity";
    }

    public static class kDeserialiseExceptionMessage
    {
        public const string IsNull = "is null";
        public const string IsInvalid = "is invalid";
        public const string IsInconsistent = "is inconsistent";
        public const string IsEmpty = "is empty";

        public const string ContainsNulls = "contains nulls";
        public const string ContainsInvalidValues = "contains invalid values";
        public const string ContainsDuplicates = "contains duplicates";
        public const string IncorrectSequence = "incorrect sequence";
    }
}