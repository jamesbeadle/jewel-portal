using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Architects;

/// <summary>
/// Which projects — and therefore which records — belong to a signed-in architect: the projects
/// that name their practice as the party (PartyKind Architect, PartyId the practice; Nigel's
/// decision, 2026-09-19), and the projects whose client is the party but that list one of the
/// practice's people among their contacts (Project settings → Contacts, a row linked to the
/// practice's contact book; 2026-09-24 — most projects correspond with the client, and the
/// architect is on them all the same). Lead-stage projects are unsold work and never appear, as
/// for a client. Every architect-scoped read and write filters through here; a record outside the
/// scope is refused.
/// </summary>
internal static class ArchitectProjects
{
    public static async Task<bool> OwnsProjectAsync(
        JpmsContext context, string architectId, string projectId, CancellationToken cancellationToken) =>
        await For(context, architectId)
            .AnyAsync(project => project.ProjectId == projectId, cancellationToken);

    public static async Task<bool> OwnsRequestAsync(
        JpmsContext context, string architectId, string requestId, CancellationToken cancellationToken) =>
        await context.Requests
            .AsNoTracking()
            .Where(request => request.RequestId == requestId)
            .Join(For(context, architectId),
                request => request.ProjectId, project => project.ProjectId,
                (request, project) => request.RequestId)
            .AnyAsync(cancellationToken);

    public static async Task<bool> OwnsVariationOrderAsync(
        JpmsContext context, string architectId, string variationOrderId, CancellationToken cancellationToken) =>
        await context.VariationOrders
            .AsNoTracking()
            .Where(order => order.VariationOrderId == variationOrderId)
            .Join(For(context, architectId),
                order => order.ProjectId, project => project.ProjectId,
                (order, project) => order.VariationOrderId)
            .AnyAsync(cancellationToken);

    public static async Task<bool> OwnsInstructionAsync(
        JpmsContext context, string architectId, string instructionId, CancellationToken cancellationToken) =>
        await context.ArchitectInstructions
            .AsNoTracking()
            .Where(instruction => instruction.ArchitectInstructionId == instructionId)
            .Join(For(context, architectId),
                instruction => instruction.ProjectId, project => project.ProjectId,
                (instruction, project) => instruction.ArchitectInstructionId)
            .AnyAsync(cancellationToken);

    public static IQueryable<ProjectEntity> For(JpmsContext context, string architectId)
    {
        var practiceContactIds = context.PartyContacts
            .Where(contact => contact.PartyKind == (int)PartyKind.Architect && contact.PartyId == architectId)
            .Select(contact => contact.PartyContactId);
        var projectsNamingThePractice = context.ProjectContacts
            .Where(contact => contact.PartyContactId != null && practiceContactIds.Contains(contact.PartyContactId))
            .Select(contact => contact.ProjectId);
        return context.Projects
            .AsNoTracking()
            .Where(project => project.Stage != (int)ProjectStage.Lead)
            .Where(project => (project.PartyKind == (int)PartyKind.Architect && project.PartyId == architectId)
                || projectsNamingThePractice.Contains(project.ProjectId));
    }
}
