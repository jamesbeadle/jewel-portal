using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>The text a reference document's save replaced — the reference twin of
/// SkillRevisionEntity, so a reference edit is never destructive either. Append-only.</summary>
[Index(nameof(SkillKey), nameof(RefKey), nameof(Version), Name = "IX_SkillReferenceRevisions_SkillKey_RefKey_Version")]
public sealed class SkillReferenceRevisionEntity
{
    [Key, MaxLength(64)] public string SkillReferenceRevisionId { get; set; } = "";
    [MaxLength(128)] public string SkillKey { get; set; } = "";
    [MaxLength(128)] public string RefKey { get; set; } = "";
    /// <summary>The version this text WAS.</summary>
    public int Version { get; set; }
    [MaxLength(256)] public string DisplayName { get; set; } = "";
    [MaxLength(2000)] public string Description { get; set; } = "";
    public string Body { get; set; } = "";
    [MaxLength(256)] public string WrittenByEmail { get; set; } = "";
    public DateTimeOffset WrittenAt { get; set; }
    public DateTimeOffset ReplacedAt { get; set; }
}
