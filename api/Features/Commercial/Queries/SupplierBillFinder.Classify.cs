using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero.Ledger;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

internal sealed partial class SupplierBillFinder
{
    /// <summary>
    /// One bill's place on the account, decided once for the whole bill the way the allocation page
    /// decides a bill: a bill with any line on the project (allocated whole, a split share, or linked
    /// to one of the supplier's orders) is on it, and its still-pending lines come along — bills are
    /// coded as a whole in practice, so a half-allocated bill is not half a bill. A bill with nothing
    /// on the project yet is placed by <see cref="SupplierBillPlacement"/> from its pending lines, or
    /// left out. A bill whose every line is settled elsewhere — another project, ignored, bucketed —
    /// is not this account's at all.
    /// </summary>
    private static SupplierBill? Classify(
        string invoiceId,
        List<XeroLedgerLineEntity> lines,
        SupplierBillSearch search,
        IReadOnlyDictionary<string, decimal> projectSplitShares,
        XeroAllocationSuggester suggester)
    {
        var projectNetByLineId = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var pending = new List<XeroLedgerLineEntity>();
        foreach (var line in lines)
        {
            if (projectSplitShares.TryGetValue(line.XeroLedgerLineId, out var share))
            {
                projectNetByLineId[line.XeroLedgerLineId] = share;
                continue;
            }
            if (IsAllocatedWholeTo(line, search.ProjectId))
            {
                projectNetByLineId[line.XeroLedgerLineId] = line.Net;
                continue;
            }
            if (IsPending(line)) pending.Add(line);
        }

        var isOnProject = projectNetByLineId.Count > 0
                          || lines.Any(line => search.LinkedLineIds.Contains(line.XeroLedgerLineId));
        var placement = isOnProject
            ? ProjectSupplierInvoicePlacement.Allocated
            : PlacementOfPending(lines, pending, search, suggester);
        if (placement is null) return null;

        foreach (var line in pending)
            projectNetByLineId[line.XeroLedgerLineId] = line.Net;
        return new SupplierBill(invoiceId, lines, projectNetByLineId, placement.Value);
    }

    private static bool IsAllocatedWholeTo(XeroLedgerLineEntity line, string projectId) =>
        line.AllocationStatus == (int)XeroAllocationStatus.Allocated
        && line.CostCenterCode != null
        && string.Equals(line.ProjectId, projectId, StringComparison.OrdinalIgnoreCase);

    private static bool IsPending(XeroLedgerLineEntity line) =>
        line.AllocationStatus is (int)XeroAllocationStatus.Unallocated or (int)XeroAllocationStatus.Disputed;

    // The bill's site is the project already decided on it in the queue, else the project its
    // Xero Sites tracking names — the same precedence the allocation page and the sweep use.
    private static ProjectSupplierInvoicePlacement? PlacementOfPending(
        List<XeroLedgerLineEntity> lines,
        List<XeroLedgerLineEntity> pending,
        SupplierBillSearch search,
        XeroAllocationSuggester suggester)
    {
        if (pending.Count == 0) return null;

        var hintedProjectId = pending
            .Select(line => line.ProjectId ?? suggester.SuggestProject(line.XeroSite))
            .FirstOrDefault(projectId => projectId is not null);
        var head = lines[0];
        var orderNumbersOnBill = WorkOrderBillReference.NumbersOn(
            head.Reference, lines.Select(line => line.Description), head.InvoiceNumber);
        var placement = SupplierBillPlacement.Decide(
            search.ProjectId, hintedProjectId, orderNumbersOnBill, search.ProjectOrderNumbers, search.IsSupplierOnlyHere);
        if (placement is null) return null;

        var isDisputed = pending.Any(line => line.AllocationStatus == (int)XeroAllocationStatus.Disputed);
        return isDisputed ? ProjectSupplierInvoicePlacement.Disputed : placement;
    }
}
