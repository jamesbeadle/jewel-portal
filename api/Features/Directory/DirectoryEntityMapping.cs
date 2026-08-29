using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Directory;

internal static class DirectoryEntityMapping
{
    public static DirectoryUser ToModel(this DirectoryUserEntity entity, IReadOnlyList<Role> roles) => new(
        Email: entity.Email,
        DisplayName: entity.DisplayName,
        Roles: roles,
        RevertToOwnRole: entity.RevertToOwnRole);

    /// <summary>Only meaningful for rows with RevokedAt set — the revoked-users read filters on
    /// that before mapping, so the bang never fires on an active row.</summary>
    public static RevokedDirectoryUser ToRevokedModel(this DirectoryUserEntity entity, IReadOnlyList<Role> roles) => new(
        Email: entity.Email,
        DisplayName: entity.DisplayName,
        Roles: roles,
        RevokedAt: entity.RevokedAt!.Value,
        RevokedBy: entity.RevokedBy ?? "");
}
