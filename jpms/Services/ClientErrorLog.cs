using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Services;

/// <summary>
/// Ships the full error report — stack, server message, exception type — to the API's log under
/// the report's reference, so the user's card can stay short and support can still find the
/// whole picture. Best effort: this must never raise an error of its own (that way lies a loop),
/// and an expired session simply cannot log, which is fine — an expired session is not a fault.
/// </summary>
public sealed class ClientErrorLog
{
    private const string Route = "api/client-errors";

    private readonly HttpClient httpClient;

    public ClientErrorLog(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public void Send(ErrorReport report)
    {
        if (report.StatusCode == 401) return;
        _ = SendQuietlyAsync(new ClientErrorReport(
            report.Reference, report.OccurredAt, report.Summary, report.Detail, report.Operation,
            report.Endpoint, report.ExceptionType, report.StackTrace, report.Page));
    }

    private async Task SendQuietlyAsync(ClientErrorReport report)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(Route, report);
        }
        catch
        {
            // The report is still on screen and in the Copy button; nothing more can be done for it.
        }
    }
}
