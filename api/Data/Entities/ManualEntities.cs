using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One module of the site manual: the working text under Status/Version, and beside it the text the
/// site sees — PublishedBody at PublishedVersion, written only by an approval. A module never
/// deletes: retiring it is Status = Superseded, and every approved version stays in ManualModuleVersions.
/// </summary>
public sealed class ManualModuleEntity
{
    [Key, MaxLength(64)] public string ManualModuleId { get; set; } = "";
    [MaxLength(16)] public string Code { get; set; } = "";
    [MaxLength(256)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string Purpose { get; set; } = "";
    public string Body { get; set; } = "";
    public string PublishedBody { get; set; } = "";
    [MaxLength(256)] public string OwnerEmail { get; set; } = "";
    [MaxLength(256)] public string ApproverEmail { get; set; } = "";
    public int Status { get; set; }
    public int Version { get; set; } = 1;
    public int PublishedVersion { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    [MaxLength(256)] public string ApprovedByEmail { get; set; } = "";
    public DateTimeOffset? NextReviewAt { get; set; }
    public bool IsForSiteManagers { get; set; } = true;
    public bool IsForHealthAndSafetyOfficer { get; set; } = true;
    public bool IsForForemen { get; set; } = true;
    [MaxLength(1000)] public string LinkedFormSlugs { get; set; } = "";
    [MaxLength(1000)] public string LinkedStandards { get; set; } = "";
    [MaxLength(2000)] public string ChangeSummary { get; set; } = "";
    [MaxLength(256)] public string SourceSections { get; set; } = "";
    public int Sequence { get; set; }
    [MaxLength(256)] public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)] public string UpdatedByEmail { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>One approved version of a module, written at the moment of approval and closed by the next.</summary>
public sealed class ManualModuleVersionEntity
{
    [Key, MaxLength(64)] public string ManualModuleVersionId { get; set; } = "";
    [MaxLength(64)] public string ManualModuleId { get; set; } = "";
    public int Version { get; set; }
    [MaxLength(256)] public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    [MaxLength(2000)] public string ChangeSummary { get; set; } = "";
    [MaxLength(256)] public string ApprovedByEmail { get; set; } = "";
    public DateTimeOffset ApprovedAt { get; set; }
    public DateTimeOffset? SupersededAt { get; set; }
}

/// <summary>One person's acknowledgement of one version of a module — a typed name and a server time.</summary>
public sealed class ManualAcknowledgementEntity
{
    [Key, MaxLength(64)] public string ManualAcknowledgementId { get; set; } = "";
    [MaxLength(64)] public string ManualModuleId { get; set; } = "";
    public int Version { get; set; }
    [MaxLength(256)] public string Email { get; set; } = "";
    [MaxLength(256)] public string TypedName { get; set; } = "";
    public DateTimeOffset AcknowledgedAt { get; set; }
}
