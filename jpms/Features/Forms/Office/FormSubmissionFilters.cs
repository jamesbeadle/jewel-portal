namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// How the Received screen narrows the forms that came in: where the office has got to with them and —
/// from a person's or company's folder — theirs alone.
/// </summary>
public static class FormSubmissionFilters
{
    public const string ToHandle = "to-handle";
    public const string Handled = "handled";
    public const string Everything = "all";

    public static IReadOnlyList<TabItem> StatusChips(IReadOnlyList<FormSubmission>? submissions) => new[]
    {
        new TabItem(ToHandle, "To handle", Count: submissions?.Count(IsToHandle)),
        new TabItem(Handled, "Handled"),
        new TabItem(Everything, "Everything")
    };

    public static IReadOnlyList<FormSubmission> Apply(IEnumerable<FormSubmission> submissions, string status, string? formFolderId) =>
        submissions
            .Where(submission => IsIn(submission, status))
            .Where(submission => formFolderId is null || submission.FormFolderId == formFolderId)
            .ToList();

    public static string LinkText(FormSubmission submission) => submission switch
    {
        { FormPackId: not null } => "New starter pack",
        { IsVerifiedLink: true } => "One-time link",
        _ => "Open address"
    };

    private static bool IsToHandle(FormSubmission submission) =>
        submission.Status is FormSubmissionStatus.New or FormSubmissionStatus.InProgress;

    private static bool IsIn(FormSubmission submission, string status) => status switch
    {
        ToHandle => IsToHandle(submission),
        Handled => submission.Status == FormSubmissionStatus.Handled,
        _ => true
    };
}
