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

    // ---- The brief + AI research (added the same day, AddSalesStrategyResearch) ----
    // The idea in the team's own words; the research reads this first.
    [MaxLength(4000)]    public string Brief { get; set; } = "";
    public int ResearchStatus { get; set; }
    public DateTimeOffset? ResearchRequestedAt { get; set; }
    public DateTimeOffset? ResearchCompletedAt { get; set; }
    [MaxLength(2000)]    public string? ResearchError { get; set; }
    // Markdown findings with sources; nvarchar(max).
    public string ResearchFindings { get; set; } = "";
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

// One imagine round (see Jewel.JPMS.Models.ImagineRoundView): the prospect's submission — photos
// and brief, or a chosen concept and what to change — and the render's state. The photos and
// concepts are ImagineImageEntity rows pointing at it; their bytes live in the "imagine" blob
// container. No FKs, as everywhere else.
public sealed class ImagineRoundEntity
{
    [Key, MaxLength(64)] public string RoundId { get; set; } = "";
    [MaxLength(64)]      public string LeadId { get; set; } = "";
    // 1-based per lead.
    public int Number { get; set; }
    public int Kind { get; set; }
    [MaxLength(4000)]    public string Brief { get; set; } = "";
    [MaxLength(64)]      public string? BasedOnImageId { get; set; }
    public int Status { get; set; }
    [MaxLength(2000)]    public string? Error { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    // What Claude saw in the photos, shown above the concepts; nvarchar(max).
    public string Observations { get; set; } = "";
    // Who submitted it, as typed on the page (the lead's contact fields are updated from these
    // when blank).
    [MaxLength(256)]     public string ProspectName { get; set; } = "";
    [MaxLength(256)]     public string ProspectEmail { get; set; } = "";
    // SHA-256 of the submitting IP — the per-address throttle, never the address itself.
    [MaxLength(64)]      public string? ClientHash { get; set; }
}

// One image on a round: an uploaded photo or a rendered concept. BlobRef is the key in the
// "imagine" container; Prompt is the image-generation prompt a concept was rendered from
// (kept so a revision can build on it and so the team can see what was asked for).
public sealed class ImagineImageEntity
{
    [Key, MaxLength(64)] public string ImageId { get; set; } = "";
    [MaxLength(64)]      public string LeadId { get; set; } = "";
    [MaxLength(64)]      public string RoundId { get; set; } = "";
    public int Kind { get; set; }
    public int Order { get; set; }
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(2000)]    public string Description { get; set; } = "";
    public string Prompt { get; set; } = "";
    [MaxLength(512)]     public string BlobRef { get; set; } = "";
    [MaxLength(128)]     public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool Liked { get; set; }
    [MaxLength(2000)]    public string Comment { get; set; } = "";
}

// A proposal on a lead (see Jewel.JPMS.Models.SalesProposal): scope, base price, options,
// schedule of works and terms, versioned; acceptance is recorded on the row. Options and phases
// are JSON columns (nvarchar(max)) — small, read whole, never queried.
public sealed class SalesProposalEntity
{
    [Key, MaxLength(64)] public string ProposalId { get; set; } = "";
    [MaxLength(64)]      public string LeadId { get; set; } = "";
    public int Version { get; set; }
    [MaxLength(256)]     public string Title { get; set; } = "";
    public string Scope { get; set; } = "";
    public decimal BasePrice { get; set; }
    public string OptionsJson { get; set; } = "[]";
    public string ScheduleJson { get; set; } = "[]";
    public string Terms { get; set; } = "";
    [MaxLength(64)]      public string? HeroImageId { get; set; }
    public int Status { get; set; }
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    [MaxLength(256)]     public string? AcceptedByName { get; set; }
    [MaxLength(256)]     public string? AcceptedByEmail { get; set; }
    public string AcceptedOptionIdsJson { get; set; } = "[]";
    public decimal? AcceptedPrice { get; set; }
    // SHA-256 of the accepting IP, with the moment — the acceptance record.
    [MaxLength(64)]      public string? AcceptedClientHash { get; set; }
    public DateTimeOffset? DeclinedAt { get; set; }
    [MaxLength(1024)]    public string? DeclineReason { get; set; }
}
