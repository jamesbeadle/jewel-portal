using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A global client account (see Jewel.JPMS.Models.Client). Architects live in their own table
// (ArchitectEntity); a project/request corresponds either with a client directly or with an
// architect acting on the client's behalf (see the PartyKind / PartyId columns there). When the
// client is the selected party, request documents are addressed to the primary contact email.
public sealed class ClientEntity
{
    [Key, MaxLength(64)] public string ClientId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string? PrimaryContactName { get; set; }
    [MaxLength(256)]     public string? PrimaryContactEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
