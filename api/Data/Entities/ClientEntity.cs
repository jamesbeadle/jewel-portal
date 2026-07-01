using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

// A global client account (see Jewel.JPMS.Models.Client). Holds the architect email used when a
// request is promoted to an RFI.
public sealed class ClientEntity
{
    [Key, MaxLength(64)] public string ClientId { get; set; } = "";
    [MaxLength(256)]     public string Name { get; set; } = "";
    [MaxLength(256)]     public string? PrimaryContactName { get; set; }
    [MaxLength(256)]     public string? PrimaryContactEmail { get; set; }
    [MaxLength(256)]     public string? ArchitectName { get; set; }
    [MaxLength(256)]     public string? ArchitectEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
