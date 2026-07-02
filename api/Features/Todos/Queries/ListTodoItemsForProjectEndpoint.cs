using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListTodoItemsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTodoItemsForProject, IReadOnlyList<TodoItem>> handler;
    public ListTodoItemsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListTodoItemsForProject, IReadOnlyList<TodoItem>> handler) { this.users = users; this.handler = handler; }

    [Function(nameof(ListTodoItemsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/todos")] HttpRequest request, string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListTodoItemsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
