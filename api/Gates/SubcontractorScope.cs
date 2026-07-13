using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Record-level scoping for the subcontractor portal. Role checks (RoleSet) answer "may this kind
/// of user do this?"; this gate answers "which subcontractor's data may they touch?". Every
/// /portal/my/* endpoint must resolve the caller's own SubcontractorId through here and filter by
/// it — never trust an id supplied in the route or body.
/// </summary>
public static class SubcontractorScope
{
    /// <summary>
    /// The caller's own subcontractor id, or null if the caller is not a portal-scoped
    /// subcontractor (wrong role, or a Role.Subcontractor login that was never linked to a
    /// directory record). Callers must treat null as Forbid.
    /// </summary>
    public static string? OwnSubcontractorId(SignedInUser user)
    {
        if (!user.Roles.Contains(Role.Subcontractor)) return null;
        return string.IsNullOrWhiteSpace(user.SubcontractorId) ? null : user.SubcontractorId;
    }
}
