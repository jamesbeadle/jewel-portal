using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListTodoLinkCandidatesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTodoLinkCandidates, IReadOnlyList<TodoItem>> handler;
    public ListTodoLinkCandidatesEndpoint(SignedInUserResolver users, IQueryHandler<ListTodoLinkCandidates, IReadOnlyList<TodoItem>> handler)
    { this.users = users; this.handler = handler; }

    // Gate: the manage roles, because the only thing the pool feeds is making links.
    private static readonly RoleSet RolesThatMayListCandidates = TodoRoles.AllowedToManageTodos;

    [Function(nameof(ListTodoLinkCandidates))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo-link-candidates")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayListCandidates.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        string? projectId = request.Query["projectId"];
        return new OkObjectResult(
            await handler.HandleAsync(new ListTodoLinkCandidates(projectId), request.HttpContext.RequestAborted));
    }
}
