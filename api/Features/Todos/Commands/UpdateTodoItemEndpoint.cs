using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class UpdateTodoItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateTodoItemAuthorisation authorisation;
    private readonly UpdateTodoItemValidation validation;
    private readonly ICommandHandler<UpdateTodoItem, TodoItem> handler;

    public UpdateTodoItemEndpoint(SignedInUserResolver users, UpdateTodoItemAuthorisation authorisation, UpdateTodoItemValidation validation, ICommandHandler<UpdateTodoItem, TodoItem> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(UpdateTodoItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo-items/{todoItemId}")] HttpRequest request, string todoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateTodoItem>();
        if (command is null) return new BadRequestResult();
        if (command.TodoItemId != todoItemId) return new BadRequestObjectResult("Route todoItemId does not match body.");
        // Managers pass on role; anyone else may still update an item currently assigned to them
        // (ticking their own item off from the dashboard / To-dos browser).
        if (!authorisation.Allows(signedInUser, command)
            && !await authorisation.AllowsAsAssigneeAsync(signedInUser, command, request.HttpContext.RequestAborted))
            return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
