using System;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Contains named message-flag contants.
    /// </summary>
    public static class kMessageFlag
    {
        /**<summary>\*</summary>*/
        public const string CreateNewIsPossible = @"\*";
        /**<summary>\Recent</summary>*/
        public const string Recent = @"\Recent";

        /**<summary>\Answered</summary>*/
        public const string Answered = @"\Answered";
        /**<summary>\Flagged</summary>*/
        public const string Flagged = @"\Flagged";
        /**<summary>\Deleted</summary>*/
        public const string Deleted = @"\Deleted";
        /**<summary>\Seen</summary>*/
        public const string Seen = @"\Seen";
        /**<summary>\Draft</summary>*/
        public const string Draft = @"\Draft";

        // rfc 5788/ 5550
        /**<summary>$Forwarded</summary>*/
        public const string Forwarded = "$Forwarded";
        /**<summary>$SubmitPending</summary>*/
        public const string SubmitPending = "$SubmitPending";
        /**<summary>$Submitted</summary>*/
        public const string Submitted = "$Submitted";
    }
}