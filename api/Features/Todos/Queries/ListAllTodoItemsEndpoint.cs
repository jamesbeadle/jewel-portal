using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListAllTodoItemsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListAllTodoItems, IReadOnlyList<TodoItem>> handler;
    public ListAllTodoItemsEndpoint(SignedInUserResolver users, IQueryHandler<ListAllTodoItems, IReadOnlyList<TodoItem>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListAllTodoItems))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        // Seeing EVERYONE's items is the MD's / administrators' browser view only; other roles
        // read their own list through ListMyTodoItems.
        if (!TodoRoles.AllowedToSeeAllTodos.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListAllTodoItems(), request.HttpContext.RequestAborted));
    }
}
