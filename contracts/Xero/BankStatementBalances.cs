using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Xero;

/// <summary>
/// One bank account's balance from Xero's bank summary report, in the organisation's base
/// currency (GBP). <see cref="Balance"/> is the report's closing balance as of today — "Balance
/// in Xero", unreconciled payments included. <see cref="Statement"/> is what the bank says the
/// account holds, when Xero or a director has told the portal; it is the cash in the bank.
/// </summary>
public sealed record XeroBankAccountBalance(
    string AccountId,
    string Name,
    decimal Balance,
    BankStatementBalance? Statement = null)
{
    public bool HasStatement => Statement is not null;

    public decimal CashInBank => Statement?.Balance ?? Balance;

    public DateTimeOffset? StatementDate => Statement?.StatementDate;

    /// <summary>The account with the candidate statement in place of its own when the candidate
    /// is at least as new — a person's word stands against Xero's on the same day.</summary>
    public XeroBankAccountBalance WithNewerStatement(BankStatementBalance candidate)
    {
        var isCandidateNewer = Statement is null || candidate.StatementDate >= Statement.StatementDate;
        return isCandidateNewer ? this with { Statement = candidate } : this;
    }

    public XeroBankAccountBalance WithKeyedStatement(IReadOnlyDictionary<string, KeyedBankStatementBalance> keyedByAccount) =>
        keyedByAccount.TryGetValue(AccountId, out var keyed) ? WithNewerStatement(keyed.AsStatement()) : this;
}

/// <summary>A keyed statement balance as stored, with the account it belongs to.</summary>
public sealed record KeyedBankStatementBalance(
    string AccountId,
    string AccountName,
    decimal Balance,
    DateTimeOffset StatementDate,
    string KeyedByEmail,
    DateTimeOffset KeyedAt)
{
    public BankStatementBalance AsStatement() =>
        new(Balance, StatementDate, BankStatementSource.Keyed, KeyedByEmail);
}

/// <summary>Where a bank account's statement balance came from: Xero's own record of the bank
/// feed, or a director who keyed it in from the bank or Xero's dashboard.</summary>
public enum BankStatementSource
{
    Xero = 0,
    Keyed = 1
}

/// <summary>
/// What the bank itself says an account holds — the statement balance — as of a date. The
/// opposite of Xero's own "Balance in Xero", which counts every payment and receipt coded in
/// Xero whether or not it has reached the bank (2026-10-02: Nigel's Lloyds account read
/// £5,718.66 on the statement against £107,899.50 in Xero, 33 items unreconciled). The
/// statement balance is the cash in the bank. StatementDate is a UK calendar date at midnight UTC.
/// </summary>
public sealed record BankStatementBalance(
    decimal Balance,
    DateTimeOffset StatementDate,
    BankStatementSource Source,
    string? KeyedByEmail = null);

/// <summary>
/// Keys an account's statement balance by hand — for an account whose statement balance Xero
/// will not give the portal, or one whose feed is behind the bank. The newest statement date
/// wins, so a keyed figure stands until Xero (or a director) reports a newer one. One row per
/// account: keying again replaces it. KeyedByEmail is stamped server-side from the signed-in user.
/// </summary>
public sealed record KeyBankStatementBalance(
    string AccountId,
    string AccountName,
    decimal Balance,
    DateTimeOffset StatementDate,
    string KeyedByEmail = "") : ICommand<KeyedBankStatementBalance>;

/// <summary>The limits a keyed statement balance is held to — read by the service's check,
/// the stored columns and the form alike, so they cannot drift apart.</summary>
public static class BankStatementBalanceLimits
{
    public const int AccountIdLength = 128;
    public const int AccountNameLength = 200;
    public const int EmailLength = 256;
    public const int PenceDecimalPlaces = 2;
    public const decimal LargestBalance = 99_999_999_999_999m;
}
