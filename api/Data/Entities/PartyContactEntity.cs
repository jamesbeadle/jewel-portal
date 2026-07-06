using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A person at a client account or architect practice — the party's communication preferences.
/// DefaultRouting (a CorrespondenceRouting) is how they join outbound request correspondence on
/// every project corresponding with the party; a project can override it per contact through a
/// linked ProjectContactEntity row. The IsPrimary contact is the party's To correspondent and
/// supersedes ClientEntity.PrimaryContactEmail / ArchitectEntity.ContactEmail (kept in step until
/// those fields are retired).
/// Lives in the api project's entity folder, which the worker links into its own compilation, so
/// both Function apps share one definition.
/// </summary>
public sealed class PartyContactEntity
{
    [Key, MaxLength(64)] public string PartyContactId { get; set; } = "";
    public int PartyKind { get; set; }
    [MaxLength(64)]      public string PartyId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string Email { get; set; } = "";
    [MaxLength(256)]     public string? JobTitle { get; set; }
    public int DefaultRouting { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
