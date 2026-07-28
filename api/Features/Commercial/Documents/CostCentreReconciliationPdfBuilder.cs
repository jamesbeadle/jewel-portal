using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>The rendered reconciliation PDF plus the filename it should travel under.</summary>
public sealed record CostCentreReconciliationPdf(byte[] Content, string FileName);

/// <summary>
/// Assembles and renders one cost centre's (or roll-up group's) reconciliation PDF. Every figure
/// comes through the same query handlers the Financials tab reads — the financial summary rows
/// for sales / target / non-WO cost, the valuation lines for the sales detail, the project's
/// work orders for the committed detail (drafts included, rejected never — matching
/// ProjectDrawdown.CommittedByCostCode), and the per-centre Xero lines for the spend detail —
/// so the PDF the accountant sends the managing director says what the screen says.
/// </summary>
public sealed class CostCentreReconciliationPdfBuilder
{
    private readonly IQueryHandler<GetProjectFinancialSummary, IReadOnlyList<ProjectFinancialSummaryRow>> summary;
    private readonly IQueryHandler<ListValuationLinesForProject, IReadOnlyList<ValuationLineItem>> valuationLines;
    private readonly IQueryHandler<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>> workOrders;
    private readonly IQueryHandler<ListCostCentreActualCosts, IReadOnlyList<CostCentreActualCostLine>> actualCosts;
    private readonly JpmsContext context;

    public CostCentreReconciliationPdfBuilder(
        IQueryHandler<GetProjectFinancialSummary, IReadOnlyList<ProjectFinancialSummaryRow>> summary,
        IQueryHandler<ListValuationLinesForProject, IReadOnlyList<ValuationLineItem>> valuationLines,
        IQueryHandler<ListProjectWorkOrders, IReadOnlyList<ProjectWorkOrderDetail>> workOrders,
        IQueryHandler<ListCostCentreActualCosts, IReadOnlyList<CostCentreActualCostLine>> actualCosts,
        JpmsContext context)
    {
        this.summary = summary;
        this.valuationLines = valuationLines;
        this.workOrders = workOrders;
        this.actualCosts = actualCosts;
        this.context = context;
    }

    public async Task<CostCentreReconciliationPdf> BuildAsync(
        string projectId, IReadOnlyList<string> costCodes, string heading, CancellationToken cancellationToken)
    {
        var project = await context.Projects.FindAsync(new object[] { projectId }, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var codes = new HashSet<string>(costCodes, StringComparer.OrdinalIgnoreCase);

        var rows = (await summary.HandleAsync(new GetProjectFinancialSummary(projectId), cancellationToken))
            .Where(row => codes.Contains(row.CostCode))
            .ToList();
        var salesValue = rows.Sum(row => row.BudgetedSales);
        var targetCost = rows.Sum(row => row.BudgetedCost);
        var nonWoCost = rows.Sum(row => row.NonWorkOrderActualCost);
        var labourCost = rows.Sum(row => row.LabourActualCost);

        // Sales detail: the counting valuation lines coded to the centre — the same set
        // BudgetedSales is built from, so the section totals to the figure it sits under.
        var salesLines = (await valuationLines.HandleAsync(new ListValuationLinesForProject(projectId), cancellationToken))
            .Where(line => codes.Contains(line.CostCode) && line.CountsTowardTotals)
            .OrderBy(line => line.CostCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.DisplayOrder)
            .Select(line => new ReconciliationSalesLine(
                SalesRef(line),
                string.IsNullOrWhiteSpace(line.Description) ? line.VariationTitle : line.Description,
                line.LineAmount))
            .ToList();
        var salesOther = Math.Round(salesValue - salesLines.Sum(line => line.Amount), 2);

        // Work-order detail: each order's share of this centre (its lines coded here).
        // Drafts count and are marked; rejected drafts never count — the same rule as
        // ProjectDrawdown.CommittedByCostCode, so the total ties to the Financials row.
        var orders = (await workOrders.HandleAsync(new ListProjectWorkOrders(projectId), cancellationToken))
            .Where(detail => !detail.Order.IsRejected)
            .Select(detail => new
            {
                detail.Order,
                detail.SubcontractorName,
                Share = detail.Lines
                    .Where(line => codes.Contains(line.CostCode))
                    .Sum(line => line.LineTotal)
            })
            .Where(entry => entry.Share != 0m)
            .OrderBy(entry => entry.Order.Number)
            .ThenBy(entry => entry.Order.CreatedAt)
            .Select(entry => new ReconciliationWorkOrderLine(
                entry.Order.Reference,
                entry.SubcontractorName,
                entry.Order.Title,
                entry.Order.IsDraft ? "Draft" : entry.Order.Status.ToString(),
                entry.Share))
            .ToList();
        var woCommitted = orders.Sum(order => order.Amount);

        // Xero detail: the allocated spend on each centre that is NOT linked to a work
        // order — one query per code, the same shape the cost-of-sales drill-down shows.
        // Re-attribution rows are skipped (they move linked spend between centres for the
        // Actual Cost view; the non-WO figure is computed before re-attribution).
        var xeroEntries = new List<(DateTime? Date, ReconciliationCostLine Line)>();
        var xeroListed = 0m;
        foreach (var code in costCodes)
        {
            var lines = await actualCosts.HandleAsync(new ListCostCentreActualCosts(projectId, code), cancellationToken);
            foreach (var line in lines.Where(line => !line.IsWorkOrderAttribution))
            {
                var share = line.Net - (line.IsSplit ? 0m : line.LinkedTotal);
                if (share == 0m) continue;
                xeroListed += share;
                xeroEntries.Add((line.Date, new ReconciliationCostLine(
                    line.Date is { } date ? date.ToString("dd MMM yyyy") : "",
                    line.Supplier,
                    line.InvoiceNumber,
                    line.Description,
                    share)));
            }
        }
        var xeroLines = xeroEntries
            .OrderBy(entry => entry.Date ?? DateTime.MaxValue)
            .Select(entry => entry.Line)
            .ToList();
        var otherAdjustments = Math.Round(nonWoCost - labourCost - xeroListed, 2);

        var document = new CostCentreReconciliationDocument(
            project.Reference,
            project.Name,
            project.ClientName,
            heading,
            costCodes,
            DateTimeOffset.UtcNow,
            salesLines,
            salesOther,
            orders,
            xeroLines,
            labourCost,
            otherAdjustments,
            salesValue,
            targetCost,
            woCommitted,
            nonWoCost);

        var pdf = CostCentreReconciliationRenderer.Render(document);
        var fileName = SanitiseFileName(
            $"{project.Reference} - Cost centre reconciliation - {heading} - {document.GeneratedAt:yyyy-MM-dd}.pdf");
        return new CostCentreReconciliationPdf(pdf, fileName);
    }

    // Mirrors the on-screen convention (valuation report / sales-lines modal): variation
    // lines show their V-ref; everything else its cost code with the bill section as fallback.
    private static string SalesRef(ValuationLineItem line) =>
        line.ElementType == ValuationElementType.Variation
            ? (string.IsNullOrWhiteSpace(line.VariationRef) ? line.CostCode : line.VariationRef)
            : (string.IsNullOrWhiteSpace(line.CostCode) ? line.SectionCode : line.CostCode);

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
