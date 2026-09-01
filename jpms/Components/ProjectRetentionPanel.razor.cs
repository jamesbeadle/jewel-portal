using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Retention;

namespace Jewel.JPMS.Components;

public partial class ProjectRetentionPanel
{
    [Parameter, EditorRequired] public string ProjectId { get; set; } = "";

    private bool termsOpen;
    private bool confirmOpen;
    private bool busy;
    private string? error;

    private decimal depositPercent = 0m;
    private decimal depositReleasedOpening = 0m;
    private decimal retentionPercent = 5m;
    private decimal completionReleasePercent = 2.5m;
    private int defectsPeriodMonths = 12;
    private DateTime? practicalCompletionDate;

    private RetentionMilestone confirmMilestone;
    private string confirmAmount = "";

    private ProjectRetention? Current => Retention.RetentionFor(ProjectId);

    // Mirrors the server-side authorisations (directors and the finance director;
    // administrators carry every role).
    private bool CanEdit =>
        Session.AvailableRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector);

    // Works complete / contract sum / deposit released from the latest claim (live for
    // drafts) — the same figures the Valuation Report footer and Cashflow tab use. The
    // invoice summary supplies the deposit credits already taken on issued/paid invoices,
    // so "Released to date" counts them; certified itself doesn't affect this panel.
    private ProjectValuationInvoiceSummary? invoiceSummary;

    private ValuationSummaryFigures Figures
    {
        get
        {
            var claims = ValuationReport.ClaimsFor(ProjectId);
            var latest = claims.OrderByDescending(claim => claim.ClaimNumber).FirstOrDefault();
            var entries = latest is { Status: ValuationClaimStatus.Draft }
                ? ValuationReport.EntriesFor(latest.ValuationClaimId)
                : Array.Empty<ClaimLine>();
            return ValuationSummaryFigures.For(ValuationReport.LinesFor(ProjectId), entries, latest,
                invoiceSummary?.TotalCertified ?? 0m, invoiceSummary?.TotalDepositCredited ?? 0m);
        }
    }

    private RetentionSchedule Schedule
    {
        get
        {
            var figures = Figures;
            return RetentionSchedule.For(Current!, figures.TotalWorksComplete, figures.RevisedContractSum);
        }
    }

    // The deposit trio reads the TERMS' percent against the live contract sum (so editing
    // the % updates "Received" immediately); released-to-date comes from the claims, which
    // carry the same terms — SetProjectRetention keeps open drafts in step. It includes
    // the opening balance settled before the portal began deducting.
    private decimal DepositReceived =>
        ValuationCalculations.DepositReceived(Figures.ContractSum, Current?.DepositPercent ?? 0m);

    private decimal DepositReleasedToDate => Figures.DepositReleasedToDate;

    private decimal? ParsedConfirmAmount =>
        decimal.TryParse(confirmAmount, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.GetCultureInfo("en-GB"), out var value) ? value : null;

    protected override void OnInitialized()
    {
        Retention.OnChange += StateHasChanged;
        ValuationReport.OnChange += StateHasChanged;
        Retention.Refresh(ProjectId);
    }

    protected override async Task OnInitializedAsync()
    {
        try { invoiceSummary = await Invoices.GetSummaryAsync(ProjectId); }
        catch { /* deposit released-to-date just omits invoice credits until it loads */ }
    }

    private void OpenTerms()
    {
        depositPercent = Current?.DepositPercent ?? 0m;
        depositReleasedOpening = Current?.DepositReleasedOpening ?? 0m;
        retentionPercent = Current?.RetentionPercent ?? 5m;
        completionReleasePercent = Current?.CompletionReleasePercent ?? 2.5m;
        defectsPeriodMonths = Current?.DefectsPeriodMonths ?? 12;
        practicalCompletionDate = Current?.PracticalCompletionAt?.Date;
        error = null;
        termsOpen = true;
    }

    private void CloseTerms() { termsOpen = false; error = null; }

    private async Task SaveTerms()
    {
        if (busy) return;
        error = null;
        busy = true;
        try
        {
            await Retention.SetAsync(new SetProjectRetention(
                ProjectId, retentionPercent, completionReleasePercent, defectsPeriodMonths,
                practicalCompletionDate is { } date ? new DateTimeOffset(date.Date, TimeSpan.Zero) : null,
                DepositPercent: depositPercent,
                DepositReleasedOpening: depositReleasedOpening));
            CloseTerms();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't save the deposit & retention terms. Please try again."; }
        finally { busy = false; }
    }

    private void OpenConfirm(RetentionScheduleLine line)
    {
        confirmMilestone = line.Milestone;
        confirmAmount = line.Amount.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
        error = null;
        confirmOpen = true;
    }

    private void CloseConfirm() { confirmOpen = false; error = null; }

    private async Task SaveConfirm()
    {
        if (busy || ParsedConfirmAmount is not { } amount) return;
        error = null;
        busy = true;
        try
        {
            await Retention.ConfirmReleaseAsync(new ConfirmRetentionRelease(ProjectId, confirmMilestone, amount));
            CloseConfirm();
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't confirm the release. Please try again."; }
        finally { busy = false; }
    }


    private static string Pct(decimal value) =>
        value.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("en-GB")) + "%";

    public void Dispose()
    {
        Retention.OnChange -= StateHasChanged;
        ValuationReport.OnChange -= StateHasChanged;
    }
}
