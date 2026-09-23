using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Features.Forms.Office;

namespace Jewel.JPMS.Pages;

/// <summary>
/// Every form that came in, newest first — the office's one list of what strangers sent, for both
/// Jewel companies (the JPS Dashboard's forms inbox). A restricted form is listed here like any
/// other and opens only for its readers. From a folder (?folder=) it lists that person's or
/// company's forms alone.
/// </summary>
public partial class FormsReceived
{
    [SupplyParameterFromQuery(Name = "folder")] public string? FormFolderId { get; set; }

    private IReadOnlyList<FormSubmission>? submissions;
    private bool dataFailed;
    private string status = FormSubmissionFilters.ToHandle;
    private string company = FormSubmissionFilters.BothCompanies;

    private IReadOnlyList<FormSubmission> Rows =>
        FormSubmissionFilters.Apply(submissions ?? Array.Empty<FormSubmission>(), status, company, FormFolderId);

    private IReadOnlyList<TabItem> StatusChips => FormSubmissionFilters.StatusChips(submissions);

    private string? Subtitle => submissions is null ? null : $"{Rows.Count} of {submissions.Count} forms";

    private string? FolderName =>
        FormFolderId is null ? null : submissions?.FirstOrDefault(submission => submission.FormFolderId == FormFolderId)?.FilingName;

    private string EmptyMessage => dataFailed
        ? "Couldn't load the forms — refresh to try again."
        : "Nothing here. A form sent from a link or an open address lands here the moment it is submitted.";

    protected override async Task OnInitializedAsync()
    {
        if (FormFolderId is not null) status = FormSubmissionFilters.Everything;
        var mayRead = await Entry.MayReadAsync(FormRoleSets.Office);
        if (mayRead) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try { submissions = await Queries.AskAsync(new ListFormSubmissions(FormFolderId), CancellationToken.None); }
        catch { dataFailed = true; }
    }

    private void Open(FormSubmission submission) => Nav.NavigateTo($"/forms/received/{submission.FormSubmissionId}");
}
