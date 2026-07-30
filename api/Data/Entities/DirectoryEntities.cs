using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

public sealed class DirectoryUserEntity
{
    [Key, MaxLength(256)] public string Email { get; set; } = "";
    [MaxLength(256)]      public string DisplayName { get; set; } = "";

    /// <summary>Set when this login belongs to an external subcontractor contact. Portal endpoints
    /// scope every read/write to this id — a Role.Subcontractor session with no link sees nothing.
    /// Null for all internal users.</summary>
    [MaxLength(64)] public string? SubcontractorId { get; set; }

    /// <summary>Set when the user's access is revoked. The row and its role rows survive — so a
    /// restore puts the user back exactly as they were — but a revoked user cannot sign in and is
    /// filtered out of every active-user read. Null = active.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>The administrator who revoked them — stamped by RemoveDirectoryUserEndpoint from
    /// the signed-in caller. Null while active.</summary>
    [MaxLength(256)] public string? RevokedBy { get; set; }
}

public sealed class DirectoryUserRoleEntity
{
    [Key, MaxLength(64)] public string DirectoryUserRoleId { get; set; } = "";
    [MaxLength(256)]     public string DirectoryUserEmail { get; set; } = "";
    public int Role { get; set; }
}

public sealed class AccessRequestEntity
{
    [Key, MaxLength(256)] public string Email { get; set; } = "";
    [MaxLength(256)]      public string DisplayName { get; set; } = "";
    public DateTimeOffset RequestedAt { get; set; }
}
