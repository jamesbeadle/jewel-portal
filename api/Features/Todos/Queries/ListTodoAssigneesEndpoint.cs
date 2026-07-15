using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListTodoAssigneesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTodoAssignees, IReadOnlyList<DirectoryUser>> handler;
    public ListTodoAssigneesEndpoint(SignedInUserResolver users, IQueryHandler<ListTodoAssignees, IReadOnlyList<DirectoryUser>> handler) { this.users = users; this.handler = handler; }

    // The picker feed for the assignee dropdowns (triage's to-do form, the project To-do tab's add
    // modal). Those forms only render for to-do managers, so the read is gated the same way.
    [Function(nameof(ListTodoAssignees))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo-assignees")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TodoRoles.AllowedToManageTodos.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListTodoAssignees(), request.HttpContext.RequestAborted));
    }
}
