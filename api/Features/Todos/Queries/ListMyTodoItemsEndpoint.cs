using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListMyTodoItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListMyTodoItems, IReadOnlyList<TodoItem>> handler;
    public ListMyTodoItemsEndpoint(SignedInUserResolver users, IQueryHandler<ListMyTodoItems, IReadOnlyList<TodoItem>> handler) { this.users = users; this.handler = handler; }

    // Any internal user may read the items assigned to THEM — the assignee email is stamped from
    // the session here, never taken from the request, so this can't be used to browse someone
    // else's list. The browse-everything read is ListAllTodoItems, gated separately.
    private static readonly RoleSet RolesThatMayReadOwnTodos = JpmsRoleSets.AllInternal;

    [Function(nameof(ListMyTodoItems))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my/todos")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadOwnTodos.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListMyTodoItems(signedInUser.Email), request.HttpContext.RequestAborted));
    }
}
