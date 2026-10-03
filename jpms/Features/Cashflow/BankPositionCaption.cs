namespace Jewel.JPMS.Features.Cashflow;

/// <summary>What the Cash in bank figure is made of, in a line under it: the bank statements
/// and their date, or how many accounts still stand on Xero's own balance — which counts
/// payments and receipts not yet reconciled to the bank.</summary>
public static class BankPositionCaption
{
    public static string For(XeroCashSummarySnapshot bank)
    {
        var accounts = bank.BankAccounts;
        var accountsOnXeroBalance = accounts.Count(account => !account.HasStatement);
        var oldestStatementDate = accounts.Min(account => account.StatementDate);
        if (accountsOnXeroBalance == 0 && oldestStatementDate is { } statementDate)
            return $"Bank statement, {statementDate.UtcDateTime:d MMM}";
        if (accountsOnXeroBalance == accounts.Count)
            return "Xero's balance — unreconciled items included";
        return $"{accountsOnXeroBalance} of {accounts.Count} accounts on Xero's balance, unreconciled included";
    }
}
