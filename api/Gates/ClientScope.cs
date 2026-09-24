using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Record-level scoping for a client login, the client twin of ArchitectScope. Role checks
/// (RoleSet) answer "may this kind of user do this?"; this gate answers "which client's data may
/// they touch?". Every read or write a client makes resolves the caller's own ClientId through
/// here (Features/Parties/PartyReads, VariationOrderScope) — never trust an id supplied in the
/// route or body.
/// </summary>
public static class ClientScope
{
    /// <summary>
    /// The caller's own client id, or null if the caller is not a linked client (wrong
    /// role, or a Role.Client login that was never linked to a client account). Callers must
    /// treat null as Forbid.
    /// </summary>
    public static string? OwnClientId(SignedInUser user)
    {
        if (!JpmsRoleSets.ClientLogins.IncludesAny(user.Roles)) return null;
        return string.IsNullOrWhiteSpace(user.ClientId) ? null : user.ClientId;
    }
}
