using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Answers 429 to an address that has knocked fruitlessly on a sign-in door more than
/// AuthRateLimit allows in its window. Only the routes an outsider can reach without a session
/// are counted: sign-in, forgot-password, set-password and the invite check. Each door keeps its
/// own budget, so a mail-bomb through forgot-password cannot shut the sign-in page, and a
/// sign-in that succeeds is never counted at all.
/// </summary>
public sealed class AuthRateLimitMiddleware : IFunctionsWorkerMiddleware
{
    private const string RoutePrefix = "/api/auth/";
    private const string SignInRoute = "/api/auth/login";
    private const int TooManyRequests = 429;
    private const int Unauthorised = 401;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is null || !IsASignInDoor(http.Request))
        {
            await next(context);
            return;
        }
        var limit = context.InstanceServices.GetRequiredService<AuthRateLimit>();
        var doorKey = DoorKeyFor(http.Request);
        if (!limit.IsWithinLimit(doorKey, DateTimeOffset.UtcNow))
        {
            await RefuseAsync(http);
            return;
        }
        if (!IsTheSignInItself(http.Request))
        {
            limit.RecordKnock(doorKey, DateTimeOffset.UtcNow);
            await next(context);
            return;
        }
        await next(context);
        var wasRefused = http.Response.StatusCode == Unauthorised;
        if (wasRefused) limit.RecordKnock(doorKey, DateTimeOffset.UtcNow);
    }

    private static async Task RefuseAsync(HttpContext http)
    {
        http.Response.StatusCode = TooManyRequests;
        await http.Response.WriteAsJsonAsync(new { error = "Too many attempts from this address. Try again in a few minutes." });
    }

    private static string DoorKeyFor(HttpRequest request)
    {
        var door = IsTheSignInItself(request) ? SignInRoute : RoutePrefix;
        return $"{ClientKey.Of(request)}|{door}";
    }

    private static bool IsTheSignInItself(HttpRequest request)
    {
        var path = request.Path.Value ?? "";
        return path.Equals(SignInRoute, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsASignInDoor(HttpRequest request)
    {
        var path = request.Path.Value ?? "";
        var isAuthRoute = path.StartsWith(RoutePrefix, StringComparison.OrdinalIgnoreCase);
        var isTheSessionsOwnCall = path.EndsWith("/me", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/logout", StringComparison.OrdinalIgnoreCase);
        return isAuthRoute && !isTheSessionsOwnCall;
    }
}
