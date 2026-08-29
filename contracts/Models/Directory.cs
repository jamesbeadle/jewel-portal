namespace Jewel.JPMS.Models;

public sealed record DirectoryUser(
    string Email,
    string DisplayName,
    IReadOnlyList<Role> Roles,
    // When true, a "Viewing as" switch to another role is temporary: after two hours the app
    // defaults back to this user's own role (HomeRoleSelection.From). Off by default — most
    // users would find the revert prompt an interruption; it exists for people (the FD above
    // all) whose Administrator view kept "sticking" across days.
    bool RevertToOwnRole = false);

/// <summary>
/// A user whose access has been revoked. Their directory record survives — with the roles they
/// held, so a restore puts them back exactly as they were — but they cannot sign in and they
/// appear in no active-user list. RevokedBy is the administrator who revoked them (empty when
/// unknown, e.g. rows revoked before this was recorded).
/// </summary>
public sealed record RevokedDirectoryUser(
    string Email,
    string DisplayName,
    IReadOnlyList<Role> Roles,
    DateTimeOffset RevokedAt,
    string RevokedBy);

public sealed record AccessRequest(
    string Email,
    string DisplayName,
    DateTimeOffset RequestedAt);
