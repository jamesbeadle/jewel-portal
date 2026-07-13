using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Auth;
using Jewel.JPMS.Contracts.Auth;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Invites a subcontractor's contact to the portal: mints the standard set-password invite (via
/// UserInviter) and links the directory user to the subcontractor record, which is what scopes
/// their session to their own company's data (see Gates/SubcontractorScope).
/// </summary>
public sealed class SubcontractorPortalInviter
{
    private readonly JpmsContext context;
    private readonly UserInviter inviter;

    public SubcontractorPortalInviter(JpmsContext context, UserInviter inviter)
    {
        this.context = context;
        this.inviter = inviter;
    }

    public sealed record Outcome(InviteResult? Result, string? Error, int StatusCode);

    public async Task<Outcome> InviteAsync(
        string subcontractorId, string? emailOverride, string? displayNameOverride, string baseUrl,
        CancellationToken cancellationToken)
    {
        var subcontractor = await context.Subcontractors
            .FirstOrDefaultAsync(row => row.SubcontractorId == subcontractorId, cancellationToken);
        if (subcontractor is null)
            return new Outcome(null, "Subcontractor not found.", StatusCodes.Status404NotFound);

        var email = FirstNonBlank(emailOverride, subcontractor.ContactEmail)?.Trim() ?? "";
        if (!LooksLikeEmail(email))
            return new Outcome(null, "The record has no valid contact email. Provide one to invite.", StatusCodes.Status400BadRequest);

        var displayName = (FirstNonBlank(displayNameOverride, subcontractor.ContactName, subcontractor.CompanyName) ?? email).Trim();

        // One login maps to exactly one subcontractor. Re-inviting the same link is fine (fresh
        // invite link); an email already linked elsewhere is a conflict the admin must resolve.
        var existing = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        if (existing?.SubcontractorId is { Length: > 0 } linked
            && !string.Equals(linked, subcontractorId, StringComparison.OrdinalIgnoreCase))
            return new Outcome(null, "That email is already linked to a different subcontractor.", StatusCodes.Status409Conflict);

        // UserInviter replaces the directory user's roles, so preserve any the user already holds.
        var roles = (await context.DirectoryUserRoles
                .Where(row => row.DirectoryUserEmail == email)
                .Select(row => (Role)row.Role)
                .ToListAsync(cancellationToken))
            .Append(Role.Subcontractor)
            .Distinct()
            .ToList();

        var result = await inviter.InviteAsync(email, displayName, roles, baseUrl, cancellationToken);

        var directoryUser = await context.DirectoryUsers
            .FirstAsync(row => row.Email == email, cancellationToken);
        directoryUser.SubcontractorId = subcontractorId;
        await context.SaveChangesAsync(cancellationToken);

        return new Outcome(result, null, StatusCodes.Status200OK);
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static bool LooksLikeEmail(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@') && value.IndexOf('@') < value.LastIndexOf('.');
}
