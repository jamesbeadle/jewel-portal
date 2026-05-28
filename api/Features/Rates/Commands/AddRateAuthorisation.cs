using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Rates;

namespace Jewel.JPMS.Api.Features.Rates.Commands;

public sealed class AddRateAuthorisation
{
    private static readonly RoleSet RolesThatMayEditRates =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, AddRate command) => RolesThatMayEditRates.Includes(user.Role);
}
