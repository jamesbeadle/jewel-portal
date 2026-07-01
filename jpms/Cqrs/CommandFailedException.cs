namespace Jewel.JPMS.Cqrs;

/// <summary>
/// Raised when a command endpoint rejects a request with an error the user should see verbatim —
/// for example "Reference 'RFI-012' is already used by another request on this project." The message
/// carries the server's own text so dialogs can show it instead of a generic failure notice.
/// </summary>
public sealed class CommandFailedException : Exception
{
    public CommandFailedException(string message) : base(message) { }
}
