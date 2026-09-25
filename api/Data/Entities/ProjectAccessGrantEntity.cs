using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A project an external login may see (2026-09-25, Nigel: an architect's projects are given to
/// their login in Admin → Users, not assembled through the Directory). One row per login and
/// project; the architect's reads are confined to the projects their login holds a grant for
/// (Features/Architects/ArchitectProjects). No FKs, as everywhere else.
/// </summary>
[Index(nameof(Email), nameof(ProjectId), IsUnique = true, Name = "IX_ProjectAccessGrants_Email_ProjectId")]
public sealed class ProjectAccessGrantEntity
{
    [Key, MaxLength(64)] public string ProjectAccessGrantId { get; set; } = "";
    [MaxLength(256)]     public string Email { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string GrantedByEmail { get; set; } = "";
    public DateTimeOffset GrantedAt { get; set; }
}
