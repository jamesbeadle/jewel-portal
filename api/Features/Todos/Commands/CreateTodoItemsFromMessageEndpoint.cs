using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class CreateTodoItemsFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateTodoItemsFromMessageAuthorisation authorisation;
    private readonly CreateTodoItemsFromMessageValidation validation;
    private readonly ICommandHandler<CreateTodoItemsFromMessage, IReadOnlyList<TodoItem>> handler;

    public CreateTodoItemsFromMessageEndpoint(SignedInUserResolver users, CreateTodoItemsFromMessageAuthorisation authorisation, CreateTodoItemsFromMessageValidation validation, ICommandHandler<CreateTodoItemsFromMessage, IReadOnlyList<TodoItem>> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(CreateTodoItemsFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-todos")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateTodoItemsFromMessage>();
        if (posted is null || string.IsNullOrWhiteSpace(posted.MessageId) || string.IsNullOrWhiteSpace(posted.ProjectId))
            return new BadRequestObjectResult("messageId and projectId are required.");

        // The creator is always the signed-in user — never trusted from the client body.
        var command = posted with { CreatedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
