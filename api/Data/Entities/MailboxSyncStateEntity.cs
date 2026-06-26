using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// Durable state for the projects@ mailbox ingestion layer. Single-row table keyed by mailbox
// address. Holds the Graph /messages/delta cursor (so the sweep is incremental and the one-time
// backlog import only runs once) and the current change-notification subscription id + expiry
// (so the renewal timer can PATCH it before it lapses and recreate it if it has gone).
public sealed class MailboxSyncStateEntity
{
    [Key, MaxLength(256)] public string Mailbox { get; set; } = "";

    // Opaque Graph deltaLink used as the cursor for the next incremental sweep.
    // Null until the first (backlog) sweep completes.
    public string? DeltaLink { get; set; }

    public DateTimeOffset? LastSyncedAt { get; set; }

    // Set true once the initial full-inbox backlog import has run, so it never repeats.
    public bool BacklogImported { get; set; }

    [MaxLength(450)] public string? SubscriptionId { get; set; }
    public DateTimeOffset? SubscriptionExpiresAt { get; set; }
}
