using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class DeleteTodoItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteTodoItemAuthorisation authorisation;
    private readonly DeleteTodoItemValidation validation;
    private readonly ICommandHandler<DeleteTodoItem, Acknowledgement> handler;

    public DeleteTodoItemEndpoint(SignedInUserResolver users, DeleteTodoItemAuthorisation authorisation, DeleteTodoItemValidation validation, ICommandHandler<DeleteTodoItem, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(DeleteTodoItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo-items/{todoItemId}")] HttpRequest request, string todoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteTodoItem(todoItemId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
