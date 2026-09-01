using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.ClientPortal;

/// <summary>
/// Which projects — and therefore which records — belong to a signed-in client. A project is the
/// client's when they are its party directly (PartyKind Client) or when an architect corresponds
/// on their behalf (OnBehalfOfClientId). Lead-stage projects are unsold work and never appear.
/// Every client-portal read and write filters through here; a record outside the scope reads as
/// "not found", indistinguishable from not existing.
/// </summary>
internal static class ClientProjects
{
    public static IQueryable<ProjectEntity> For(JpmsContext context, string clientId) =>
        context.Projects
            .AsNoTracking()
            .Where(project => project.Stage != (int)ProjectStage.Lead)
            .Where(project =>
                (project.PartyKind == (int)PartyKind.Client && project.PartyId == clientId)
                || project.OnBehalfOfClientId == clientId);

    /// <summary>Variations the client can see at all: everything from Issued onward. Quoting is
    /// internal pricing work — the order hasn't reached the client yet.</summary>
    public static IQueryable<VariationOrderEntity> VisibleVariationOrders(JpmsContext context) =>
        context.VariationOrders
            .AsNoTracking()
            .Where(order => order.Status != (int)VariationOrderStatus.Quoting);

    // A merged-away request is gone from the client's view everywhere, not just the list —
    // its surviving twin carries the conversation on.
    public static async Task<bool> OwnsRequestAsync(
        JpmsContext context, string clientId, string requestId, CancellationToken cancellationToken) =>
        await context.Requests
            .AsNoTracking()
            .Where(request => request.RequestId == requestId && request.MergedIntoRequestId == null)
            .Join(For(context, clientId),
                request => request.ProjectId, project => project.ProjectId,
                (request, project) => request.RequestId)
            .AnyAsync(cancellationToken);

    public static async Task<bool> OwnsVariationOrderAsync(
        JpmsContext context, string clientId, string variationOrderId, CancellationToken cancellationToken) =>
        await VisibleVariationOrders(context)
            .Where(order => order.VariationOrderId == variationOrderId)
            .Join(For(context, clientId),
                order => order.ProjectId, project => project.ProjectId,
                (order, project) => order.VariationOrderId)
            .AnyAsync(cancellationToken);
}
