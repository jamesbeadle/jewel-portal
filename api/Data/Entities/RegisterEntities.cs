using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One row of a company register (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §8 —
/// the Monday replacement): insurances, subscriptions, vans, trade accounts. Kind mirrors the
/// contracts RegisterKind enum; the dated fields drive renewal visibility. Rows deactivate,
/// never delete — a lapsed policy is history, not noise.
/// </summary>
public sealed class CompanyRegisterItemEntity
{
    [Key, MaxLength(64)] public string RegisterItemId { get; set; } = "";
    public int Kind { get; set; }
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string Counterparty { get; set; } = "";
    [MaxLength(128)]     public string Reference { get; set; } = "";
    [MaxLength(256)]     public string OwnerEmail { get; set; } = "";
    public decimal Cost { get; set; }
    [MaxLength(64)]      public string BillingCycle { get; set; } = "";
    public DateTimeOffset? KeyDate { get; set; }
    public DateTimeOffset? SecondaryDate { get; set; }
    [MaxLength(2048)]    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>A published staff document requiring acknowledgement. A new revision of the same
/// title is a NEW row (Revision + 1) with fresh sign-off rows — the old revision's evidence
/// stays intact.</summary>
public sealed class PolicyDocumentEntity
{
    [Key, MaxLength(64)] public string PolicyDocumentId { get; set; } = "";
    [MaxLength(256)]     public string Title { get; set; } = "";
    [MaxLength(4096)]    public string Summary { get; set; } = "";
    public int Revision { get; set; } = 1;
    [MaxLength(256)]     public string PublishedByEmail { get; set; } = "";
    public DateTimeOffset PublishedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>One recipient's acknowledgement of one policy revision: requested, then signed with
/// a typed name and a server timestamp — the drawing-approval evidential pattern.</summary>
public sealed class PolicySignOffEntity
{
    [Key, MaxLength(64)] public string PolicySignOffId { get; set; } = "";
    [MaxLength(64)]      public string PolicyDocumentId { get; set; } = "";
    [MaxLength(256)]     public string RecipientEmail { get; set; } = "";
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? SignedAt { get; set; }
    [MaxLength(256)]     public string SignedName { get; set; } = "";
}
