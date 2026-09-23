namespace Jewel.JPMS.Api.Features.Forms.Office;

/// <summary>
/// How an office forms endpoint answers once its gates have passed: a failed validation is a 400
/// with the reasons, a refusal the handler raises is a 409 with its sentence, anything else the
/// handler's result — so every endpoint says the same things the same way.
/// </summary>
internal static class OfficeAnswers
{
    public static async Task<IActionResult> RunAsync<TCommand, TResult>(
        ICommandHandler<TCommand, TResult> handler, TCommand command, ValidationOutcome outcome, CancellationToken cancellationToken)
        where TCommand : ICommand<TResult>
    {
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try { return new OkObjectResult(await handler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException refusal) { return new ConflictObjectResult(refusal.Message); }
    }

    public static async Task<IActionResult> ReadAsync<TQuery, TResult>(
        IQueryHandler<TQuery, TResult> handler, TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<TResult>
    {
        try { return new OkObjectResult(await handler.HandleAsync(query, cancellationToken)); }
        catch (InvalidOperationException refusal) { return new NotFoundObjectResult(refusal.Message); }
    }
}
