using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Jewel.JPMS.Api.Gates;

public sealed class SignedInUserResolver
{
    private const string PrincipalHeader = "X-MS-CLIENT-PRINCIPAL";

    public SignedInUser? Resolve(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(PrincipalHeader, out var encoded)) return null;
        if (string.IsNullOrWhiteSpace(encoded)) return null;

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded!));
        var principal = JsonSerializer.Deserialize<StaticWebAppsPrincipal>(json, JsonOptions);
        if (principal is null) return null;
        if (string.IsNullOrWhiteSpace(principal.UserDetails)) return null;

        var role = principal.UserRoles?.FirstOrDefault(name => name != "anonymous" && name != "authenticated") ?? "authenticated";
        return new SignedInUser(principal.UserDetails!, principal.UserDetails!, role);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record StaticWebAppsPrincipal(
        string? IdentityProvider,
        string? UserId,
        string? UserDetails,
        IReadOnlyList<string>? UserRoles);
}
