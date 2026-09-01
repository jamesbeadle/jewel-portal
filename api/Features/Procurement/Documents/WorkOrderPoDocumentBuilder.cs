using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Features.Procurement.Queries;

namespace Jewel.JPMS.Api.Features.Procurement.Documents;

/// <summary>
/// Resolves everything the purchase-order PDF prints, mirroring what the portal's PO page host
/// resolves for PurchaseOrderSheet: the order and its lines (PaidToDate restated from the settled
/// Xero position, exactly as the Work Orders tab shows it), the supplier's directory record
/// (name, contact, letter-style address, payment terms), the project's name and site address, and
/// the approver's display name. Returns null when the order doesn't exist — the callers turn that
/// into their own not-found answer.
/// </summary>
public static class WorkOrderPoDocumentBuilder
{
    public static async Task<WorkOrderPoDocumentModel?> BuildAsync(
        JpmsContext context, string workOrderId, CancellationToken cancellationToken)
    {
        var orderEntity = await context.WorkOrders.AsNoTracking()
            .FirstOrDefaultAsync(order => order.WorkOrderId == workOrderId, cancellationToken);
        if (orderEntity is null) return null;

        var order = orderEntity.ToModel();

        var lines = (await context.WorkOrderLines.AsNoTracking()
                .Where(line => line.WorkOrderId == workOrderId)
                .ToListAsync(cancellationToken))
            .OrderBy(line => line.SortOrder)
            .Select(line => line.ToModel())
            .ToList();

        // Paid-from-Xero, restated across the lines exactly as ListProjectWorkOrdersHandler does
        // for the Work Orders tab and the printed PO — one implementation, so the emailed PDF's
        // Paid column always agrees with the portal. Orders the ledger knows nothing about keep
        // their stored opening balances.
        var paidByOrder = await WorkOrderPaidPositions.ForProjectAsync(context, orderEntity.ProjectId, cancellationToken);
        if (paidByOrder.TryGetValue(workOrderId, out var paid))
            lines = ListProjectWorkOrdersHandler.SpreadPaidAcrossLines(lines, paid);

        var supplier = await context.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(sub => sub.SubcontractorId == orderEntity.SubcontractorId, cancellationToken);

        var project = await context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == orderEntity.ProjectId, cancellationToken);

        // Same fallback chain as the PO page: directory display name, else the sheet falls back
        // to the raw AwardedByEmail at render time.
        var approver = string.IsNullOrWhiteSpace(orderEntity.AwardedByEmail)
            ? null
            : await context.DirectoryUsers.AsNoTracking()
                .FirstOrDefaultAsync(user => user.Email == orderEntity.AwardedByEmail, cancellationToken);

        var supplierAddress = supplier is null
            ? Array.Empty<string>()
            : new[] { supplier.AddressLine, supplier.Town, supplier.County, supplier.Postcode }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

        var siteAddress = project is null
            ? Array.Empty<string>()
            : new[] { project.AddressLine, project.Town, project.Postcode }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToArray();

        return new WorkOrderPoDocumentModel(
            order,
            lines,
            supplier?.CompanyName ?? "(unknown supplier)",
            supplier?.ContactName ?? "",
            supplierAddress,
            project?.Name ?? "",
            siteAddress,
            approver?.DisplayName ?? "",
            supplier?.PaymentTermsDays ?? 30);
    }
}
