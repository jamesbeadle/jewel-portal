using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// A marked quiz as the office reads it: the score and whether it passed, and filing it to the
/// directory company it was taken for, so the due diligence is evidenced on the company's record.
/// </summary>
public partial class QuizResultPanel
{
    [Parameter, EditorRequired] public FormSubmissionReading Reading { get; set; } = default!;
    [Parameter, EditorRequired] public FormQuizScore Score { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }

    private readonly FormWrite write = new();
    private string subcontractorId = "";
    private bool isOpen;
    private FormDirectoryFiling? filed;

    private Tone ScoreTone => Score.HasPassed ? Tone.Positive : Tone.Negative;

    private string Explanation => Score.HasPassed
        ? "The quiz's record goes on the company's compliance record as their current quiz, standing for a year."
        : "The quiz's record goes on the company's compliance record as their current quiz, already expired, so their standing reads Expired until a retake passes.";

    protected override void OnInitialized() => Directory.OnChange += StateHasChanged;

    private void Open()
    {
        subcontractorId = DirectoryCompanyPicks.NamedBy(Directory, Reading.View.Answers.GetValueOrDefault("company", ""));
        isOpen = true;
    }

    private async Task FileAsync()
    {
        var command = new FileQuizToDirectory(Reading.Submission.FormSubmissionId, subcontractorId);
        var isFiled = await write.RunAsync(async () => filed = await Commands.SendAsync(command, CancellationToken.None));
        isOpen = !isFiled;
        if (isFiled) await OnChanged.InvokeAsync();
    }

    public void Dispose() => Directory.OnChange -= StateHasChanged;
}
