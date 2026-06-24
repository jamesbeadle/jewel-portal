using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Resolves the signed-in user for an incoming request from the HTTP-only session cookie.
/// The principal comes from a validated session opened by the email/password login flow.
/// </summary>
public sealed class SignedInUserResolver
{
    private readonly JpmsContext context;
    private readonly SessionManager sessions;

    public SignedInUserResolver(JpmsContext context, SessionManager sessions)
    {
        this.context = context;
        this.sessions = sessions;
    }

    public async Task<SignedInUser?> ResolveAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var secret = SessionCookie.Read(request);
        if (secret is null) return null;

        var email = await sessions.ResolveEmailAsync(secret, cancellationToken);
        if (string.IsNullOrWhiteSpace(email)) return null;

        var displayName = await ResolveDisplayNameAsync(email, cancellationToken);
        var roles = await ResolveRolesAsync(email, cancellationToken);
        return new SignedInUser(email, displayName, roles);
    }

    private async Task<string> ResolveDisplayNameAsync(string email, CancellationToken cancellationToken)
    {
        var directoryUser = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        return string.IsNullOrWhiteSpace(directoryUser?.DisplayName) ? email : directoryUser!.DisplayName;
    }

    private async Task<IReadOnlyList<Role>> ResolveRolesAsync(string email, CancellationToken cancellationToken)
    {
        if (JpmsAdministrators.Contains(email)) return Enum.GetValues<Role>();
        return await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);
    }
}
