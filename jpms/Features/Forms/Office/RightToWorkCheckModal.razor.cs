using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// Record a check (the dashboard's register form): the six numbered steps on one dialog, checked on
/// the page by the register's own rules before it is sent — a pass is a legal statement and cannot be
/// half true — then the evidence filed against the saved check. A check that saved but whose evidence
/// did not is kept, and the dialog says so rather than pretending neither happened.
/// </summary>
public partial class RightToWorkCheckModal
{
    [Parameter] public RightToWorkCheckDraft? Draft { get; set; }
    [Parameter] public EventCallback<RightToWorkCheck> OnSaved { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private readonly FormWrite write = new();
    private IBrowserFile? evidence;
    private string? problem;

    private string Title => Draft?.RightToWorkCheckId is null ? "Record a check" : "Correct the check";

    protected override void OnParametersSet()
    {
        if (Draft is not null) return;
        evidence = null;
        problem = null;
    }

    private async Task SaveAsync()
    {
        problem = Draft!.FirstProblem(DateOnly.FromDateTime(DateTime.Today));
        if (problem is not null) return;
        RightToWorkCheck? saved = null;
        var isSaved = await write.RunAsync(async () => saved = await Commands.SendAsync(Draft.ToCommand(), CancellationToken.None));
        problem = write.Problem;
        if (!isSaved || saved is null) return;
        Draft.RightToWorkCheckId = saved.RightToWorkCheckId;
        var evidenceProblem = evidence is null ? null : await RightToWorkEvidenceUpload.SendAsync(Http, saved.RightToWorkCheckId, evidence);
        problem = evidenceProblem is null ? null : $"The check was recorded but the evidence did not upload: {evidenceProblem}. Open the row and attach it.";
        if (problem is null) await OnSaved.InvokeAsync(saved);
    }
}
