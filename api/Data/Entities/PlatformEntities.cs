using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// The announced app version — a single row (AppVersionId is always "current") that Admin → System
/// bumps and VersionStampMiddleware reports on every response. A row rather than configuration
/// because publishing an update is an act with an author and a time, and the row records all three.
/// </summary>
public sealed class AppVersionEntity
{
    [Key, MaxLength(64)] public string AppVersionId { get; set; } = "";
    public long Version { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    [MaxLength(256)]     public string PublishedBy { get; set; } = "";
}
