using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RaiseRequestAuthorisation
{
    private static readonly RoleSet RolesThatMayRaiseRequests =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect, JpmsRoles.Subcontractor);
    public bool Allows(SignedInUser user, RaiseRequest command) => RolesThatMayRaiseRequests.IncludesAny(user.Roles);
}
