using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Record-level scoping for the client portal, the client twin of SubcontractorScope. Role checks
/// (RoleSet) answer "may this kind of user do this?"; this gate answers "which client's data may
/// they touch?". Every /client-portal/my/* endpoint must resolve the caller's own ClientId through
/// here and filter by it — never trust an id supplied in the route or body.
/// </summary>
public static class ClientScope
{
    /// <summary>
    /// The caller's own client id, or null if the caller is not a portal-scoped client (wrong
    /// role, or a Role.Client login that was never linked to a client account). Callers must
    /// treat null as Forbid.
    /// </summary>
    public static string? OwnClientId(SignedInUser user)
    {
        if (!user.Roles.Contains(Role.Client)) return null;
        return string.IsNullOrWhiteSpace(user.ClientId) ? null : user.ClientId;
    }
}
