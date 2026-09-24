using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// Builds one supplier's account on one project from the register (2026-09-14, the accountant's
/// ask): the supplier's live orders there with their lines, invoiced-and-linked and paid positions
/// (the same link slices and CIS-safe paid maths the Work orders and WO Allocation tabs read), and
/// every bill received from the supplier for the project — linked, allocated or still pending —
/// with its labour / materials split by Xero account. One partial per half: Orders, Invoices.
/// </summary>
public sealed partial class GetProjectSupplierAccountHandler
    : IQueryHandler<GetProjectSupplierAccount, ProjectSupplierAccount>
{
    private readonly JpmsContext context;
    private readonly XeroOptions xeroOptions;

    public GetProjectSupplierAccountHandler(JpmsContext context, XeroOptions xeroOptions)
    {
        this.context = context;
        this.xeroOptions = xeroOptions;
    }

    public async Task<ProjectSupplierAccount> HandleAsync(
        GetProjectSupplierAccount query, CancellationToken cancellationToken)
    {
        var project = await context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ProjectId == query.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {query.ProjectId} not found.");
        var supplier = await context.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SubcontractorId == query.SubcontractorId, cancellationToken)
            ?? throw new InvalidOperationException($"Supplier {query.SubcontractorId} not found.");

        var orders = await LiveOrdersAsync(query, cancellationToken);
        var orderIds = orders.Select(order => order.WorkOrderId).ToList();
        var linesByOrder = await LinesByOrderAsync(orderIds, cancellationToken);
        var links = await LinksAsync(orderIds, cancellationToken);
        var paidByOrder = await WorkOrderPaidPositions.ForProjectAsync(context, query.ProjectId, cancellationToken);

        var search = new SupplierBillSearch(
            query.ProjectId,
            supplier,
            orders.Select(order => order.Number).ToHashSet(),
            links.Select(link => link.XeroLedgerLineId).ToHashSet(StringComparer.OrdinalIgnoreCase),
            await IsSupplierOnlyHereAsync(query, cancellationToken));
        var bills = await new SupplierBillFinder(context).FindAsync(search, cancellationToken);
        var referencesByOrder = orders.ToDictionary(order => order.WorkOrderId, order => order.ReferenceOn(project.Reference), StringComparer.OrdinalIgnoreCase);

        var invoices = bills
            .Select(bill => BuildInvoice(bill, links, referencesByOrder))
            .OrderBy(invoice => invoice.Date ?? DateTime.MaxValue)
            .ThenBy(invoice => invoice.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var accountOrders = orders
            .Select(order => BuildOrder(order, referencesByOrder[order.WorkOrderId], linesByOrder, links, invoices, paidByOrder))
            .ToList();

        return new ProjectSupplierAccount(
            project.ProjectId,
            project.Reference,
            project.Name,
            supplier.SubcontractorId,
            supplier.CompanyName,
            DateTimeOffset.UtcNow,
            await LedgerSyncedAtAsync(cancellationToken),
            accountOrders,
            invoices);
    }

    // When JPMS last heard from Xero at all — the paid figures and statuses are as current as this.
    private Task<DateTimeOffset?> LedgerSyncedAtAsync(CancellationToken cancellationToken) =>
        context.XeroLedgerLines.AsNoTracking()
            .MaxAsync(line => (DateTimeOffset?)line.LastSyncedAtUtc, cancellationToken);

    // Live orders anywhere else mean an unplaced bill could be another project's; none means an
    // unplaced bill of this supplier's can only be this project's.
    private async Task<bool> IsSupplierOnlyHereAsync(GetProjectSupplierAccount query, CancellationToken cancellationToken)
    {
        var otherProjectCount = await context.WorkOrders.AsNoTracking()
            .Where(order => order.SubcontractorId == query.SubcontractorId
                            && order.ProjectId != query.ProjectId
                            && LiveStatuses.Contains(order.Status))
            .Select(order => order.ProjectId)
            .Distinct()
            .CountAsync(cancellationToken);
        return otherProjectCount == 0;
    }

    private static readonly int[] LiveStatuses = { (int)WorkOrderStatus.Released, (int)WorkOrderStatus.Complete };
}
