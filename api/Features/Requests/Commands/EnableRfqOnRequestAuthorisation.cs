using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class EnableRfqOnRequestAuthorisation
{
    private static readonly RoleSet RolesThatMayEnableRfq =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, EnableRfqOnRequest command) =>
        RolesThatMayEnableRfq.IncludesAny(user.Roles);
}
