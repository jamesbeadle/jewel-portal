using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class SaveWeeklyCashflowSupplierGroupAuthorisation
{
    public bool Allows(SignedInUser user, SaveWeeklyCashflowSupplierGroup command) =>
        WeeklyCashflowGates.WeeklyCashflowRoles.IncludesAny(user.Roles);
}
