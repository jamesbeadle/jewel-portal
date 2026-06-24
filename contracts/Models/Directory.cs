namespace Jewel.JPMS.Models;

public sealed record DirectoryUser(
    string Email,
    string DisplayName,
    IReadOnlyList<Role> Roles);

public sealed record AccessRequest(
    string Email,
    string DisplayName,
    DateTimeOffset RequestedAt);
