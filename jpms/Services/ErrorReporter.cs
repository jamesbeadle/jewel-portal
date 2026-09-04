using System.Text.Json;

namespace Jewel.JPMS.Services;

/// <summary>
/// Holds the error currently being shown to the user, and builds the report behind it.
///
/// One at a time, newest wins. A stack of toasts is a worse experience than a single clear one:
/// when something is broken it usually breaks several times in a row, and the user needs the most
/// recent state of the world, not a history of it. Everything needed to diagnose the failure is
/// captured at the moment it happens — page, user, endpoint, status, the server's own words — so
/// that "it went red" can be forwarded as something actionable.
/// </summary>
public sealed class ErrorReporter : IErrorSink, IDisposable
{
    private readonly NavigationManager navigation;
    private readonly AuthService auth;
    private readonly GlobalErrorSink sink;

    /// <summary>The same failure repeating inside this window is treated as one event. Background
    /// refreshes retry on their own, and three identical toasts say nothing the first did not.</summary>
    private static readonly TimeSpan RepeatWindow = TimeSpan.FromSeconds(5);

    private string? lastSignature;
    private DateTimeOffset lastReportedAt;

    public ErrorReporter(NavigationManager navigation, AuthService auth, GlobalErrorSink sink)
    {
        this.navigation = navigation;
        this.auth = auth;
        this.sink = sink;

        sink.OnError += HandleCaptured;
        foreach (var captured in sink.DrainPending()) HandleCaptured(captured);
    }

    public ErrorReport? Current { get; private set; }

    public event Action? OnChange;

    public void Dismiss()
    {
        if (Current is null) return;
        Current = null;
        OnChange?.Invoke();
    }

    /// <summary>Raise a report written by the calling code — use when you already know what the
    /// user was trying to do, which makes for a far better summary than anything inferred.</summary>
    public void Report(string summary, string? detail = null, string? operation = null)
    {
        Publish(new ErrorReport(
            ErrorReport.NewReference(),
            DateTimeOffset.Now,
            summary,
            Detail: detail,
            Operation: operation,
            Page: navigation.Uri,
            User: auth.CurrentUser?.Email));
    }

    void IErrorSink.ReportRequestFailure(
        string operation, string httpMethod, string path, int? statusCode, string? serverMessage, Exception? exception)
    {
        // Same endpoint, same status, same moment — one report.
        if (IsRepeat($"{httpMethod} {path} {statusCode}")) return;

        Publish(new ErrorReport(
            ErrorReport.NewReference(),
            DateTimeOffset.Now,
            SummaryFor(httpMethod, statusCode, serverMessage, exception),
            Detail: serverMessage ?? exception?.Message,
            Operation: operation,
            HttpMethod: httpMethod,
            RequestPath: path,
            StatusCode: statusCode,
            ExceptionType: exception?.GetType().FullName,
            StackTrace: exception?.StackTrace,
            Page: navigation.Uri,
            User: auth.CurrentUser?.Email));
    }

    /// <summary>An exception nothing else caught — from an error boundary or the logging pipeline.</summary>
    public void ReportUnhandled(Exception exception, string? context = null)
    {
        if (IsRepeat($"{exception.GetType().FullName}:{exception.Message}")) return;

        Publish(new ErrorReport(
            ErrorReport.NewReference(),
            DateTimeOffset.Now,
            "Something on this page stopped working. Nothing you've entered has been lost — reload to carry on.",
            Detail: exception.Message,
            Operation: context,
            ExceptionType: exception.GetType().FullName,
            StackTrace: exception.StackTrace,
            Page: navigation.Uri,
            User: auth.CurrentUser?.Email));
    }

    private void HandleCaptured(GlobalErrorSink.CapturedError captured)
    {
        if (captured.Exception is null) return;
        ReportUnhandled(captured.Exception, captured.Category);
    }

    private void Publish(ErrorReport report)
    {
        Current = report;
        OnChange?.Invoke();
    }

    private bool IsRepeat(string signature)
    {
        var now = DateTimeOffset.Now;
        if (signature == lastSignature && now - lastReportedAt < RepeatWindow) return true;
        lastSignature = signature;
        lastReportedAt = now;
        return false;
    }

    /// <summary>
    /// What the user reads. The server's own message wins whenever it has one, because an endpoint
    /// that bothered to explain itself ("Reference 'RFI-012' is already used…") is always clearer
    /// than anything we could infer from a status code.
    /// </summary>
    private static string SummaryFor(string httpMethod, int? statusCode, string? serverMessage, Exception? exception)
    {
        if (!string.IsNullOrWhiteSpace(serverMessage)) return serverMessage!;

        // A JsonException means the request did land — we simply could not read the answer. Telling
        // the user to check their connection sends them hunting for a fault that is ours, not theirs.
        if (exception is JsonException)
            return "The server's answer couldn't be read. Copy the reference below and send it on — this one is ours to fix.";

        return statusCode switch
        {
            401 => "Your session has expired. Sign in again to carry on.",
            403 => "You don't have permission to do that.",
            404 => "That record no longer exists — someone may have deleted it.",
            408 or 504 => "The server took too long to answer. Try again in a moment.",
            >= 500 when IsRead(httpMethod) => "The server hit a problem answering that. Nothing has changed — try again in a moment.",
            >= 500 => "The server hit a problem handling that. Nothing was saved.",
            null => "Couldn't reach the server. Check your connection and try again.",
            _ => "That request didn't go through."
        };
    }

    /// <summary>
    /// Whether the failed request was only ever going to read. "Nothing was saved" is the
    /// reassurance a failed write needs; on a read it is a non-sequitur — the user was not saving
    /// anything — and it sends them hunting for work they never lost. A cold host answering 503 to
    /// a background GET is the common case, so this is the sentence most users actually meet.
    /// </summary>
    private static bool IsRead(string httpMethod) =>
        string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase)
        || string.Equals(httpMethod, "HEAD", StringComparison.OrdinalIgnoreCase);

    public void Dispose() => sink.OnError -= HandleCaptured;
}
