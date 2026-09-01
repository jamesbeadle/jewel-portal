using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class MoveTodoItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly MoveTodoItemAuthorisation authorisation;
    private readonly MoveTodoItemValidation validation;
    private readonly ICommandHandler<MoveTodoItem, TodoItem> handler;

    public MoveTodoItemEndpoint(SignedInUserResolver users, AuditActor auditActor, MoveTodoItemAuthorisation authorisation, MoveTodoItemValidation validation, ICommandHandler<MoveTodoItem, TodoItem> handler)
    { this.users = users; this.auditActor = auditActor; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(MoveTodoItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo-items/{todoItemId}/move")] HttpRequest request, string todoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<MoveTodoItem>();
        if (command is null) return new BadRequestResult();
        if (command.TodoItemId != todoItemId) return new BadRequestObjectResult("Route todoItemId does not match body.");
        auditActor.Email = signedInUser.Email; // the timeline records who moved the item
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
