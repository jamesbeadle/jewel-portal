using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Clients;

/// <summary>
/// Which projects — and therefore which records — belong to a signed-in client. A project is the
/// client's when they are its party directly (PartyKind Client) or when an architect corresponds
/// on their behalf (OnBehalfOfClientId). Lead-stage projects are unsold work and never appear.
/// Every read and write a client makes filters through here (Parties/PartyReads,
/// VariationOrderScope, DefectScope); a record outside the scope reads as "not found",
/// indistinguishable from not existing.
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

    /// <summary>Variations a client can act on at all: everything from Issued onward. Quoting is
    /// internal pricing work — the order hasn't reached the client yet.</summary>
    public static IQueryable<VariationOrderEntity> VisibleVariationOrders(JpmsContext context) =>
        context.VariationOrders
            .AsNoTracking()
            .Where(order => order.Status != (int)VariationOrderStatus.Quoting);

    public static async Task<bool> OwnsProjectAsync(
        JpmsContext context, string clientId, string projectId, CancellationToken cancellationToken) =>
        await For(context, clientId)
            .AnyAsync(project => project.ProjectId == projectId, cancellationToken);

    public static async Task<bool> OwnsVariationOrderAsync(
        JpmsContext context, string clientId, string variationOrderId, CancellationToken cancellationToken) =>
        await VisibleVariationOrders(context)
            .Where(order => order.VariationOrderId == variationOrderId)
            .Join(For(context, clientId),
                order => order.ProjectId, project => project.ProjectId,
                (order, project) => order.VariationOrderId)
            .AnyAsync(cancellationToken);
}
