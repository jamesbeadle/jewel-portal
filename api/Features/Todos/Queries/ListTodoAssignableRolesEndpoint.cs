using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListTodoAssignableRolesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTodoAssignableRoles, IReadOnlyList<Role>> handler;
    public ListTodoAssignableRolesEndpoint(SignedInUserResolver users, IQueryHandler<ListTodoAssignableRoles, IReadOnlyList<Role>> handler) { this.users = users; this.handler = handler; }

    // The picker feed for the assignee-role dropdowns (triage's to-do form, the project To-do
    // tab's add modal, the To-dos browser). Those forms only render for to-do managers, so the
    // read is gated the same way.
    [Function(nameof(ListTodoAssignableRoles))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo-assignable-roles")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TodoRoles.AllowedToManageTodos.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListTodoAssignableRoles(), request.HttpContext.RequestAborted));
    }
}
