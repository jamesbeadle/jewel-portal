using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class AddTodoItemEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddTodoItemAuthorisation authorisation;
    private readonly AddTodoItemValidation validation;
    private readonly ICommandHandler<AddTodoItem, TodoItem> handler;

    public AddTodoItemEndpoint(SignedInUserResolver users, AddTodoItemAuthorisation authorisation, AddTodoItemValidation validation, ICommandHandler<AddTodoItem, TodoItem> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AddTodoItem))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/todos")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<AddTodoItem>();
        if (posted is null) return new BadRequestResult();
        if (posted.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        // The creator is always the signed-in user — never trusted from the client body.
        var command = posted with { CreatedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
