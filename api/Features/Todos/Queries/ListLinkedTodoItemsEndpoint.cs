using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// GET a to-do item's linked to-dos — the stored undirected pairs naming it (TodoItemLinks).
// Internal-only read, like the item lists themselves: every internal role, no externals.
public sealed class ListLinkedTodoItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListLinkedTodoItems, IReadOnlyList<TodoItem>> handler;

    public ListLinkedTodoItemsEndpoint(
        SignedInUserResolver users, IQueryHandler<ListLinkedTodoItems, IReadOnlyList<TodoItem>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    private static readonly RoleSet RolesThatMayReadLinkedTodos = JpmsRoleSets.AllInternal;

    [Function(nameof(ListLinkedTodoItems))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo-items/{todoItemId}/linked-todos")] HttpRequest request,
        string todoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadLinkedTodos.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(
            await handler.HandleAsync(new ListLinkedTodoItems(todoItemId), request.HttpContext.RequestAborted));
    }
}
