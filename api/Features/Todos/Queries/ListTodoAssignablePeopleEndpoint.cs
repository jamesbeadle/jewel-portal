using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListTodoAssignablePeopleEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTodoAssignablePeople, IReadOnlyList<TodoAssignablePerson>> handler;
    public ListTodoAssignablePeopleEndpoint(SignedInUserResolver users, IQueryHandler<ListTodoAssignablePeople, IReadOnlyList<TodoAssignablePerson>> handler) { this.users = users; this.handler = handler; }

    // The person half of the assignee pickers — served alongside ListTodoAssignableRoles to the
    // same forms (triage's to-do form, the add modals, the detail modal's reassign), so it wears
    // the same manage gate.
    [Function(nameof(ListTodoAssignablePeople))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo-assignable-people")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TodoRoles.AllowedToManageTodos.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListTodoAssignablePeople(), request.HttpContext.RequestAborted));
    }
}
