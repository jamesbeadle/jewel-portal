using System.Text;
using System.Text.Json;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Gates;

public sealed class SignedInUserResolver
{
    private const string PrincipalHeader = "X-MS-CLIENT-PRINCIPAL";
    private readonly JpmsContext context;

    public SignedInUserResolver(JpmsContext context) { this.context = context; }

    public async Task<SignedInUser?> ResolveAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var email = EmailFromPrincipal(request);
        if (email is null) return null;

        var roles = await ResolveRolesAsync(email, cancellationToken);
        return new SignedInUser(email, email, roles);
    }

    private async Task<IReadOnlyList<Role>> ResolveRolesAsync(string email, CancellationToken cancellationToken)
    {
        if (JpmsAdministrators.Contains(email)) return Enum.GetValues<Role>();
        return await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .Select(row => (Role)row.Role)
            .ToListAsync(cancellationToken);
    }

    private static string? EmailFromPrincipal(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(PrincipalHeader, out var encoded)) return null;
        if (string.IsNullOrWhiteSpace(encoded)) return null;

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded!));
        var principal = JsonSerializer.Deserialize<StaticWebAppsPrincipal>(json, JsonOptions);
        if (string.IsNullOrWhiteSpace(principal?.UserDetails)) return null;
        return principal.UserDetails;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record StaticWebAppsPrincipal(
        string? IdentityProvider,
        string? UserId,
        string? UserDetails,
        IReadOnlyList<string>? UserRoles);
}
