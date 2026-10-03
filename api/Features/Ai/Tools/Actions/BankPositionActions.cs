using Jewel.JPMS.Api.Features.Xero.Commands;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The bank position as connector actions (2026-10-03). Cash in bank is each account's
/// statement balance — what the bank says it holds — never Xero's own balance, which counts
/// payments not yet reconciled. Xero gives the statement balance when the connection may read it;
/// otherwise a director keys it here.
/// </summary>
public sealed class BankPositionActions : IAiActionSource
{
    public IEnumerable<AiAction> Build() => new[]
    {
        new AiAction(
            Name: "key_bank_statement_balance",
            Area: "Cashflow",
            Description: "Keys one Xero bank account's statement balance by hand — the figure the "
                + "bank's statement (or Xero's dashboard, 'Statement balance') shows on a date. It "
                + "becomes that account's Cash in bank until Xero reports a newer statement; keying "
                + "again replaces it.",
            CommandType: typeof(KeyBankStatementBalance),
            ResultType: typeof(KeyedBankStatementBalance),
            AuthorisationType: typeof(KeyBankStatementBalanceAuthorisation),
            ValidationType: typeof(KeyBankStatementBalanceValidation),
            VisibleTo: BankPositionGates.BankPositionRoles,
            EmailStamps: new[] { "KeyedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "accountId and accountName are the account's as get_weekly_cashflow_grid or the "
                + "cash summary report them. balance is in pounds to the penny, negative when "
                + "overdrawn; statementDate is the statement's day (yyyy-MM-dd). Read the figure back "
                + "to the user before keying it.")
    };
}
