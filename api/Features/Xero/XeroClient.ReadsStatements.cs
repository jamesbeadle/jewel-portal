using System.Globalization;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    private const string CashValidationUrl = "https://api.xero.com/finance.xro/1.0/CashValidation";
    private const string CreditBalance = "CREDIT";
    private const string CashValidationScopeHint =
        "Xero would not give the bank statement balances — the Xero custom connection needs the "
        + "finance.cashvalidation.read scope ticked in the Xero developer portal (and the Xero__TenantId "
        + "app setting, if Xero asks for the organisation). Until then, key the statement balance by hand. ";

    /// <summary>
    /// Every bank account's statement balance by account id, from Xero's Finance API cash
    /// validation — what the bank feed says the account holds, reconciled or not, unlike the bank
    /// summary report's "Balance in Xero". A refusal is not a failed read: the bank summary
    /// still stands, and the reason comes back for the page to show.
    /// </summary>
    private async Task<(IReadOnlyDictionary<string, BankStatementBalance> Statements, string? Error)>
        FetchStatementBalancesAsync(string token, CancellationToken ct)
    {
        try
        {
            using var document = await GetJsonAsync(token, CashValidationUrl, "cash validation", ct);
            return (ReadStatementBalances(document.RootElement), null);
        }
        catch (XeroCallFailedException refusal)
        {
            return (new Dictionary<string, BankStatementBalance>(), CashValidationScopeHint + refusal.Message);
        }
    }

    private static IReadOnlyDictionary<string, BankStatementBalance> ReadStatementBalances(JsonElement accounts)
    {
        var statements = new Dictionary<string, BankStatementBalance>(StringComparer.OrdinalIgnoreCase);
        if (accounts.ValueKind != JsonValueKind.Array) return statements;

        foreach (var account in accounts.EnumerateArray())
        {
            var accountId = StringOf(account, "accountId");
            var statement = StatementOf(account);
            if (accountId is null || statement is null) continue;
            statements[accountId] = statement;
        }
        return statements;
    }

    private static BankStatementBalance? StatementOf(JsonElement account)
    {
        if (!account.TryGetProperty("statementBalance", out var balance) || balance.ValueKind != JsonValueKind.Object)
            return null;
        var statementDateText = StringOf(account, "statementBalanceDate");
        var isDated = DateTimeOffset.TryParse(statementDateText, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var statementDate);
        if (!isDated) return null;

        var balanceType = StringOf(balance, "type");
        var isInCredit = string.Equals(balanceType, CreditBalance, StringComparison.OrdinalIgnoreCase);
        var value = DecimalOf(balance, "value");
        var statementMoment = statementDate.UtcDateTime;
        var calendarDate = new DateTimeOffset(statementMoment.Date, TimeSpan.Zero);
        return new BankStatementBalance(isInCredit ? -value : value, calendarDate, BankStatementSource.Xero);
    }
}
