using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Architects;

/// <summary>
/// Which projects — and therefore which records — belong to a signed-in architect: the projects an
/// administrator gave their login in Admin → Users (ProjectAccessGrants; Nigel, 2026-09-25 — the
/// relationship is kept on the login, not assembled through the Directory's practices and
/// contacts). Lead-stage projects are unsold work and never appear, as for a client. Every
/// architect-scoped read and write filters through here; a record outside the scope is refused.
/// </summary>
internal static class ArchitectProjects
{
    public static async Task<bool> OwnsProjectAsync(
        JpmsContext context, string architectLogin, string projectId, CancellationToken cancellationToken) =>
        await For(context, architectLogin)
            .AnyAsync(project => project.ProjectId == projectId, cancellationToken);

    public static async Task<bool> OwnsRequestAsync(
        JpmsContext context, string architectLogin, string requestId, CancellationToken cancellationToken) =>
        await context.Requests
            .AsNoTracking()
            .Where(request => request.RequestId == requestId)
            .Join(For(context, architectLogin),
                request => request.ProjectId, project => project.ProjectId,
                (request, project) => request.RequestId)
            .AnyAsync(cancellationToken);

    public static async Task<bool> OwnsVariationOrderAsync(
        JpmsContext context, string architectLogin, string variationOrderId, CancellationToken cancellationToken) =>
        await context.VariationOrders
            .AsNoTracking()
            .Where(order => order.VariationOrderId == variationOrderId)
            .Join(For(context, architectLogin),
                order => order.ProjectId, project => project.ProjectId,
                (order, project) => order.VariationOrderId)
            .AnyAsync(cancellationToken);

    public static async Task<bool> OwnsInstructionAsync(
        JpmsContext context, string architectLogin, string instructionId, CancellationToken cancellationToken) =>
        await context.ArchitectInstructions
            .AsNoTracking()
            .Where(instruction => instruction.ArchitectInstructionId == instructionId)
            .Join(For(context, architectLogin),
                instruction => instruction.ProjectId, project => project.ProjectId,
                (instruction, project) => instruction.ArchitectInstructionId)
            .AnyAsync(cancellationToken);

    public static IQueryable<ProjectEntity> For(JpmsContext context, string architectLogin)
    {
        var grantedProjectIds = context.ProjectAccessGrants
            .Where(grant => grant.Email == architectLogin)
            .Select(grant => grant.ProjectId);
        return context.Projects
            .AsNoTracking()
            .Where(project => project.Stage != (int)ProjectStage.Lead)
            .Where(project => grantedProjectIds.Contains(project.ProjectId));
    }
}
