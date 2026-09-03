using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// Someone KPIs are filed under (2026-09-03): a portal user (Email = their directory key, name
// snapshotted from the directory) or a person added by name alone (Email null — staff without a
// login, "James Clark"). No FK to the directory: a revoked user's KPIs survive under their name.
public sealed class KpiPersonEntity
{
    [Key, MaxLength(64)] public string KpiPersonId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// An email marked as a KPI against one person (2026-09-03). Administrators only, end to end.
// NOT a record-link: nothing in the mailbox is tagged — the row IS the mark, with the envelope
// snapshotted so the register reads without a mailbox round-trip and the ids kept so the email
// opens live on request.
public sealed class KpiEmailEntity
{
    [Key, MaxLength(64)] public string KpiEmailId { get; set; } = "";
    [MaxLength(64)]      public string PersonId { get; set; } = "";
    [MaxLength(512)]     public string MessageId { get; set; } = "";
    [MaxLength(512)]     public string? InternetMessageId { get; set; }
    [MaxLength(512)]     public string? ConversationId { get; set; }
    [MaxLength(1024)]    public string Subject { get; set; } = "";
    [MaxLength(256)]     public string FromEmail { get; set; } = "";
    [MaxLength(256)]     public string FromName { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
    [MaxLength(2048)]    public string Note { get; set; } = "";
    [MaxLength(256)]     public string MarkedByEmail { get; set; } = "";
    public DateTimeOffset MarkedAt { get; set; }

    // Sequential, human-readable number (rendered as KPI-0001). Global, like defect and to-do
    // numbers. Minted by MarkEmailAsKpiHandler — never a row count.
    public int Number { get; set; }

    // Computed, not stored. The id-derived fallback covers any unnumbered row (there should be
    // none) so two such rows can never share the "KPI-0000" reference.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string Reference => Number > 0
        ? $"KPI-{Number:0000}"
        : $"KPI-{KpiEmailId.PadRight(8, '0')[..8].ToUpperInvariant()}";
}
