using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// A new starter's pack: one link for several forms. Only the token's hash is stored, so a copy
/// of the database is not a set of working links. String-keyed with no FKs, as everywhere else.
/// </summary>
[Index(nameof(TokenHash), IsUnique = true, Name = "IX_FormPacks_TokenHash")]
public sealed class FormPackEntity
{
    [Key, MaxLength(64)] public string FormPackId { get; set; } = "";
    [MaxLength(256)]     public string PersonName { get; set; } = "";
    [MaxLength(256)]     public string Email { get; set; } = "";
    public int EngagedAs { get; set; }
    public bool HasP45 { get; set; }
    public bool IsWorkingAtAScreen { get; set; }
    public bool IsGettingAVehicle { get; set; }
    public bool MustHoldATicket { get; set; }
    [MaxLength(64)]      public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    [MaxLength(256)]     public string SentByEmail { get; set; } = "";
    [MaxLength(256)]     public string SentByName { get; set; } = "";
    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? LastChasedAt { get; set; }
    public int ChaseCount { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

/// <summary>One form sent to one named person. The invite row is the evidential record of who it was for.</summary>
[Index(nameof(TokenHash), IsUnique = true, Name = "IX_FormInvites_TokenHash")]
[Index(nameof(FormPackId), Name = "IX_FormInvites_FormPackId")]
public sealed class FormInviteEntity
{
    [Key, MaxLength(64)] public string FormInviteId { get; set; } = "";
    [MaxLength(64)]      public string? FormPackId { get; set; }
    [MaxLength(64)]      public string FormSlug { get; set; } = "";
    [MaxLength(256)]     public string PersonName { get; set; } = "";
    [MaxLength(256)]     public string CompanyName { get; set; } = "";
    [MaxLength(256)]     public string Email { get; set; } = "";
    [MaxLength(64)]      public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    [MaxLength(256)]     public string SentByEmail { get; set; } = "";
    [MaxLength(256)]     public string SentByName { get; set; } = "";
    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    [MaxLength(64)]      public string? FormSubmissionId { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    [MaxLength(512)]     public string Reason { get; set; } = "";
    [MaxLength(64)]      public string? PolicyDocumentId { get; set; }
}
