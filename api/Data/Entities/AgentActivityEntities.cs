using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One agent run. Append-only — never updated, never deleted.
///
/// <para>Written for every run whether it succeeded or not: a sweep that errored is exactly the
/// thing you need to see. Costs are stored as computed pence rather than derived on read, so
/// changing the rate in configuration cannot rewrite what a run actually cost.</para>
/// </summary>
public sealed class AgentActivityEntity
{
    [Key, MaxLength(64)] public string ActivityId { get; set; } = "";
    [MaxLength(64)] public string AgentKey { get; set; } = "";
    public int Trigger { get; set; }

    [MaxLength(256)] public string ActorEmail { get; set; } = "";
    public bool IsAutonomous { get; set; }

    [MaxLength(128)] public string Action { get; set; } = "";
    public int Outcome { get; set; }
    [MaxLength(1024)] public string Summary { get; set; } = "";

    [MaxLength(64)] public string? ConversationId { get; set; }
    [MaxLength(64)] public string? ProjectId { get; set; }
    [MaxLength(64)] public string? RecordReference { get; set; }
    [MaxLength(512)] public string? Route { get; set; }

    [MaxLength(512)] public string? ToolsUsed { get; set; }

    public int DurationMs { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal CostPence { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
