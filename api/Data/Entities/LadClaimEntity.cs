using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A Liquidated Damages (LADs) claim the client has notified against the project — the client-side
// counterpart to Jewel's own NOD/EOT notices on the Schedule tab's Claims view. The sequential
// Number renders as "LAD-0001", which doubles as the mailbox tag stem ("JPMS/LAD-0001") — the link
// between a claim and its emails is the tag, never a stored copy (same mechanism as to-do items).
public sealed class LadClaimEntity
{
    [Key, MaxLength(64)] public string LadClaimId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2048)]    public string Description { get; set; } = "";
    public DateTimeOffset? PeriodFrom { get; set; }
    public DateTimeOffset? PeriodTo { get; set; }
    public int DaysClaimed { get; set; }
    public decimal RatePerWeek { get; set; }
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public DateTimeOffset RaisedAt { get; set; }
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";

    // Sequential, human-readable claim number (rendered as LAD-0001). Global — like request and
    // to-do numbers — so the tag stem is unique across the flat JPMS mailbox-category space.
    public int Number { get; set; }

    // The canonical reference this claim's emails are tagged with ("LAD-0001" -> "JPMS/LAD-0001").
    // Computed, not stored.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => $"LAD-{Number:0000}";
}
