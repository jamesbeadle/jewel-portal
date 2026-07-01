using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CashCalls;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

public sealed class IssueClientInvoiceAuthorisation
{
    public bool Allows(SignedInUser user, IssueClientInvoice command) =>
        CashCallRoles.AllowedToManageCashCalls.IncludesAny(user.Roles);
}
