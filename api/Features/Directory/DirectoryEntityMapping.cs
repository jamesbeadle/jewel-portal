using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Directory;

internal static class DirectoryEntityMapping
{
    public static DirectoryUser ToModel(this DirectoryUserEntity entity, IReadOnlyList<Role> roles) => new(
        Email: entity.Email,
        DisplayName: entity.DisplayName,
        Roles: roles);
}
