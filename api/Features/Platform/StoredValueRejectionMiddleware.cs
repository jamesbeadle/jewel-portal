using Jewel.JPMS.Api.Data.StoredValues;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Platform;

/// <summary>
/// A value its column cannot hold is the person's to fix, not a server fault. The save's own check
/// (<see cref="StoredValuesRejectedException"/>) answers 400 with its sentences; SQL Server's own
/// refusals of the same kind — text truncated, a number overflowing, a required value missing —
/// that reach the database by another road (raw SQL, a column the model does not know is narrower)
/// answer 400 too, with the server's sentence, instead of the "Backend call failure" toast.
/// The body is the validation shape every command endpoint already answers with — a JSON array of
/// sentences — so every dialog shows it next to the field it was already showing its errors by.
/// </summary>
public sealed class StoredValueRejectionMiddleware : IFunctionsWorkerMiddleware
{
    private const int BadRequest = 400;
    private static readonly HashSet<int> SqlValueRefusals = new()
    {
        SqlErrorNumbers.StringTruncatedNamingColumn,
        SqlErrorNumbers.StringTruncated,
        SqlErrorNumbers.ArithmeticOverflow,
        SqlErrorNumbers.NullIntoRequiredColumn
    };

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception failure) when (SentencesFor(failure) is { } sentences && context.GetHttpContext() is { Response.HasStarted: false } http)
        {
            var logger = context.InstanceServices.GetRequiredService<ILogger<StoredValueRejectionMiddleware>>();
            logger.LogWarning("Save refused, a value did not fit its column: {Sentences}", string.Join(" ", sentences));
            http.Response.StatusCode = BadRequest;
            await http.Response.WriteAsJsonAsync(sentences);
        }
    }

    private static IReadOnlyList<string>? SentencesFor(Exception failure)
    {
        var causes = CausesOf(failure).ToList();
        var rejection = causes.OfType<StoredValuesRejectedException>().FirstOrDefault();
        if (rejection is not null) return rejection.Sentences;
        var refusal = causes.OfType<SqlException>().FirstOrDefault(IsAValueRefusal);
        if (refusal is null) return null;
        return new[] { $"A value was too long or too large for where it is stored, so nothing was saved. {refusal.Message}" };
    }

    private static bool IsAValueRefusal(SqlException refusal) => SqlValueRefusals.Contains(refusal.Number);

    private static IEnumerable<Exception> CausesOf(Exception failure)
    {
        for (Exception? cause = failure; cause is not null; cause = cause.InnerException)
        {
            yield return cause;
            if (cause is AggregateException aggregate)
                foreach (var inner in aggregate.InnerExceptions.SelectMany(CausesOf)) yield return inner;
        }
    }
}
