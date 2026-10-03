using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Contracts.Xero;
using static Jewel.JPMS.Contracts.Xero.BankStatementBalanceLimits;

namespace Jewel.JPMS.Api.Features.Xero.Commands;

/// <summary>Holds a keyed statement balance to its columns: the account named within its
/// lengths, the balance in whole pence and inside the stored range, and the statement dated a
/// calendar day no later than today — a statement cannot come from tomorrow.</summary>
public sealed class KeyBankStatementBalanceValidation
{
    public ValidationOutcome Check(KeyBankStatementBalance command)
    {
        var errors = new List<string>();
        CheckText(errors, command.AccountId, "accountId", AccountIdLength);
        CheckText(errors, command.AccountName, "accountName", AccountNameLength);
        CheckBalance(errors, command.Balance);
        CheckStatementDate(errors, command.StatementDate);
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }

    private static void CheckText(List<string> errors, string? value, string field, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) { errors.Add($"{field} is required."); return; }
        if (value.Length > maximumLength) errors.Add($"{field} is too long ({maximumLength} characters at most).");
    }

    private static void CheckBalance(List<string> errors, decimal balance)
    {
        var isWholePence = decimal.Round(balance, PenceDecimalPlaces) == balance;
        if (!isWholePence) errors.Add($"balance must be in whole pence ({PenceDecimalPlaces} decimal places at most).");
        if (Math.Abs(balance) > LargestBalance) errors.Add($"balance is beyond ±{LargestBalance:N0}.");
    }

    private static void CheckStatementDate(List<string> errors, DateTimeOffset statementDate)
    {
        var isCalendarDate = statementDate.Offset == TimeSpan.Zero && statementDate.TimeOfDay == TimeSpan.Zero;
        if (!isCalendarDate) errors.Add("statementDate must be a calendar date (midnight UTC).");
        if (statementDate > SiteClock.Today()) errors.Add("statementDate cannot be later than today.");
    }
}
