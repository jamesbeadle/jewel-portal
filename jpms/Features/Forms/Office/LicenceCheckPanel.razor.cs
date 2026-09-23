using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// A Company Vehicle Form's licence check: how many of the DVLA check code's 21 days are left, and
/// recording the check — which keeps only whether the driver meets the insurance criteria, as the
/// form promised, and removes the photograph and the driving-record answers.
/// </summary>
public partial class LicenceCheckPanel
{
    [Parameter, EditorRequired] public FormSubmissionReading Reading { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }

    private readonly FormWrite write = new();
    private IReadOnlyList<DrivingLicenceCheck>? checks;
    private bool isOpen;
    private string checkedOn = FormDates.Write(DateOnly.FromDateTime(DateTime.Today));
    private bool isWithinCriteria;
    private string note = "";

    private DrivingLicenceCheck? Check =>
        checks?.FirstOrDefault(check => check.FormSubmissionId == Reading.Submission.FormSubmissionId);

    private int DaysLeft => DrivingLicenceChecks.CheckCodeDaysLeft(Reading.Submission.SubmittedAt, DateTimeOffset.UtcNow);

    private string CodeLine => DaysLeft > 0
        ? $"The DVLA check code lasts {DrivingLicenceChecks.DaysACheckCodeLasts} days: {DaysLeft} left to view their record with it."
        : "The DVLA check code has run out. Ask them for a new one to view their record.";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        try { checks = await Queries.AskAsync(new ListDrivingLicenceChecks(), CancellationToken.None); }
        catch (Exception unreadable) when (unreadable is not OperationCanceledException) { checks = null; }
    }

    private async Task RecordAsync()
    {
        var command = new RecordDrivingLicenceCheck(Reading.Submission.FormSubmissionId, FormDates.Read(checkedOn)!.Value, isWithinCriteria, note.Trim());
        var isRecorded = await write.RunAsync(() => Commands.SendAsync(command, CancellationToken.None));
        isOpen = !isRecorded;
        if (!isRecorded) return;
        await LoadAsync();
        await OnChanged.InvokeAsync();
    }
}
