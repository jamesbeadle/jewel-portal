using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

/// <summary>
/// Why an email cannot be invited to the architect portal. One login maps to exactly one
/// practice and one portal: re-inviting the same link is fine (a fresh invite link); an email
/// already linked elsewhere, or scoped to another portal, or a staff login, is a conflict the
/// office must resolve;
/// and a revoked user is never quietly resurrected by an invite that is not admin-gated.
/// </summary>
internal static class ArchitectInviteRefusals
{
    public static string? For(DirectoryUserEntity? existing, IReadOnlyList<Role> heldRoles, string architectId)
    {
        if (existing is null) return null;
        if (LoginRoles.IncludeStaff(heldRoles))
            return "That email is a Jewel staff login. An architect's login needs the architect's own address.";
        if (IsLinkedToAnotherPractice(existing, architectId))
            return "That email is already linked to a different architect practice.";
        if (existing.ClientId is { Length: > 0 })
            return "That email belongs to a client's login. Use a different address for the architect's contact.";
        if (existing.SubcontractorId is { Length: > 0 })
            return "That email belongs to a subcontractor portal login. Use a different address for the architect's contact.";
        if (existing.RevokedAt is not null)
            return "That email belongs to a user whose access was revoked. An administrator must restore (or permanently delete) them first.";
        return null;
    }

    private static bool IsLinkedToAnotherPractice(DirectoryUserEntity existing, string architectId) =>
        existing.ArchitectId is { Length: > 0 } linked
        && !string.Equals(linked, architectId, StringComparison.OrdinalIgnoreCase);
}
