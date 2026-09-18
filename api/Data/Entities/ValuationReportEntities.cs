using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class ValuationLineItemEntity
{
    [Key, MaxLength(64)] public string ValuationLineItemId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int ElementType { get; set; }
    [MaxLength(16)]      public string SectionCode { get; set; } = "";
    [MaxLength(128)]     public string SectionName { get; set; } = "";
    [MaxLength(16)]      public string VariationRef { get; set; } = "";
    [MaxLength(256)]     public string VariationTitle { get; set; } = "";
    public int LineType { get; set; }
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(16)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal LineAmount { get; set; }
    [MaxLength(512)]     public string Comments { get; set; } = "";
    public int DisplayOrder { get; set; }
    // The client's schedule-of-works item number for THIS line ("1.03"). Line-level: beats the
    // per-cost-centre ClientCostReferences map at snapshot capture; empty falls back to the map.
    [MaxLength(64)]      public string ClientReference { get; set; } = "";
}

public sealed class ValuationClaimEntity
{
    [Key, MaxLength(64)] public string ValuationClaimId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int ClaimNumber { get; set; }
    // Free-text period name ("June 2026"); empty for pre-name claims — UI falls back to "Claim n".
    [MaxLength(128)]     public string Name { get; set; } = "";
    public DateTimeOffset ClaimDate { get; set; }
    public int Status { get; set; }
    public decimal RetentionPercent { get; set; }
    public decimal RetentionReleasePercent { get; set; }
    public DateTimeOffset? PreapprovedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public decimal ContractSum { get; set; }
    public decimal NetVariations { get; set; }
    public decimal RevisedContractSum { get; set; }
    public decimal TotalWorksComplete { get; set; }
    public decimal RetentionHeld { get; set; }
    public decimal RetentionReleased { get; set; }
    public decimal CertifiedToDate { get; set; }
    public decimal PaymentDueExVat { get; set; }
    // Cash-up-front deposit: % and opening balance stamped from the project's terms at
    // claim start (kept live on Drafts). DepositReleased is the amount deducted from this
    // claim's payment due (earned release less opening), frozen when the claim locks.
    public decimal DepositPercent { get; set; }
    public decimal DepositReleased { get; set; }
    public decimal DepositReleasedOpening { get; set; }
    // When the valuation's statement lines were frozen onto its ClaimLines (the lock — "We're
    // claiming this"). Null while Draft. Since 2026-09-18 the locked claim IS the statement the
    // client is sent: there is no separate snapshot object.
    public DateTimeOffset? LockedAt { get; set; }
}

// One line of a valuation: the % complete / money entered against a bill line, and — once the
// valuation is locked — the bill line itself copied by value (description, code, quantity, rate,
// amount, client reference, display order), so the statement the client was sent survives every
// later edit or deletion of the live bill. The copied columns are blank on a Draft, whose
// statement is computed from the live bill on each read; ValuationStatementLines writes them at
// lock and refreshes them for the lines a value-neutral variation re-breakdown re-deals under a
// locked claim (a claim locks its money, never the shape beneath it — decision 2026-09-16).
// ValuationLineItemId stays the working link to the live bill (the next claim seeds from it);
// on a locked claim it is provenance only and may name a line that no longer exists.
public sealed class ClaimLineEntity
{
    [Key, MaxLength(64)] public string ClaimLineId { get; set; } = "";
    [MaxLength(64)]      public string ValuationClaimId { get; set; } = "";
    [MaxLength(64)]      public string ValuationLineItemId { get; set; } = "";
    public decimal PercentComplete { get; set; }
    public decimal CumulativeClaimed { get; set; }
    public decimal PeriodIncrement { get; set; }
    // Frozen copy of the bill line (written at lock; blank on a Draft):
    public int ElementType { get; set; }
    [MaxLength(16)]      public string SectionCode { get; set; } = "";
    [MaxLength(128)]     public string SectionName { get; set; } = "";
    [MaxLength(16)]      public string VariationRef { get; set; } = "";
    [MaxLength(256)]     public string VariationTitle { get; set; } = "";
    public int LineType { get; set; }
    [MaxLength(32)]      public string CostCode { get; set; } = "";
    [MaxLength(512)]     public string Description { get; set; } = "";
    [MaxLength(16)]      public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal LineAmount { get; set; }
    [MaxLength(512)]     public string Comments { get; set; } = "";
    // Statement order at lock (bill order — element, variation number, display order); -1 on a
    // row not yet frozen.
    public int DisplayOrder { get; set; } = -1;
    [MaxLength(64)]      public string ClientReference { get; set; } = "";
}

// The alias register for the RETIRED valuation-report-snapshot object (consolidated into the
// claim 2026-09-18): every snapshot that existed, by its old id and its per-project number, with
// the claim it was frozen from. Not a domain object — nothing lists, tags or renders it. It is
// what keeps the old world resolving: a snapshot id in an old link or connector call opens its
// claim's statement, and a "JPMS/VRS-{project}-{Number}" mailbox tag stamped before the
// consolidation reads as the claim's correspondence (ValuationClaimLinkProvider companions).
public sealed class ValuationClaimLegacyStatementEntity
{
    [Key, MaxLength(64)] public string ValuationReportSnapshotId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string ValuationClaimId { get; set; } = "";
    public int Number { get; set; }
    [MaxLength(256)]     public string Label { get; set; } = "";
    public DateTimeOffset TakenAt { get; set; }
    public bool IsSuperseded { get; set; }
    [MaxLength(64)]      public string? ValuationInvoiceId { get; set; }
}
