namespace Jewel.JPMS.Contracts.Platform;

/// <summary>
/// The whole of one error the portal showed a user, as the browser captured it: the server's
/// own words, the endpoint and status, the exception type and its stack. The user sees only the
/// summary, the reference, the time and the page; this is the rest, posted to the API so that
/// support can find the full picture in the logs from the reference alone.
/// </summary>
public sealed record ClientErrorReport(
    string Reference,
    DateTimeOffset OccurredAt,
    string Summary,
    string? Detail,
    string? Operation,
    string? Endpoint,
    string? ExceptionType,
    string? StackTrace,
    string? Page);
