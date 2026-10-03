using System.ComponentModel.DataAnnotations;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>A bank account's statement balance as a director keyed it — one row per Xero bank
/// account, replaced by keying again. Company-wide, string-keyed, no FKs: the account itself
/// lives in Xero and is read live.</summary>
public sealed class KeyedBankStatementBalanceEntity
{
    [Key, MaxLength(BankStatementBalanceLimits.AccountIdLength)] public string AccountId { get; set; } = "";
    [MaxLength(BankStatementBalanceLimits.AccountNameLength)] public string AccountName { get; set; } = "";
    public decimal Balance { get; set; }
    public DateTimeOffset StatementDate { get; set; }
    [MaxLength(BankStatementBalanceLimits.EmailLength)] public string KeyedByEmail { get; set; } = "";
    public DateTimeOffset KeyedAt { get; set; }

    public KeyedBankStatementBalance ToModel() =>
        new(AccountId, AccountName, Balance, StatementDate, KeyedByEmail, KeyedAt);
}
