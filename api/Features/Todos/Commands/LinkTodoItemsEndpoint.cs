using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class LinkTodoItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly LinkTodoItemsAuthorisation authorisation;
    private readonly LinkTodoItemsValidation validation;
    private readonly ICommandHandler<LinkTodoItems, Acknowledgement> handler;

    public LinkTodoItemsEndpoint(SignedInUserResolver users, LinkTodoItemsAuthorisation authorisation, LinkTodoItemsValidation validation, ICommandHandler<LinkTodoItems, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(LinkTodoItems))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo-items/{todoItemId}/links")] HttpRequest request,
        string todoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<LinkTodoItems>();
        if (posted is null) return new BadRequestResult();
        if (posted.TodoItemId != todoItemId) return new BadRequestObjectResult("Route todoItemId does not match body.");

        // LinkedByEmail is stamped from the signed-in user — never trusted from the client body.
        var command = posted with { LinkedByEmail = signedInUser.Email };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
