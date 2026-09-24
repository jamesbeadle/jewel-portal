using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Features.Cvr;

/// <summary>
/// One project's profit row, built the one way for every page that shows it (the Profit Summary,
/// the Owner Overview). The loads run in order because only a Draft latest claim needs its
/// per-line % entries, so claims must land before the entries read.
/// </summary>
public sealed class ProjectProfitReadModel
{
    private readonly ProjectFinancialSummaryReadModel summary;
    private readonly ProjectWorkOrdersReadModel workOrders;
    private readonly ValuationLinesReadModel lines;
    private readonly ValuationClaimsReadModel claims;
    private readonly ClaimLinesReadModel claimEntries;
    private readonly IValuationInvoiceStore invoices;
    private readonly IQueryClient queries;

    private readonly Dictionary<string, (decimal Certified, decimal DepositCredited)> certificationByProject = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<PackageReconciliationRow>> packagesByProject = new(StringComparer.OrdinalIgnoreCase);

    public ProjectProfitReadModel(
        ProjectFinancialSummaryReadModel summary,
        ProjectWorkOrdersReadModel workOrders,
        ValuationLinesReadModel lines,
        ValuationClaimsReadModel claims,
        ClaimLinesReadModel claimEntries,
        IValuationInvoiceStore invoices,
        IQueryClient queries)
    {
        this.summary = summary;
        this.workOrders = workOrders;
        this.lines = lines;
        this.claims = claims;
        this.claimEntries = claimEntries;
        this.invoices = invoices;
        this.queries = queries;
    }

    /// <summary>Every read one project's row needs, in order; a failed read fails the row.</summary>
    public async Task LoadAsync(string projectId, CancellationToken cancellationToken)
    {
        await summary.RefreshAsync(projectId, cancellationToken);
        if (summary.LastRefreshFailed(projectId))
            throw new InvalidOperationException("The financial summary could not be loaded.");
        await workOrders.RefreshAsync(projectId, cancellationToken);
        await lines.RefreshAsync(projectId, cancellationToken);
        await claims.RefreshAsync(projectId, cancellationToken);
        var invoiceSummary = await invoices.GetSummaryAsync(projectId, cancellationToken);
        certificationByProject[projectId] = (invoiceSummary.TotalCertified, invoiceSummary.TotalDepositCredited);
        if (LatestClaimFor(projectId) is { Status: ValuationClaimStatus.Draft } draft)
            await claimEntries.RefreshAsync(draft.ValuationClaimId, cancellationToken);
        packagesByProject[projectId] = await queries.AskAsync(new ListPackageReconciliation(projectId), cancellationToken);
    }

    public ValuationClaim? LatestClaimFor(string projectId) =>
        claims.Current(projectId)
            .OrderByDescending(claim => claim.ClaimNumber)
            .FirstOrDefault();

    public ProfitRow RowFor(string projectId)
    {
        var latest = LatestClaimFor(projectId);
        var entries = latest is { Status: ValuationClaimStatus.Draft }
            ? claimEntries.Current(latest.ValuationClaimId)
            : Array.Empty<ClaimLine>();
        var certification = certificationByProject.TryGetValue(projectId, out var totals) ? totals : (0m, 0m);
        var figures = ValuationSummaryFigures.For(
            lines.Current(projectId), entries, latest,
            certification.Item1, certification.Item2);

        var summaryRows = summary.Current(projectId);
        var packages = packagesByProject.TryGetValue(projectId, out var packageRows)
            ? packageRows
            : Array.Empty<PackageReconciliationRow>();

        // Actual cost of sales is the gross allocated spend — the Financials tab's total, which
        // adds packaged invoiced cost back in via the package rows (RowActual + InvoicedToDate).
        var actualCost = summaryRows.Sum(row => row.ActualCost - row.PackagedActualCost)
                         + packages.Sum(package => package.InvoicedToDate);

        return new ProfitRow(
            InitialContractSum: figures.ContractSum,
            // The same target-cost rule as the Financials tab, applied to the initial sum: what
            // the contract should cost us with the assumed markup backed out.
            InitialContractCosts: Math.Round(figures.ContractSum * FinancialSummaryAssumptions.CostFactor, 2),
            NetVariations: figures.NetVariations,
            CertifiedToDate: figures.CertifiedToDate,
            ActualCostOfSales: actualCost,
            ContractValue: figures.RevisedContractSum,
            ForecastCostOfSales: ProjectDrawdown.ForecastCostOfSales(
                summaryRows, ProjectDrawdown.CommittedByCostCode(workOrders.Current(projectId)), packages),
            WorksComplete: figures.TotalWorksComplete,
            RetentionOutstanding: figures.RetentionOutstanding);
    }
}
