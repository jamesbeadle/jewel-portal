using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Rates;

namespace Jewel.JPMS.Api.Features.Rates.Commands;

public sealed class ReviseRateAuthorisation
{
    private static readonly RoleSet RolesThatMayReviseRates =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, ReviseRate command) => RolesThatMayReviseRates.IncludesAny(user.Roles);
}
