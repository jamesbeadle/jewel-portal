using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// One row of a project's correspondence profile. Two shapes:
///  - linked (<see cref="PartyContactId"/> set): a per-project routing override for a person on
///    the corresponding party's contact book — name/email read through from the PartyContact so
///    party-level edits propagate to every project;
///  - ad-hoc (<see cref="PartyContactId"/> null): a recipient owned by this project alone, e.g.
///    an internal Jewel staff member CC'd on issued documents.
/// <see cref="Routing"/> (a CorrespondenceRouting) drives outbound sends: To rows are the fallback
/// correspondent when no party link resolves; Cc/Bcc rows are copied on every issue.
/// <see cref="ReceivesRequests"/> is the legacy flag, kept equal to (Routing == To) on write until
/// it is dropped.
/// Lives in the api project's entity folder, which the worker links into its own compilation, so
/// both Function apps share one definition.
/// </summary>
public sealed class ProjectContactEntity
{
    [Key, MaxLength(64)] public string ContactId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string Email { get; set; } = "";
    [MaxLength(256)]     public string? Organisation { get; set; }
    public int Role { get; set; }
    public bool ReceivesRequests { get; set; }
    public int Routing { get; set; }
    [MaxLength(64)]      public string? PartyContactId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
