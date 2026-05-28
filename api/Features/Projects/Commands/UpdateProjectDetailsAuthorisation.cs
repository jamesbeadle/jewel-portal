using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class UpdateProjectDetailsAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateProjects =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, UpdateProjectDetails command) =>
        RolesThatMayUpdateProjects.IncludesAny(user.Roles);
}
