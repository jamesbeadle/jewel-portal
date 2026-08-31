using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// The portal's one shared Bluebeam Studio connection (row id is always "bluebeam"). Bluebeam's
/// API only works in user context — an admin signs a Studio account in once from Admin →
/// Integrations and every extraction runs through it. Tokens live here rather than in memory
/// because refresh tokens rotate on use: whichever app refreshed last must be the one whose copy
/// survives, and the api and worker share only the database. The refresh token dies after 7 days
/// unused, so the worker exercises it nightly and stamps the outcome columns the admin page shows.
/// </summary>
public sealed class BluebeamConnectionEntity
{
    [Key, MaxLength(64)] public string BluebeamConnectionId { get; set; } = "";
    [MaxLength(2048)]    public string RefreshToken { get; set; } = "";
    [MaxLength(2048)]    public string? AccessToken { get; set; }
    public DateTimeOffset? AccessTokenExpiresAt { get; set; }
    [MaxLength(256)]     public string ConnectedEmail { get; set; } = "";
    [MaxLength(256)]     public string ConnectedBy { get; set; } = "";
    public DateTimeOffset ConnectedAt { get; set; }
    public DateTimeOffset RefreshTokenUpdatedAt { get; set; }
    public DateTimeOffset? LastRefreshSucceededAt { get; set; }
    public DateTimeOffset? LastRefreshFailedAt { get; set; }
    [MaxLength(1024)]    public string? LastRefreshError { get; set; }
}

/// <summary>
/// One drawing revision's extraction — the pipeline status plus, once it succeeds, the shape of
/// what came out. One row per revision (re-extraction overwrites in place); the raw payloads —
/// Bluebeam's markups JSON verbatim and the PdfPig text layer — live as blobs under the revision's
/// own key prefix in the drawings container, with only their refs here. ProjectId/DrawingId are
/// denormalised so the register's bulk queries never join through the revision.
/// </summary>
public sealed class DrawingExtractionEntity
{
    [Key, MaxLength(64)] public string DrawingExtractionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingRevisionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";

    // DrawingExtractionStatus: 0 Queued, 1 Running, 2 Succeeded, 3 Failed.
    public int Status { get; set; }
    [MaxLength(256)]     public string QueuedBy { get; set; } = "";
    public DateTimeOffset QueuedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int Attempts { get; set; }
    [MaxLength(2048)]    public string? ErrorMessage { get; set; }

    // Results — null until the run succeeds.
    public int? PageCount { get; set; }
    // Per-page geometry as JSON: [{"page":1,"widthPoints":…,"heightPoints":…,"rotation":0}, …].
    public string? PagesJson { get; set; }
    public int? MarkupCount { get; set; }
    [MaxLength(1024)]    public string? MarkupsBlobRef { get; set; }
    [MaxLength(1024)]    public string? TextBlobRef { get; set; }
    // The Studio session the run used — diagnostics only; the session is finalised and deleted.
    [MaxLength(128)]     public string? BluebeamSessionId { get; set; }
}

/// <summary>
/// One markup from a drawing revision's extraction, normalised for the data view and for diffing
/// two revisions later (Bluebeam's own markup id is the anchor a comparison joins on). The parse
/// is deliberately lossy-safe: whatever fields Bluebeam returns, the whole markup object is kept
/// verbatim in RawJson, so a field the parser missed is recoverable without re-extracting.
/// </summary>
public sealed class DrawingMarkupEntity
{
    [Key, MaxLength(64)] public string DrawingMarkupId { get; set; } = "";
    [MaxLength(64)]      public string DrawingExtractionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingRevisionId { get; set; } = "";
    [MaxLength(128)]     public string BluebeamMarkupId { get; set; } = "";
    public int PageNumber { get; set; }
    [MaxLength(64)]      public string MarkupType { get; set; } = "";
    [MaxLength(256)]     public string Subject { get; set; } = "";
    [MaxLength(256)]     public string Author { get; set; } = "";
    [MaxLength(4000)]    public string Comment { get; set; } = "";
    [MaxLength(32)]      public string Colour { get; set; } = "";
    // Bluebeam's own timestamps, kept as returned — their format is theirs to define.
    [MaxLength(64)]      public string CreatedAtRaw { get; set; } = "";
    [MaxLength(64)]      public string ModifiedAtRaw { get; set; } = "";
    [Column(TypeName = "decimal(18,4)")]
    public decimal? MeasurementValue { get; set; }
    [MaxLength(32)]      public string? MeasurementUnit { get; set; }
    // Bounding box as returned, JSON.
    [MaxLength(512)]     public string? RectJson { get; set; }
    public string RawJson { get; set; } = "";
}
