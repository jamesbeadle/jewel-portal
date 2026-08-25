using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// GET a to-do item's timeline. Same gate as every other to-do read: internal roles only.
public sealed class ListTodoActivityEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTodoActivity, IReadOnlyList<TodoActivity>> handler;

    public ListTodoActivityEndpoint(SignedInUserResolver users, IQueryHandler<ListTodoActivity, IReadOnlyList<TodoActivity>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    private static readonly RoleSet RolesThatMayReadTodos = JpmsRoleSets.AllInternal;

    [Function(nameof(ListTodoActivity))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo-items/{todoItemId}/activity")] HttpRequest request,
        string todoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadTodos.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(
            await handler.HandleAsync(new ListTodoActivity(todoItemId), request.HttpContext.RequestAborted));
    }
}
