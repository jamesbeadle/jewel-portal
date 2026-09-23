using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Features.Forms.Office;

namespace Jewel.JPMS.Pages;

/// <summary>
/// One form that came in, read in the form's own order with every file under the question that asked
/// for it (the dashboard's office overlay) — and the thing the office does with this kind of form,
/// from accepting a certificate to recording a licence check. Health answers stay hidden until someone
/// who may see them reveals them, and every reveal is on the audit trail.
/// </summary>
public partial class FormSubmissionDetail
{
    [Parameter] public string FormSubmissionId { get; set; } = "";

    private readonly FormWrite write = new();
    private FormSubmissionView? view;
    private IReadOnlyDictionary<string, string>? revealed;
    private FormSubmissionReading? reading;
    private string? problem;
    private bool isRevealing;

    private string Title => view is { } opened ? FormCatalogue.TitleOf(opened.Submission.FormSlug) : "Form";

    private string FolderAddress => $"/forms?folder={view?.Submission.FormFolderId}";

    private bool IsDestroyed => view?.Submission.Status == FormSubmissionStatus.Destroyed;

    private IReadOnlyList<DropdownMenu.Item> StatusMoves => new[] { FormSubmissionStatus.New, FormSubmissionStatus.InProgress, FormSubmissionStatus.Handled }
        .Select(status => new DropdownMenu.Item(status.DisplayName(), EventCallback.Factory.Create(this, () => MoveAsync(status)),
            Selected: status == view?.Submission.Status))
        .ToList();

    protected override async Task OnInitializedAsync()
    {
        var mayRead = await Entry.MayReadAsync(FormRoleSets.AnyReader);
        if (mayRead) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try { view = await Queries.AskAsync(new OpenFormSubmission(FormSubmissionId), CancellationToken.None); }
        catch { problem = "This form could not be opened. It may be kept in a store your role does not read, or it no longer exists."; }
        reading = view is null ? null : new FormSubmissionReading(view, revealed);
    }

    private async Task MoveAsync(FormSubmissionStatus status)
    {
        var isMoved = await write.RunAsync(() => Commands.SendAsync(new SetFormSubmissionStatus(FormSubmissionId, status), CancellationToken.None));
        if (isMoved) await LoadAsync();
    }

    private async Task RevealAsync()
    {
        isRevealing = true;
        try { revealed = await Queries.AskAsync(new RevealHealthAnswers(FormSubmissionId), CancellationToken.None); }
        catch (Exception unreadable) when (unreadable is not OperationCanceledException) { revealed = null; }
        isRevealing = false;
        reading = view is null ? null : new FormSubmissionReading(view, revealed);
    }
}
