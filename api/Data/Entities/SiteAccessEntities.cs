using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A QR poster's link onto one folder of a project's document register. TokenHash is the SHA-256
/// of the secret in the URL, which is stored nowhere; a scan is resolved by hashing what arrived
/// and looking this up. Revoked, expired and unknown all answer alike to the scanner.
/// </summary>
[Index(nameof(TokenHash), IsUnique = true, Name = "IX_SiteDrawingLinks_TokenHash")]
[Index(nameof(ProjectId), Name = "IX_SiteDrawingLinks_ProjectId")]
public sealed class SiteDrawingLinkEntity
{
    [Key, MaxLength(64)] public string SiteDrawingLinkId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(64)]      public string DrawingFolderId { get; set; } = "";
    public bool IncludeSubFolders { get; set; }
    [MaxLength(SiteDrawingLinkLimits.LabelLength)] public string Label { get; set; } = "";
    [MaxLength(64)]      public string TokenHash { get; set; } = "";
    [MaxLength(256)]     public string CreatedByEmail { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public int ScanCount { get; set; }
    public DateTimeOffset? LastScannedAt { get; set; }
}
