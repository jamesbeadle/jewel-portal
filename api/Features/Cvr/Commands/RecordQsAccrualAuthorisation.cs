using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordQsAccrualAuthorisation
{
    private static readonly RoleSet RolesThatMayRecordAccruals = RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);
    public bool Allows(SignedInUser user, RecordQsAccrual command) => RolesThatMayRecordAccruals.Includes(user.Role);
}
