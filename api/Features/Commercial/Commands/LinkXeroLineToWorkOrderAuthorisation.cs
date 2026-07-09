using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class LinkXeroLineToWorkOrderAuthorisation
{
    // Same commercial roles as the Financials tab's other inputs.
    private static readonly RoleSet RolesThatMayLink =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, LinkXeroLineToWorkOrder command) =>
        RolesThatMayLink.IncludesAny(user.Roles);
}
