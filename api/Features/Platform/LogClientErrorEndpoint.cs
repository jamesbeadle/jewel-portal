using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Api.Features.Platform;

/// <summary>
/// POST /api/client-errors — the browser's full error report, written to the API's log under
/// its JPMS-XXXXXX reference. The user's error card shows them only what they can act on; the
/// stack, the server's words and the exception type land here, so support can look a report up
/// from the reference the user reads out. Signed-in only, and the user is the resolved caller —
/// never the client's say-so. Nothing is stored: the log is the record.
/// </summary>
public sealed class LogClientErrorEndpoint
{
    private const int MaxStackCharacters = 8000;

    private readonly SignedInUserResolver users;
    private readonly ILogger<LogClientErrorEndpoint> logger;

    public LogClientErrorEndpoint(SignedInUserResolver users, ILogger<LogClientErrorEndpoint> logger)
    {
        this.users = users;
        this.logger = logger;
    }

    [Function("LogClientError")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "client-errors")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var report = await request.ReadFromJsonAsync<ClientErrorReport>();
        if (report is null || string.IsNullOrWhiteSpace(report.Reference)) return new BadRequestResult();

        logger.LogError(
            "Portal error {Reference} at {OccurredAt} for {User} on {Page}: {Summary} | doing {Operation} | at {Endpoint} | detail {Detail} | {ExceptionType}\n{StackTrace}",
            report.Reference, report.OccurredAt, signedInUser.Email, report.Page, report.Summary,
            report.Operation, report.Endpoint, report.Detail, report.ExceptionType, Clip(report.StackTrace));
        return new NoContentResult();
    }

    private static string Clip(string? stack) =>
        stack is null || stack.Length <= MaxStackCharacters ? stack ?? "" : stack[..MaxStackCharacters];
}
