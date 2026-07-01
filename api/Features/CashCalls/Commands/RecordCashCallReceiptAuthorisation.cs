using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CashCalls;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

public sealed class RecordCashCallReceiptAuthorisation
{
    public bool Allows(SignedInUser user, RecordCashCallReceipt command) =>
        CashCallRoles.AllowedToManageCashCalls.IncludesAny(user.Roles);
}
