using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// POST progress on a to-do (Working on it / a chase / a note). The actor is stamped from the
// signed-in user — never trusted from the body. A handler guard ("this item is done") comes back
// as a 400 with its sentence so the page can print it inline, not a bodiless 500.
public sealed class LogTodoProgressEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly LogTodoProgressAuthorisation authorisation;
    private readonly LogTodoProgressValidation validation;
    private readonly ICommandHandler<LogTodoProgress, TodoItem> handler;

    public LogTodoProgressEndpoint(
        SignedInUserResolver users, AuditActor auditActor, LogTodoProgressAuthorisation authorisation,
        LogTodoProgressValidation validation, ICommandHandler<LogTodoProgress, TodoItem> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(LogTodoProgress))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo-items/{todoItemId}/progress")] HttpRequest request,
        string todoItemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<LogTodoProgress>();
        if (posted is null) return new BadRequestResult();
        if (posted.TodoItemId != todoItemId) return new BadRequestObjectResult("Route todoItemId does not match body.");

        var command = posted with { ActorEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!authorisation.Allows(signedInUser, command)
            && !await authorisation.AllowsAsAssigneeAsync(signedInUser, command, cancellationToken))
            return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try { return new OkObjectResult(await handler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
