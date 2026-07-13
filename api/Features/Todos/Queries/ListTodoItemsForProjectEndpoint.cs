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

    // Project to-dos are internal-only reads; managing them is gated separately by
    // TodoRoles.AllowedToManageTodos.
    private static readonly RoleSet RolesThatMayReadTodos = JpmsRoleSets.AllInternal;

    [Function(nameof(ListTodoItemsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/todos")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadTodos.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListTodoItemsForProject(projectId), request.HttpContext.RequestAborted));
    }
}
