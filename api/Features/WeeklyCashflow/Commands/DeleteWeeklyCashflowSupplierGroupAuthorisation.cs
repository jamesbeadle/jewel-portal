using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class DeleteWeeklyCashflowSupplierGroupAuthorisation
{
    public bool Allows(SignedInUser user, DeleteWeeklyCashflowSupplierGroup command) =>
        WeeklyCashflowGates.WeeklyCashflowRoles.IncludesAny(user.Roles);
}
