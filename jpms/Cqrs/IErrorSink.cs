namespace Jewel.JPMS.Cqrs;

/// <summary>
/// Where the CQRS transport hands failures it cannot resolve itself. Lives here, next to the
/// senders, so the transport can report without depending on the UI layer that renders the report.
/// </summary>
public interface IErrorSink
{
    /// <summary>
    /// A command or query came back as a failure. <paramref name="serverMessage"/> is the endpoint's
    /// own words where it had any, which is usually the only genuinely useful line in the report.
    /// </summary>
    void ReportRequestFailure(
        string operation,
        string httpMethod,
        string path,
        int? statusCode,
        string? serverMessage,
        Exception? exception);
}
