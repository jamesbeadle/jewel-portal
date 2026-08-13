using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class SetExpectedMonthlyValuationAuthorisation
{
    // Same gate as SetNextValuationDate — the two are the same kind of fact (a forecast
    // assumption about the project), edited from the same places.
    private static readonly RoleSet RolesThatMayUpdateProjects =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SetExpectedMonthlyValuation command) =>
        RolesThatMayUpdateProjects.IncludesAny(user.Roles);
}
