using Jewel.JPMS.Api.Features.Auth;
using Jewel.JPMS.Contracts.Auth;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

/// <summary>
/// Invites an architect practice's contact to the portal: mints the standard set-password invite
/// (via UserInviter) and links the directory user to the practice, which is what scopes their
/// session to the projects that name it (Gates/ArchitectScope). The architect twin of
/// Clients.Commands.ClientPortalInviter.
/// </summary>
public sealed class InviteArchitectPortalUserHandler
{
    private readonly JpmsContext context;
    private readonly UserInviter inviter;
    private readonly InviteArchitectPortalUserValidation validation;

    public InviteArchitectPortalUserHandler(
        JpmsContext context, UserInviter inviter, InviteArchitectPortalUserValidation validation)
    {
        this.context = context;
        this.inviter = inviter;
        this.validation = validation;
    }

    public sealed record Outcome(InviteResult? Result, string? Error, int StatusCode);

    public async Task<Outcome> InviteAsync(
        string architectId, string? emailOverride, string? displayNameOverride, string baseUrl,
        CancellationToken cancellationToken)
    {
        var architect = await context.Architects
            .FirstOrDefaultAsync(row => row.ArchitectId == architectId, cancellationToken);
        if (architect is null)
            return new Outcome(null, "Architect not found.", StatusCodes.Status404NotFound);

        var email = FirstNonBlank(emailOverride, architect.ContactEmail)?.Trim() ?? "";
        var complaint = validation.Complaint(email);
        if (complaint is not null) return new Outcome(null, complaint, StatusCodes.Status400BadRequest);
        var displayName = (FirstNonBlank(displayNameOverride, architect.ContactName, architect.Name) ?? email).Trim();

        var existing = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        var refusal = ArchitectInviteRefusals.For(existing, architectId);
        if (refusal is not null) return new Outcome(null, refusal, StatusCodes.Status409Conflict);

        var result = await inviter.InviteAsync(
            email, displayName, await RolesKeptPlusArchitectAsync(email, cancellationToken), baseUrl, cancellationToken);
        await LinkToPracticeAsync(email, architectId, cancellationToken);
        return new Outcome(result, null, StatusCodes.Status200OK);
    }

    private async Task<IReadOnlyList<Role>> RolesKeptPlusArchitectAsync(string email, CancellationToken cancellationToken)
    {
        var held = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);
        return held.Append(Role.Architect).Distinct().ToList();
    }

    private async Task LinkToPracticeAsync(string email, string architectId, CancellationToken cancellationToken)
    {
        var directoryUser = await context.DirectoryUsers
            .FirstAsync(row => row.Email == email, cancellationToken);
        directoryUser.ArchitectId = architectId;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
