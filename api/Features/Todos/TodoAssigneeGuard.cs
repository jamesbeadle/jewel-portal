using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos;

// The person half of an assignment is only ever a CURRENT holder of the assigned role — that is
// what lets a pinned item fall back to the role (rather than orphan) when the person moves on,
// because the directory commands clear pins the moment the hold ends. Checked against the
// directory at write time here, in every path that sets an assignee; the sync validations only
// check the shape (a person needs a role), because they have no database.
internal static class TodoAssigneeGuard
{
    // Blank-to-null plus trim, so "" from a picker and null from a hand-rolled request store the
    // same unpinned value.
    public static string? NormalisePersonEmail(string? personEmail) =>
        string.IsNullOrWhiteSpace(personEmail) ? null : personEmail.Trim();

    public static async Task EnsurePersonHoldsRoleAsync(
        JpmsContext context, Role? assigneeRole, string? personEmail, CancellationToken cancellationToken)
    {
        var email = NormalisePersonEmail(personEmail);
        if (email is null) return;

        if (assigneeRole is not Role role)
            throw new InvalidOperationException(
                "A to-do can only be pinned to a person together with their role.");

        var holdsRole = await context.DirectoryUserRoles.AsNoTracking()
            .AnyAsync(row => row.DirectoryUserEmail.ToLower() == email.ToLower() && row.Role == (int)role,
                cancellationToken);
        if (!holdsRole)
            throw new InvalidOperationException(
                $"{email} doesn't hold the {role} role in the directory, so this item can't be pinned to them.");

        // A revoked user KEEPS their role rows (so a restore puts them back as they were), but a
        // person who cannot sign in must not become a pin target — revocation unpins their items,
        // and this is what stops a new pin from being created a moment later.
        var isActive = await context.DirectoryUsers.AsNoTracking()
            .AnyAsync(row => row.Email.ToLower() == email.ToLower() && row.RevokedAt == null,
                cancellationToken);
        if (!isActive)
            throw new InvalidOperationException(
                $"{email} no longer has access (revoked), so this item can't be pinned to them.");
    }
}
