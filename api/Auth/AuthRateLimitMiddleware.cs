using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Answers 429 to an address that has knocked on the sign-in doors more than AuthRateLimit
/// allows in its window. Only the routes an outsider can reach without a session are counted:
/// sign-in, forgot-password, set-password and the invite check. Everything else passes
/// straight through.
/// </summary>
public sealed class AuthRateLimitMiddleware : IFunctionsWorkerMiddleware
{
    private const string RoutePrefix = "/api/auth/";
    private const int TooManyRequests = 429;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is null || !IsASignInDoor(http.Request))
        {
            await next(context);
            return;
        }
        var limit = context.InstanceServices.GetRequiredService<AuthRateLimit>();
        var isWithinLimit = limit.IsWithinLimit(ClientKey.Of(http.Request), DateTimeOffset.UtcNow);
        if (!isWithinLimit)
        {
            http.Response.StatusCode = TooManyRequests;
            await http.Response.WriteAsJsonAsync(new { error = "Too many attempts from this address. Try again in a few minutes." });
            return;
        }
        await next(context);
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
