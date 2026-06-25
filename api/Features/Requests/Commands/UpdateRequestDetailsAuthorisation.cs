using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class UpdateRequestDetailsAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateRequests =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Architect);
    public bool Allows(SignedInUser user, UpdateRequestDetails command) => RolesThatMayUpdateRequests.IncludesAny(user.Roles);
}
