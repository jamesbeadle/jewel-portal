using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// Unpaid, unallocated Xero purchase lines whose Sites tracking points at this project —
/// the Cashflow tab's guard against money Xero holds for the site that no project view is
/// counting. Site matching reuses XeroAllocationSuggester (mapped XeroSiteName first, then
/// normalised name equality/containment) so this guard and the allocation page's
/// suggestions can never disagree about which project a site means.
/// </summary>
public sealed class ListUnallocatedSiteBillsHandler
    : IQueryHandler<ListUnallocatedSiteBills, IReadOnlyList<UnallocatedSiteBill>>
{
    private readonly JpmsContext context;

    public ListUnallocatedSiteBillsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<UnallocatedSiteBill>> HandleAsync(
        ListUnallocatedSiteBills query, CancellationToken cancellationToken)
    {
        // Candidate lines are bounded in SQL: unallocated, site-tracked, and not settled —
        // AmountDue non-zero, or (for rows synced before AddXeroLinePaymentState, whose
        // amounts read 0) any status other than Xero's terminal PAID.
        var candidates = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.AllocationStatus == (int)XeroAllocationStatus.Unallocated
                           && line.XeroSite != null && line.XeroSite != ""
                           && (line.AmountDue != 0m
                               || (line.InvoiceTotal == 0m && line.InvoiceStatus != "PAID")))
            .ToListAsync(cancellationToken);
        if (candidates.Count == 0) return Array.Empty<UnallocatedSiteBill>();

        var projects = await context.Projects.AsNoTracking().ToListAsync(cancellationToken);
        // Cost-centre suggestions aren't wanted here, so the suggester gets an empty list.
        var suggester = new XeroAllocationSuggester(projects, Array.Empty<Data.Entities.CostCenterEntity>());

        return candidates
            .Where(line => string.Equals(
                suggester.SuggestProject(line.XeroSite), query.ProjectId, StringComparison.OrdinalIgnoreCase))
            .Select(line => new UnallocatedSiteBill(
                line.XeroLedgerLineId,
                line.Date,
                line.ContactName ?? "",
                line.InvoiceNumber ?? "",
                line.Description ?? "",
                line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net,
                line.Type == "ACCPAYCREDIT" ? -line.Tax : line.Tax,
                line.InvoiceStatus,
                line.InvoiceTotal,
                line.AmountDue))
            // The fallback path can still let a settled pre-migration credit note through
            // with OutstandingNet 0 — drop anything that contributes nothing.
            .Where(bill => bill.OutstandingNet != 0m)
            .OrderByDescending(bill => bill.Date ?? DateTime.MinValue)
            .ToList();
    }
}
