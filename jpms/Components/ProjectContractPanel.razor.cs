using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Components;

public partial class ProjectContractPanel
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    private bool dataFailed;
    private bool termsOpen;
    private bool uploading;
    private string? uploadError;
    private IBrowserFile? selectedFile;

    private bool amendmentUploading;
    private string? amendmentError;
    private IBrowserFile? selectedAmendmentFile;
    private string amendmentTitle = "";
    private DateTime? amendmentDate;
    private ProjectContractAmendment? editingAmendment;
    private ProjectContractAmendment? pendingRemove;
    private bool removeBusy;
    private string? removeError;

    private ProjectContract? Current => Contracts.ForProject(ProjectId);

    // Non-null accessor for the render; the panel is gated on LoadedFor, which only opens once
    // the amendments have been fetched alongside the contract.
    private IReadOnlyList<ProjectContractAmendment> Amendments =>
        Contracts.AmendmentsFor(ProjectId) ?? Array.Empty<ProjectContractAmendment>();

    // Mirrors ProjectContractRoles.AllowedToManageContract. Deliberately narrower than who can
    // read: a wrong retention percent or completion date propagates silently into every notice the
    // project issues.
    private bool CanManage =>
        Session.AvailableRoles.Any(role =>
            role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.QuantitySurveyor);

    protected override async Task OnInitializedAsync()
    {
        Contracts.OnChange += StateHasChanged;
        await LoadAsync();
    }

    public void Dispose() => Contracts.OnChange -= StateHasChanged;

    // Once, from OnInitializedAsync, never from render. RefreshAsync re-reads on every call (it
    // only guards against a concurrent duplicate), so returning to this pane revalidates rather
    // than showing whatever was cached the first time.
    private async Task LoadAsync()
    {
        dataFailed = false;
        try
        {
            await Contracts.RefreshAsync(ProjectId, CancellationToken.None);
        }
        catch
        {
            // The gate has to open on a failure or the panel pulses forever. The toast already
            // carries the reference and the detail.
            dataFailed = true;
        }
    }

    private void OnFileSelected(InputFileChangeEventArgs args)
    {
        selectedFile = args.FileCount > 0 ? args.File : null;
        uploadError = null;
    }

    private async Task UploadAsync()
    {
        if (uploading || selectedFile is null) return;
        uploading = true;
        uploadError = null;
        try
        {
            await Contracts.UploadDocumentAsync(ProjectId, selectedFile, CancellationToken.None);
            selectedFile = null;
        }
        catch (Exception ex)
        {
            // The store surfaces the server's own sentence — a storage misconfiguration says so
            // rather than showing a bare status code.
            uploadError = $"Upload failed: {ex.Message}";
        }
        finally
        {
            uploading = false;
        }
    }

    private void OnAmendmentFileSelected(InputFileChangeEventArgs args)
    {
        selectedAmendmentFile = args.FileCount > 0 ? args.File : null;
        amendmentError = null;
    }

    private async Task UploadAmendmentAsync()
    {
        if (amendmentUploading || selectedAmendmentFile is null) return;
        amendmentUploading = true;
        amendmentError = null;
        try
        {
            // The endpoint falls back to the filename when the title is blank, so the list is
            // never a column of blanks.
            await Contracts.UploadAmendmentAsync(
                ProjectId, selectedAmendmentFile, amendmentTitle.Trim(), AsOffset(amendmentDate), null,
                CancellationToken.None);
            selectedAmendmentFile = null;
            amendmentTitle = "";
            amendmentDate = null;
        }
        catch (Exception ex)
        {
            // The store surfaces the server's own sentence — a storage misconfiguration says so
            // rather than showing a bare status code.
            amendmentError = $"Upload failed: {ex.Message}";
        }
        finally
        {
            amendmentUploading = false;
        }
    }

    private async Task RemovePendingAsync()
    {
        if (pendingRemove is null || removeBusy) return;
        removeBusy = true;
        removeError = null;
        try
        {
            await Contracts.RemoveAmendmentAsync(
                ProjectId, pendingRemove.ProjectContractAmendmentId, CancellationToken.None);
            pendingRemove = null;
        }
        catch (Exception ex)
        {
            removeError = $"Couldn't remove the amendment: {ex.Message}";
        }
        finally
        {
            removeBusy = false;
        }
    }

    private void OpenTerms() => termsOpen = true;

    private void CloseTerms() => termsOpen = false;

    private string DocumentUrl(bool inline) =>
        $"/api/projects/{ProjectId}/contract/document{(inline ? "?inline=1" : "")}";

    private string AmendmentDocumentUrl(string amendmentId, bool inline) =>
        $"/api/projects/{ProjectId}/contract/amendments/{amendmentId}/document{(inline ? "?inline=1" : "")}";

    private static DateTimeOffset? AsOffset(DateTime? date) =>
        date is { } value ? new DateTimeOffset(value.Date, TimeSpan.Zero) : null;

    private static IEnumerable<(string Label, string Value)> Summary(ProjectContract contract)
    {
        yield return ("Form", contract.FormDisplayName);
        yield return ("Contract sum", WholeMoney(contract.ContractSum));
        yield return ("LADs", $"{WholeMoney(contract.LiquidatedDamagesPerWeek)} / week");
        yield return ("Employer", Text(contract.EmployerName));
        yield return ("Contract administrator", Text(contract.ContractAdministratorName));
        yield return ("Contractor", Text(contract.ContractorName));
        yield return ("Possession", Date(contract.PossessionDate));
        yield return ("Completion", Date(contract.CompletionDate));
        yield return ("Defects liability", $"{contract.DefectsLiabilityPeriodMonths} months");
        yield return ("Retention", $"{Pct(contract.RetentionPercent)} → {Pct(contract.RetentionPercentAfterCompletion)}");
        yield return ("Valuation cut-off", contract.ApplicationCutOffDayOfMonth is { } day ? $"Day {day}" : "—");
        yield return ("Payment notices", $"{contract.PaymentNoticeDays}d notice · {contract.PayLessNoticeDays}d pay-less · {contract.FinalDateForPaymentDays}d final date");
        yield return ("OH&P", $"{Pct(contract.OhpDirectWorksPercent)} direct · {Pct(contract.OhpSubcontractorPercent)} sub · {Pct(contract.AttendanceOnClientDirectPercent)} attendance");
        yield return ("Daywork", $"{Pct(contract.DayworkLabourPercent)} labour · {Pct(contract.DayworkMaterialsPercent)} materials · {Pct(contract.DayworkPlantPercent)} plant");
    }


    private static string Pct(decimal value) => $"{value:0.##}%";

    private static string Date(DateTimeOffset? value) => value?.ToString("d MMM yyyy") ?? "—";

    private static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

}
