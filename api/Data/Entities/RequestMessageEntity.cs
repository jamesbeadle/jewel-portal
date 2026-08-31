using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class RequestMessageEntity
{
    [Key, MaxLength(64)] public string MessageId { get; set; } = "";
    [MaxLength(64)]      public string RequestId { get; set; } = "";
    [MaxLength(256)]     public string AuthorEmail { get; set; } = "";
    [MaxLength(256)]     public string AuthorName { get; set; } = "";
    [MaxLength(4000)]    public string Body { get; set; } = "";
    public int Visibility { get; set; }
    public DateTimeOffset PostedAt { get; set; }

    // The in-app message this one replies to. Null for a top-level message (and for every email
    // leg — replies thread between typed messages only; the mailbox keeps its own threading in
    // InReplyTo/ConversationId below). Replies may themselves be replied to, nesting freely.
    [MaxLength(64)] public string? ParentMessageId { get; set; }

    // Mailbox automation metadata. Direction/SentStatus default to 0 (System/NotApplicable)
    // for in-app messages; the threading identifiers are populated only for emailed legs.
    public int Direction { get; set; }
    [MaxLength(450)] public string? EmailMessageId { get; set; }
    [MaxLength(998)] public string? InReplyTo { get; set; }
    [MaxLength(998)] public string? ConversationId { get; set; }
    public int SentStatus { get; set; }
}
