using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CashCalls;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

public sealed class CreateCashCallAuthorisation
{
    public bool Allows(SignedInUser user, CreateCashCall command) =>
        CashCallRoles.AllowedToManageCashCalls.IncludesAny(user.Roles);
}
