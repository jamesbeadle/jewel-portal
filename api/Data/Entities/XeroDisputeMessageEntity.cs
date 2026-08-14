using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One message in a disputed Xero ledger line's discussion — the director and
/// the accountant talking a contested cost through without leaving the
/// allocation page (2026-08-14). Owned by JPMS like the allocation itself, so
/// syncs never touch it. The thread deliberately survives resolution: a line
/// disputed a second time continues the same conversation, which is the
/// history both sides would want in front of them.
/// </summary>
public sealed class XeroDisputeMessageEntity
{
    [Key, MaxLength(64)]  public string XeroDisputeMessageId { get; set; } = "";
    [MaxLength(140)]      public string XeroLedgerLineId { get; set; } = "";
    // The signed-in user's email, stamped server-side (same rule as AllocatedBy).
    [MaxLength(256)]      public string Author { get; set; } = "";
    [MaxLength(2048)]     public string Body { get; set; } = "";
    public DateTimeOffset SentAtUtc { get; set; }
}
