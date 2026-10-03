using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Commands;

public sealed class KeyBankStatementBalanceAuthorisation
{
    public bool Allows(SignedInUser user, KeyBankStatementBalance command) =>
        BankPositionGates.Admits(user.Roles);
}
