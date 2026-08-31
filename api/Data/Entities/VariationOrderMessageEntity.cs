using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// One entry in a variation order's in-app conversation (see Jewel.JPMS.Models.VariationOrderMessage).
// Mirrors RequestMessageEntity's typed-message shape, without the mailbox columns: a variation's
// email correspondence stays in the live tagged mailbox (the detail page's Communications section),
// so this table only ever holds messages typed in JPMS — internal notes and the shared thread the
// client portal reads and writes.
public sealed class VariationOrderMessageEntity
{
    [Key, MaxLength(64)] public string MessageId { get; set; } = "";
    [MaxLength(64)]      public string VariationOrderId { get; set; } = "";
    [MaxLength(256)]     public string AuthorEmail { get; set; } = "";
    [MaxLength(256)]     public string AuthorName { get; set; } = "";
    [MaxLength(4000)]    public string Body { get; set; } = "";
    public int Visibility { get; set; }
    public DateTimeOffset PostedAt { get; set; }

    // The message this one replies to; null for a top-level message. Replies nest freely.
    [MaxLength(64)] public string? ParentMessageId { get; set; }
}
