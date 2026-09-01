using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Components;

public partial class ProjectContractTermsDialog
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    /// <summary>Raised after the terms have been written and the store re-read.</summary>
    [Parameter] public EventCallback OnSaved { get; set; }

    [Parameter] public EventCallback OnCancelled { get; set; }

    private bool busy;
    private string? error;

    private ContractForm form = ContractForm.Unspecified;
    private string formEdition = "";
    private string bespokeDeviations = "";

    private string employerName = "";
    private string caName = "";
    private string caEmail = "";
    private string architectName = "";
    private string architectEmail = "";
    private string contractorName = "";

    private decimal contractSum;
    private decimal liquidatedDamagesPerWeek;

    private DateTime? contractDate;
    private DateTime? possessionDate;
    private DateTime? completionDate;

    // The rates a JCT job usually runs at, used only to seed a brand-new row. Zero would be a
    // worse starting point than a typical figure — it is a real number that silently prices every
    // variation at nil OH&P — but neither is a substitute for reading the Contract Particulars,
    // which is what the note above the block says.
    private decimal retentionPercent = 5m;
    private decimal retentionPercentAfterCompletion = 2.5m;
    private int defectsLiabilityPeriodMonths = 12;

    private int? applicationCutOffDayOfMonth;
    private int paymentNoticeDays = 5;
    private int payLessNoticeDays = 5;
    private int finalDateForPaymentDays = 14;

    private decimal ohpDirectWorksPercent = 10m;
    private decimal ohpSubcontractorPercent = 10m;
    private decimal attendanceOnClientDirectPercent = 5m;
    private decimal dayworkLabourPercent = 15m;
    private decimal dayworkMaterialsPercent = 10m;
    private decimal dayworkPlantPercent = 10m;

    private const string LabelClass = "block text-xs uppercase tracking-wider text-content-subtle font-semibold mb-1";

    private ProjectContract? Current => Contracts.ForProject(ProjectId);

    protected override void OnInitialized()
    {
        if (Current is not { } contract) return;

        form = contract.Form;
        formEdition = contract.FormEdition ?? "";
        bespokeDeviations = contract.BespokeDeviations ?? "";

        employerName = contract.EmployerName ?? "";
        caName = contract.ContractAdministratorName ?? "";
        caEmail = contract.ContractAdministratorEmail ?? "";
        architectName = contract.ArchitectName ?? "";
        architectEmail = contract.ArchitectEmail ?? "";
        contractorName = contract.ContractorName ?? "";

        contractSum = contract.ContractSum;
        liquidatedDamagesPerWeek = contract.LiquidatedDamagesPerWeek;

        contractDate = contract.ContractDate?.Date;
        possessionDate = contract.PossessionDate?.Date;
        completionDate = contract.CompletionDate?.Date;

        retentionPercent = contract.RetentionPercent;
        retentionPercentAfterCompletion = contract.RetentionPercentAfterCompletion;
        defectsLiabilityPeriodMonths = contract.DefectsLiabilityPeriodMonths;

        applicationCutOffDayOfMonth = contract.ApplicationCutOffDayOfMonth;
        paymentNoticeDays = contract.PaymentNoticeDays;
        payLessNoticeDays = contract.PayLessNoticeDays;
        finalDateForPaymentDays = contract.FinalDateForPaymentDays;

        ohpDirectWorksPercent = contract.OhpDirectWorksPercent;
        ohpSubcontractorPercent = contract.OhpSubcontractorPercent;
        attendanceOnClientDirectPercent = contract.AttendanceOnClientDirectPercent;
        dayworkLabourPercent = contract.DayworkLabourPercent;
        dayworkMaterialsPercent = contract.DayworkMaterialsPercent;
        dayworkPlantPercent = contract.DayworkPlantPercent;
    }

    private Task Cancel() => busy ? Task.CompletedTask : OnCancelled.InvokeAsync();

    private async Task SaveAsync()
    {
        if (busy) return;
        busy = true;
        error = null;
        try
        {
            // UpdatedByEmail is re-stamped from the session by the endpoint; whatever is sent here
            // is discarded, so there is nothing to look up client-side.
            await Contracts.SetTermsAsync(
                new SetProjectContractTerms(
                    ProjectId,
                    "",
                    form,
                    Trimmed(formEdition),
                    Trimmed(bespokeDeviations),
                    Trimmed(employerName),
                    Trimmed(caName),
                    Trimmed(caEmail),
                    Trimmed(architectName),
                    Trimmed(architectEmail),
                    Trimmed(contractorName),
                    contractSum,
                    liquidatedDamagesPerWeek,
                    AsOffset(contractDate),
                    AsOffset(possessionDate),
                    AsOffset(completionDate),
                    retentionPercent,
                    retentionPercentAfterCompletion,
                    defectsLiabilityPeriodMonths,
                    applicationCutOffDayOfMonth,
                    paymentNoticeDays,
                    payLessNoticeDays,
                    finalDateForPaymentDays,
                    ohpDirectWorksPercent,
                    ohpSubcontractorPercent,
                    attendanceOnClientDirectPercent,
                    dayworkLabourPercent,
                    dayworkMaterialsPercent,
                    dayworkPlantPercent),
                CancellationToken.None);

            await OnSaved.InvokeAsync();
        }
        catch (CommandFailedException ex)
        {
            // The endpoint's validation sentences, joined by the command sender. Shown here rather
            // than in a toast — they belong next to the fields they are about.
            error = ex.Message;
        }
        catch
        {
            error = "Couldn't save the contract terms. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    private static string? Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset? AsOffset(DateTime? date) =>
        date is { } value ? new DateTimeOffset(value.Date, TimeSpan.Zero) : null;
}
