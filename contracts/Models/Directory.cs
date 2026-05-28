namespace Jewel.JPMS.Models;

public enum AuthProvider
{
    Microsoft,
    Google
}

public sealed record DirectoryUser(
    string Email,
    string DisplayName,
    IReadOnlyList<Role> Roles);

public sealed record AccessRequest(
    string Email,
    string DisplayName,
    AuthProvider Provider,
    DateTimeOffset RequestedAt);
