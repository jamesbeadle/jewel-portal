using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A global architect practice (see Jewel.JPMS.Models.Architect), managed separately from client
// accounts. Projects and requests reference an architect through their party link (PartyKind /
// PartyId) when Jewel works through an architect on a client's behalf; the contact email here is
// where RFIs and other request documents are addressed in that case.
public sealed class ArchitectEntity
{
    [Key, MaxLength(64)] public string ArchitectId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string? ContactName { get; set; }
    [MaxLength(256)]     public string? ContactEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
