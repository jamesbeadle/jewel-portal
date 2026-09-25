using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Record-level scoping for architect logins, the architect twin of ClientScope. Role checks
/// (RoleSet) answer "may this kind of user do this?"; this gate answers "which projects may they
/// touch?". An architect's projects are the ones an administrator gave their LOGIN in Admin →
/// Users (ProjectAccessGrants, 2026-09-25), so every request, variation and instruction an
/// architect reads or writes resolves the caller's own login through here and checks the record's
/// project is one it was given (Features/Architects/ArchitectProjects) — never trust an id
/// supplied in the route or body.
/// </summary>
public static class ArchitectScope
{
    /// <summary>
    /// The caller's own login when the caller holds the Architect role, else null. A login given
    /// no projects reaches nothing, because it holds no grant.
    /// </summary>
    public static string? OwnArchitectLogin(SignedInUser user) =>
        user.Roles.Contains(Role.Architect) ? user.Email : null;
}
