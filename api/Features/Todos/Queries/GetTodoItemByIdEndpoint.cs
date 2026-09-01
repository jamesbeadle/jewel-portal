using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// GET one to-do item by id — the read behind /todos/{id}. The gate matches every other to-do
// read (internal roles only, never external portal logins); what the reader may DO with the item
// stays with the command gates.
public sealed class GetTodoItemByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetTodoItemById, TodoItem?> handler;

    public GetTodoItemByIdEndpoint(SignedInUserResolver users, IQueryHandler<GetTodoItemById, TodoItem?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    private static readonly RoleSet RolesThatMayReadTodos = JpmsRoleSets.AllInternal;

    [Function(nameof(GetTodoItemById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo-items/{todoItemId}")] HttpRequest request,
        string todoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadTodos.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(
            await handler.HandleAsync(new GetTodoItemById(todoItemId), request.HttpContext.RequestAborted));
    }
}
