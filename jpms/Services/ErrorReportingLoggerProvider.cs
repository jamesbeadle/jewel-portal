using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Services;

/// <summary>
/// Turns the framework's own error logging into user-visible reports.
///
/// Blazor WebAssembly has no usable AppDomain.UnhandledException: when a component's lifecycle
/// method throws, the renderer logs it through ILogger and then tears the circuit down. Watching
/// the logging pipeline is therefore the only way to catch the failures that never pass through
/// our own try/catch — and it costs nothing, because the log call happens either way.
/// </summary>
public sealed class ErrorReportingLoggerProvider : ILoggerProvider
{
    private readonly GlobalErrorSink sink;

    public ErrorReportingLoggerProvider(GlobalErrorSink sink) { this.sink = sink; }

    public ILogger CreateLogger(string categoryName) => new SinkLogger(categoryName, sink);

    public void Dispose() { }

    private sealed class SinkLogger : ILogger
    {
        private readonly string category;
        private readonly GlobalErrorSink sink;

        public SinkLogger(string category, GlobalErrorSink sink)
        {
            this.category = category;
            this.sink = sink;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            // An error with no exception behind it is almost always a deliberate log line from our
            // own code, which has already told the user whatever it needed to. The unhandled
            // failures we are here for always carry one.
            if (exception is null) return;
            sink.Capture(new GlobalErrorSink.CapturedError(category, formatter(state, exception), exception));
        }
    }
}
