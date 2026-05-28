using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class CreateProjectShellAuthorisation
{
    private static readonly RoleSet RolesThatMayCreateProjects =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, CreateProjectShell command) =>
        RolesThatMayCreateProjects.IncludesAny(user.Roles);
}
