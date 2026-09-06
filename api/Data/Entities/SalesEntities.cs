using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A sales strategy (see Jewel.JPMS.Models.SalesStrategy, 2026-09-06): a methodology for finding
// leads, written down with its justification — who, where, why, the evidence, the channel, the
// pitch — and the approach plan Claude drafts from those fields. Leads point at it through
// LeadEntity.StrategyId (no FK: a retired strategy keeps its leads, which are its evidence).
public sealed class SalesStrategyEntity
{
    [Key, MaxLength(64)] public string StrategyId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    public int Audience { get; set; }
    [MaxLength(512)]     public string TargetArea { get; set; } = "";
    [MaxLength(4000)]    public string Hypothesis { get; set; } = "";
    [MaxLength(4000)]    public string Evidence { get; set; } = "";
    public int Channel { get; set; }
    [MaxLength(1024)]    public string Proposition { get; set; } = "";
    // Markdown; nvarchar(max) — a plan can run to a few pages.
    public string ApproachPlan { get; set; } = "";
    public DateTimeOffset? PlanGeneratedAt { get; set; }
    public int Status { get; set; }
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// One touch on a lead — its timeline (see Jewel.JPMS.Models.LeadActivity). Stage moves write
// one too, so the timeline is the whole history of the lead in one list.
public sealed class LeadActivityEntity
{
    [Key, MaxLength(64)] public string LeadActivityId { get; set; } = "";
    [MaxLength(64)]      public string LeadId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(4000)]    public string Summary { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    [MaxLength(256)]     public string RecordedByEmail { get; set; } = "";
}
