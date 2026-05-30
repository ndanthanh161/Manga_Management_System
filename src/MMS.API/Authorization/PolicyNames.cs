namespace MMS.API.Authorization
{
    public static class PolicyNames
    {
        public const string CanReviewSubmission = nameof(CanReviewSubmission);
        public const string CanAnnotateChapter = nameof(CanAnnotateChapter);
        public const string CanVoteInSession = nameof(CanVoteInSession);
        public const string CanManageSeries = nameof(CanManageSeries);
        public const string IsBoardMember = nameof(IsBoardMember);
        public const string IsTantou = nameof(IsTantou);

        // Mangaka
        public const string IsMangaka = nameof(IsMangaka);
        public const string CanAssignAssistantTasks = nameof(CanAssignAssistantTasks);
        public const string CanReviewAssistantTasks = nameof(CanReviewAssistantTasks);
        public const string CanViewMySeriesMetrics = nameof(CanViewMySeriesMetrics);

        // Assistant
        public const string CanAccessAssignedTask = nameof(CanAccessAssignedTask);
        public const string CanSubmitTaskCompletion = nameof(CanSubmitTaskCompletion);
        public const string CanViewPersonalEarnings = nameof(CanViewPersonalEarnings);

        // Tantou
        public const string CanAnnotateManuscript = nameof(CanAnnotateManuscript);
        public const string CanManageSeriesMetrics = nameof(CanManageSeriesMetrics);
        public const string CanTrackStudioRealtime = nameof(CanTrackStudioRealtime);

        // Board
        public const string CanInputReaderVotes = nameof(CanInputReaderVotes);

        // Task (shared Mangaka + Assistant)
        public const string CanUpdateTaskStatus = nameof(CanUpdateTaskStatus);
    }
}
