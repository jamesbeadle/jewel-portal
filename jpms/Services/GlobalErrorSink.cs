namespace Jewel.JPMS.Services;

/// <summary>
/// The bridge between the logging pipeline — which is configured before the app's services exist —
/// and <see cref="ErrorReporter"/>, which is scoped and only comes into being when the first
/// component injects it.
///
/// Errors that arrive before anyone is listening are held rather than dropped, so a failure during
/// start-up still reaches the toast the moment the shell renders. That window is small but it is
/// exactly where the worst errors live.
/// </summary>
public sealed class GlobalErrorSink
{
    private readonly List<CapturedError> pending = new();

    public sealed record CapturedError(string Category, string Message, Exception? Exception);

    public event Action<CapturedError>? OnError;

    public void Capture(CapturedError error)
    {
        if (OnError is null)
        {
            // Keep the first few only: a start-up failure tends to log the same thing repeatedly,
            // and an unbounded buffer would be its own bug.
            if (pending.Count < 5) pending.Add(error);
            return;
        }
        OnError.Invoke(error);
    }

    /// <summary>Hands over anything captured before a listener existed, and empties the buffer.</summary>
    public IReadOnlyList<CapturedError> DrainPending()
    {
        var drained = pending.ToList();
        pending.Clear();
        return drained;
    }
}
