namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// How the Received screen narrows the forms that came in: where the office has got to with them,
/// which Jewel company they were sent for, and — from a person's or company's folder — theirs alone.
/// </summary>
public static class FormSubmissionFilters
{
    public const string ToHandle = "to-handle";
    public const string Handled = "handled";
    public const string Everything = "all";
    public const string BothCompanies = "both";

    public static IReadOnlyList<TabItem> StatusChips(IReadOnlyList<FormSubmission>? submissions) => new[]
    {
        new TabItem(ToHandle, "To handle", Count: submissions?.Count(IsToHandle)),
        new TabItem(Handled, "Handled"),
        new TabItem(Everything, "Everything")
    };

    public static IReadOnlyList<TabItem> CompanyChips { get; } = JewelCompanies.All
        .Select(company => new TabItem(company.Code, company.ShortName))
        .Prepend(new TabItem(BothCompanies, "Both companies"))
        .ToList();

    public static IReadOnlyList<FormSubmission> Apply(
        IEnumerable<FormSubmission> submissions, string status, string company, string? formFolderId) =>
        submissions
            .Where(submission => IsIn(submission, status) && IsFor(submission, company))
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

    private static bool IsFor(FormSubmission submission, string company) =>
        company == BothCompanies || JewelCompanies.For(submission.Company).Code == company;
}
