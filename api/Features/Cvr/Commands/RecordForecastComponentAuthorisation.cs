using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordForecastComponentAuthorisation
{
    private static readonly RoleSet RolesThatMayRecordForecastComponents =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RecordForecastComponent command) =>
        RolesThatMayRecordForecastComponents.IncludesAny(user.Roles);
}
