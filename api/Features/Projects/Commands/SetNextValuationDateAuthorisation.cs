using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class SetNextValuationDateAuthorisation
{
    // Same gate as UpdateProjectDetails — the next valuation date is part of a project's details.
    private static readonly RoleSet RolesThatMayUpdateProjects =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SetNextValuationDate command) =>
        RolesThatMayUpdateProjects.IncludesAny(user.Roles);
}
