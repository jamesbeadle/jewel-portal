using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Procurement;

/// <summary>
/// The project references a work order's reference is qualified by (see
/// <see cref="WorkOrderReferences"/>) — read once per project, never per order.
/// </summary>
internal static class WorkOrderProjectReferences
{
    /// <summary>The project's human reference (e.g. "JBB-2026-001"), or null when unset.</summary>
    public static async Task<string?> OfAsync(JpmsContext context, string projectId, CancellationToken cancellationToken) =>
        await context.Projects.AsNoTracking()
            .Where(project => project.ProjectId == projectId)
            .Select(project => project.Reference)
            .FirstOrDefaultAsync(cancellationToken);

    public static async Task<WorkOrder> ModelOfAsync(JpmsContext context, WorkOrderEntity order, CancellationToken cancellationToken) =>
        order.ToModel(await OfAsync(context, order.ProjectId, cancellationToken));
}
