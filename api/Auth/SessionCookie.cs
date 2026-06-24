using Microsoft.AspNetCore.Http;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Reads, sets and clears the HTTP-only session cookie. The cookie value is the raw session
/// secret; it is HttpOnly (invisible to JavaScript), Secure (HTTPS only) and SameSite=Lax.
/// The app and the /api endpoints share an origin on Static Web Apps, so the browser sends
/// the cookie automatically on same-origin fetches.
/// </summary>
public static class SessionCookie
{
    public const string Name = "jpms_session";

    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    public static void Set(HttpResponse response, string secret) =>
        response.Cookies.Append(Name, secret, BuildOptions(DateTimeOffset.UtcNow.Add(Lifetime)));

    public static void Clear(HttpResponse response) =>
        response.Cookies.Append(Name, "", BuildOptions(DateTimeOffset.UnixEpoch));

    private static CookieOptions BuildOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
        Expires = expires
    };
}
