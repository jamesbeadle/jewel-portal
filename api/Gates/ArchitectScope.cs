using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Record-level scoping for architect logins, the architect twin of ClientScope. Role checks
/// (RoleSet) answer "may this kind of user do this?"; this gate answers "which practice's
/// projects may they touch?". Every request, drawing, variation and instruction write an
/// architect may make resolves the caller's own ArchitectId through here and checks the record's
/// project names that practice (Features/Architects/ArchitectProjects) — never trust an id
/// supplied in the route or body.
/// </summary>
public static class ArchitectScope
{
    /// <summary>
    /// The caller's own architect id, or null if the caller is not a scoped architect (wrong
    /// role, or a Role.Architect login that was never linked to a practice). Callers must treat
    /// null as Forbid.
    /// </summary>
    public static string? OwnArchitectId(SignedInUser user)
    {
        if (!user.Roles.Contains(Role.Architect)) return null;
        return string.IsNullOrWhiteSpace(user.ArchitectId) ? null : user.ArchitectId;
    }
}
