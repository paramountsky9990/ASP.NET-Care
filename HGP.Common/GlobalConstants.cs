namespace HGP.Common
{
    public static class GlobalConstants
    {
        public enum ActivityTypes
        {
            Search = 1,
            Browse = 2,
            BrowseByLocation = 3,
            AddToCart = 4,
            RemoveFromCart = 5,
            AssetDecision = 6,
            ProcessAssetRequest = 7,
            SendPendingRequestReminder = 8,
            SendWeeklyAssetUploadSummary = 9,
            WebAppStarted = 10,
            WebAppStopped = 11,
            SubmitDraftFDorApproval = 12,
            DenyDraftAsset = 13,
            ApproveDraftAsset = 14,

        }

        public enum RequestStatusTypes
        {
            Open = 1,
            Pending,
            Approved,
            Denied,
            Completed
        }

        public enum AssetStatusTypes
        {
            Available = 1,
            Requested,
            Transferred,
            Unavailable
        }

        public enum DraftAssetStatusTypes
        {
            OpenForEditing = 1,
            SubmittedForApproval,
            SentBackForEdits,
            Approved
        }


        public enum EmailTypes
        {
            // If you add a new email, be sure to add it to the init code - \HGP.Web\Database\InitDatabase.cs
            EmailTest = 1,
            EmailValidation,
            OwnerNotification,
            ManagerNotification,
            RequestDeniedNotification,
            AssetApprovedNotification,
            WelcomeNotification,
            WelcomeNotification4AdminUser,
            ResetPasswordNotification,
            LocationNotification,
            RequestApprovedToOthers,
            LocationPendingApproval,
            AssetUploadSummary,
            PendingRequestReminder,
            RequestAssetNotAvailable,
            WishListMatchedAssets,
            ExpiringWishList,
            Header,
            Footer,
            ExpiringAssets,
            DraftAssetPendingApproval,
            DraftAssetDeniedApproval,
            DraftAssetApproved,
        }

        public enum InboxStatusTypes
        {
            Pending = 1,
            Completed,
        }

        public enum InboxItemTypes
        {
            RequestApprovalDecision = 1,
            DraftAssetApprovalDecision
        }

        public enum WishListStatusTypes
        {
            Open = 1,
            Closed, // Sucessfully matched and closed
            Expired, // Automaticall expired
            Removed // Manually removed by user
        }

        public enum MatchedAssetStatusTypes
        {
            Matched = 1, // Default : Matched-Asset is Created when a Asset is matched to a WishList
            Requested, // Matched-Asset is Requested
            Ignored // Matched-Asset is Ignored
        }

        public enum UnsubscribeTypes
        {
            ReceiveAll = 1,
            ReceiveNone
        }

    }
}