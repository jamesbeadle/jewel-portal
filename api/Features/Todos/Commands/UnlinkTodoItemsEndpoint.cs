using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class UnlinkTodoItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UnlinkTodoItemsAuthorisation authorisation;
    private readonly UnlinkTodoItemsValidation validation;
    private readonly ICommandHandler<UnlinkTodoItems, Acknowledgement> handler;

    public UnlinkTodoItemsEndpoint(SignedInUserResolver users, UnlinkTodoItemsAuthorisation authorisation, UnlinkTodoItemsValidation validation, ICommandHandler<UnlinkTodoItems, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    // DELETE names both ends in the route — the pair IS the resource, so no body to disagree
    // with the route, mirroring DeleteTodoItem.
    [Function(nameof(UnlinkTodoItems))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo-items/{todoItemId}/links/{linkedTodoItemId}")] HttpRequest request,
        string todoItemId, string linkedTodoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new UnlinkTodoItems(todoItemId, linkedTodoItemId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
