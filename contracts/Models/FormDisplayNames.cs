namespace Jewel.JPMS.Models;

/// <summary>The words the forms' states read as, on the office's screens and over the connector alike.</summary>
public static class FormDisplayNames
{
    /// <summary>What an office dialog's choice reads before one is made, so nothing is chosen by default.</summary>
    public const string NotYetChosen = "choose…";

    public static string DisplayName(this FormLinkState state) => state switch
    {
        FormLinkState.NotOpened => "Not opened",
        FormLinkState.Opened => "Opened",
        FormLinkState.Done => "Done",
        FormLinkState.Expired => "Expired",
        _ => "Replaced"
    };

    public static string DisplayName(this FormSubmissionStatus status) => status switch
    {
        FormSubmissionStatus.New => "New",
        FormSubmissionStatus.InProgress => "In progress",
        FormSubmissionStatus.Handled => "Handled",
        _ => "Destroyed"
    };

    public static string DisplayName(this TrainingStanding standing) => standing switch
    {
        TrainingStanding.Valid => "Valid",
        TrainingStanding.ExpiringSoon => "Expiring soon",
        TrainingStanding.Expired => "Expired",
        TrainingStanding.NoExpiry => "No expiry",
        _ => "Ended"
    };

    public static string DisplayName(this WorkstationActionState state) => state switch
    {
        WorkstationActionState.Open => "Open",
        WorkstationActionState.Fixed => "Fixed",
        _ => "Accepted"
    };

    public static string DisplayName(this RightToWorkRoute route) => route switch
    {
        RightToWorkRoute.OriginalPassportSeen => "Original passport seen",
        RightToWorkRoute.ShareCode => "Share code (Home Office online)",
        _ => "IDSP report"
    };

    public static string DisplayName(this RightToWorkOutcome outcome) => outcome switch
    {
        RightToWorkOutcome.Pass => "Pass",
        RightToWorkOutcome.QueryDoNotStart => "Query (do not start)",
        _ => "Fail"
    };

    public static string DisplayName(this RightToWorkSeenVia seenVia) =>
        seenVia == RightToWorkSeenVia.InPerson ? "In person" : "Video call";

    public static string DisplayName(this Engagement engagement) =>
        engagement == Engagement.Employee ? "Employee" : "Individual subcontractor";

}
