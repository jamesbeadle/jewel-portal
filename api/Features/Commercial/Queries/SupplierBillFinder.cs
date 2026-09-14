using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// Finds every Xero bill that belongs on a supplier's account for a project: the bills linked to
/// the supplier's orders there (whoever the Xero contact is), the supplier's bills allocated to the
/// project whole or by split share, and the supplier's bills nobody has allocated yet that point at
/// the project (<see cref="SupplierBillPlacement"/>). Bills are read whole once found, so a bill
/// that also carries lines for another project can say how much sits elsewhere.
/// </summary>
internal sealed partial class SupplierBillFinder
{
    private readonly JpmsContext context;

    public SupplierBillFinder(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<SupplierBill>> FindAsync(
        SupplierBillSearch search, CancellationToken cancellationToken)
    {
        var names = await NamesOfAsync(search.Supplier, cancellationToken);
        var candidates = await CandidateLinesAsync(search, cancellationToken);
        // A contact whose bill is already linked to one of the supplier's orders IS the supplier,
        // whatever Xero calls them — so their next bill is found even before anyone links it.
        names.Learn(candidates.Where(line => search.LinkedLineIds.Contains(line.XeroLedgerLineId)).Select(line => line.ContactName));
        var invoiceIds = candidates
            .Where(line => search.LinkedLineIds.Contains(line.XeroLedgerLineId) || names.Matches(line.ContactName))
            .Select(line => line.XeroInvoiceId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (invoiceIds.Count == 0) return Array.Empty<SupplierBill>();

        var billLines = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => invoiceIds.Contains(line.XeroInvoiceId))
            .ToListAsync(cancellationToken);
        var shares = await ProjectSplitSharesAsync(search.ProjectId, billLines, cancellationToken);
        var suggester = await SuggesterAsync(cancellationToken);

        return billLines
            .GroupBy(line => line.XeroInvoiceId, StringComparer.OrdinalIgnoreCase)
            .Select(bill => Classify(bill.Key, bill.OrderBy(line => line.XeroLineItemId).ToList(), search, shares, suggester))
            .OfType<SupplierBill>()
            .ToList();
    }

    private async Task<SupplierNames> NamesOfAsync(SubcontractorEntity supplier, CancellationToken cancellationToken)
    {
        var linkedContactNames = await context.SubcontractorXeroLinks.AsNoTracking()
            .Where(link => link.SubcontractorId == supplier.SubcontractorId)
            .Select(link => link.XeroContactName)
            .ToListAsync(cancellationToken);
        return new SupplierNames(supplier.CompanyName, linkedContactNames);
    }

    // Bounded in SQL to the lines that could possibly be the supplier's on this project: anything
    // already on the project (whole or by split), anything still pending a decision, and the lines
    // linked to the supplier's orders. The name test runs in memory — it is the directory's fuzzy
    // rule, not a SQL equality.
    private async Task<List<XeroLedgerLineEntity>> CandidateLinesAsync(
        SupplierBillSearch search, CancellationToken cancellationToken)
    {
        var splitLineIds = await context.XeroCostSplits.AsNoTracking()
            .Where(split => split.ProjectId == search.ProjectId)
            .Select(split => split.XeroLedgerLineId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var linkedLineIds = search.LinkedLineIds.ToList();
        return await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.ProjectId == search.ProjectId
                           || line.AllocationStatus == (int)XeroAllocationStatus.Unallocated
                           || line.AllocationStatus == (int)XeroAllocationStatus.Disputed
                           || linkedLineIds.Contains(line.XeroLedgerLineId)
                           || splitLineIds.Contains(line.XeroLedgerLineId))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<string, decimal>> ProjectSplitSharesAsync(
        string projectId, List<XeroLedgerLineEntity> billLines, CancellationToken cancellationToken)
    {
        var lineIds = billLines.Select(line => line.XeroLedgerLineId).ToList();
        var splits = await context.XeroCostSplits.AsNoTracking()
            .Where(split => split.ProjectId == projectId && lineIds.Contains(split.XeroLedgerLineId))
            .ToListAsync(cancellationToken);
        return splits
            .GroupBy(split => split.XeroLedgerLineId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(split => split.Net), StringComparer.OrdinalIgnoreCase);
    }

    // Project suggestions only — the site-to-project reading the allocation page makes, so this
    // account and the queue can never disagree about which project a Xero site means.
    private async Task<XeroAllocationSuggester> SuggesterAsync(CancellationToken cancellationToken)
    {
        var projects = await context.Projects.AsNoTracking().ToListAsync(cancellationToken);
        return new XeroAllocationSuggester(projects, Array.Empty<CostCenterEntity>());
    }
}
