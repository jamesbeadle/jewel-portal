namespace Jewel.JPMS.Api.Features.Portal;

/// <summary>
/// Which projects a signed-in subcontractor is on: the ones where they hold a work order that
/// has been issued — the same orders their portal lists (ListMyWorkOrders). A tenderer with no
/// order on a project is not on it, whatever they have quoted.
/// </summary>
internal static class SubcontractorProjects
{
    public static async Task<bool> HoldsWorkOnAsync(
        JpmsContext context, string subcontractorId, string projectId, CancellationToken cancellationToken) =>
        await context.WorkOrders
            .AsNoTracking()
            .AnyAsync(order => order.SubcontractorId == subcontractorId
                && order.ProjectId == projectId
                && order.Status != (int)WorkOrderStatus.Draft
                && order.Status != (int)WorkOrderStatus.Rejected
                && order.Status != (int)WorkOrderStatus.Cancelled, cancellationToken);
}
