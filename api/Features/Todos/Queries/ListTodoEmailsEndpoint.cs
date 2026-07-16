using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// GET a to-do item's linked emails for the detail modal. Reading a record's mail is internal-only
// mailbox content, so the gate is every internal role (never external portal logins) — the same
// stance as the Programme tab's scheduling emails.
public sealed class ListTodoEmailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTodoEmails, IReadOnlyList<MailboxMessage>> handler;

    public ListTodoEmailsEndpoint(SignedInUserResolver users, IQueryHandler<ListTodoEmails, IReadOnlyList<MailboxMessage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal mailbox content: every internal role, no externals.
    private static readonly RoleSet RolesThatMayReadTodoEmails = JpmsRoleSets.AllInternal;

    [Function(nameof(ListTodoEmails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo-items/{todoItemId}/emails")] HttpRequest request,
        string todoItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadTodoEmails.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListTodoEmails(todoItemId), request.HttpContext.RequestAborted));
    }
}
