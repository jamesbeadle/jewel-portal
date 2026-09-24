namespace Jewel.JPMS.Api.Features.Ai.Tools;

public static partial class AiToolCatalogue
{
    private static async Task<string?> FindWorkOrdersByNumberAsync(
        AiToolContext context, int number, string? projectReference, CancellationToken ct)
    {
        var orders = await context.Db.WorkOrders.AsNoTracking()
            .Where(row => row.Number == number)
            .ToListAsync(ct);
        var projects = await ProjectReferenceMapAsync(context, orders.Select(row => row.ProjectId), ct);
        var named = projectReference is null
            ? orders
            : orders.Where(row => string.Equals(projects.GetValueOrDefault(row.ProjectId), projectReference, StringComparison.OrdinalIgnoreCase)).ToList();
        if (named.Count == 0) return null;

        return Serialise(new
        {
            ok = true,
            kind = "work_order",
            matches = named.Select(row => new
            {
                reference = row.ReferenceOn(projects.GetValueOrDefault(row.ProjectId)),
                row.WorkOrderId,
                row.Title,
                status = ((WorkOrderStatus)row.Status).ToString(),
                row.Value,
                project = projects.TryGetValue(row.ProjectId, out var orderProject) ? orderProject : row.ProjectId,
                projectId = row.ProjectId,
                route = $"/projects/{row.ProjectId}/work-orders"
            }),
            note = (named.Count > 1
                    ? "The number is on more than one project — say which, by its full reference, before acting on one. "
                    : "")
                + "get_work_order_context reads the order's origin, lines and attachments; "
                + "read_record_emails record_type work_order reads its correspondence; the "
                + "work_order_edit dialog corrects it."
        });
    }
}
