using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class UpdateQsAccrualAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateAccruals = RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);
    public bool Allows(SignedInUser user, UpdateQsAccrual command) => RolesThatMayUpdateAccruals.IncludesAny(user.Roles);
}
