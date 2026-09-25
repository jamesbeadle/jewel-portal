using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

/// <summary>
/// Gives an architect login its projects (ProjectAccessGrants): the projects named become the
/// login's projects and every other grant it held is removed. Only a login holding the Architect
/// role takes projects — the grant is what an architect's reads are confined to — and only
/// projects that exist are granted.
/// </summary>
public sealed class SetLoginProjectsHandler : ICommandHandler<SetLoginProjects, Acknowledgement>
{
    private readonly JpmsContext context;

    public SetLoginProjectsHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(SetLoginProjects command, CancellationToken cancellationToken)
    {
        await RequireAnArchitectLoginAsync(command.Email, cancellationToken);
        var wanted = await ExistingProjectIdsAsync(command.ProjectIds, cancellationToken);
        var held = await context.ProjectAccessGrants
            .Where(grant => grant.Email == command.Email)
            .ToListAsync(cancellationToken);

        context.ProjectAccessGrants.RemoveRange(held.Where(grant => !wanted.Contains(grant.ProjectId)));
        var heldProjectIds = held.Select(grant => grant.ProjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        context.ProjectAccessGrants.AddRange(wanted
            .Where(projectId => !heldProjectIds.Contains(projectId))
            .Select(projectId => NewGrant(command, projectId)));
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.Email);
    }

    private async Task RequireAnArchitectLoginAsync(string email, CancellationToken cancellationToken)
    {
        var directoryRoles = await UserRoles.DirectoryRolesAsync(context, email, cancellationToken);
        var isAnArchitectLogin = directoryRoles.Contains(Role.Architect);
        if (!isAnArchitectLogin)
            throw new InvalidOperationException("Projects are given to architect logins. Give this login the Architect role first.");
    }

    private async Task<HashSet<string>> ExistingProjectIdsAsync(
        IReadOnlyList<string> projectIds, CancellationToken cancellationToken)
    {
        var existing = await context.Projects
            .Where(project => projectIds.Contains(project.ProjectId))
            .Select(project => project.ProjectId)
            .ToListAsync(cancellationToken);
        return existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static ProjectAccessGrantEntity NewGrant(SetLoginProjects command, string projectId) => new()
    {
        ProjectAccessGrantId = DirectoryIdentifierFactory.NextProjectAccessGrantId(),
        Email = command.Email,
        ProjectId = projectId,
        GrantedByEmail = command.GrantedByEmail,
        GrantedAt = DateTimeOffset.UtcNow
    };
}
