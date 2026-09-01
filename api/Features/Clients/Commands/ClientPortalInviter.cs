using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Auth;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

/// <summary>
/// Invites a client's contact to the client portal: mints the standard set-password invite (via
/// UserInviter) and links the directory user to the client account, which is what scopes their
/// session to their own projects' records (see Gates/ClientScope). The client twin of
/// Subcontractors.Commands.SubcontractorPortalInviter.
/// </summary>
public sealed class ClientPortalInviter
{
    private readonly JpmsContext context;
    private readonly UserInviter inviter;

    public ClientPortalInviter(JpmsContext context, UserInviter inviter)
    {
        this.context = context;
        this.inviter = inviter;
    }

    public sealed record Outcome(InviteResult? Result, string? Error, int StatusCode);

    public async Task<Outcome> InviteAsync(
        string clientId, string? emailOverride, string? displayNameOverride, string baseUrl,
        CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .FirstOrDefaultAsync(row => row.ClientId == clientId, cancellationToken);
        if (client is null)
            return new Outcome(null, "Client not found.", StatusCodes.Status404NotFound);

        var email = FirstNonBlank(emailOverride, client.PrimaryContactEmail)?.Trim() ?? "";
        if (!LooksLikeEmail(email))
            return new Outcome(null, "The account has no valid contact email. Provide one to invite.", StatusCodes.Status400BadRequest);

        var displayName = (FirstNonBlank(displayNameOverride, client.PrimaryContactName, client.Name) ?? email).Trim();

        // One login maps to exactly one client. Re-inviting the same link is fine (fresh invite
        // link); an email already linked elsewhere is a conflict the admin must resolve.
        var existing = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        if (existing?.ClientId is { Length: > 0 } linked
            && !string.Equals(linked, clientId, StringComparison.OrdinalIgnoreCase))
            return new Outcome(null, "That email is already linked to a different client.", StatusCodes.Status409Conflict);

        // One login, one portal: an email already scoped to the subcontractor portal must not
        // quietly become a hybrid account scoped to both.
        if (existing?.SubcontractorId is { Length: > 0 })
            return new Outcome(null, "That email belongs to a subcontractor portal login. Use a different address for the client contact.", StatusCodes.Status409Conflict);

        // A revoked user's roles survive for the admin-only Restore. This path is NOT admin-gated,
        // and UserInviter would clear the revocation and re-apply every surviving role — a portal
        // invite must never quietly resurrect a revoked internal account.
        if (existing?.RevokedAt is not null)
            return new Outcome(null,
                "That email belongs to a user whose access was revoked. An administrator must restore (or permanently delete) them first.",
                StatusCodes.Status409Conflict);

        // UserInviter replaces the directory user's roles, so preserve any the user already holds.
        var roles = (await context.DirectoryUserRoles
                .Where(row => row.DirectoryUserEmail == email)
                .Select(row => (Role)row.Role)
                .ToListAsync(cancellationToken))
            .Append(Role.Client)
            .Distinct()
            .ToList();

        var result = await inviter.InviteAsync(email, displayName, roles, baseUrl, cancellationToken);

        var directoryUser = await context.DirectoryUsers
            .FirstAsync(row => row.Email == email, cancellationToken);
        directoryUser.ClientId = clientId;
        await context.SaveChangesAsync(cancellationToken);

        return new Outcome(result, null, StatusCodes.Status200OK);
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static bool LooksLikeEmail(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@') && value.IndexOf('@') < value.LastIndexOf('.');
}
