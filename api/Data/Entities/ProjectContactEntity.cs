using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// An external party attached to a project (client, architect, consultant, engineer). Request
/// documents are emailed to the contacts whose <see cref="ReceivesRequests"/> flag is set.
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
    public DateTimeOffset CreatedAt { get; set; }
}
