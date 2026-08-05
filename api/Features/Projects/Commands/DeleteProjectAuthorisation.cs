using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class DeleteProjectAuthorisation
{
    // Deliberately narrower than UpdateProjectDetails: deleting a project erases its records
    // wholesale, so project managers are not on this list — directors (and administrators, who
    // carry every role) only.
    private static readonly RoleSet RolesThatMayDeleteProjects =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public bool Allows(SignedInUser user, DeleteProject command) =>
        RolesThatMayDeleteProjects.IncludesAny(user.Roles);
}
