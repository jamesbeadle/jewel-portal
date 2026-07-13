using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET a project's scheduling-tagged emails for the Schedule tab's Communications view. Reading a
// record's mail is a project-view concern, not a triage one — but it is still internal-only mailbox
// content, so the gate is every internal role (never external portal logins).
public sealed class ListSchedulingEmailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSchedulingEmails, IReadOnlyList<MailboxMessage>> handler;

    public ListSchedulingEmailsEndpoint(SignedInUserResolver users, IQueryHandler<ListSchedulingEmails, IReadOnlyList<MailboxMessage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal mailbox content: every internal role, no externals.
    private static readonly RoleSet RolesThatMayReadSchedulingEmails = JpmsRoleSets.AllInternal;

    [Function(nameof(ListSchedulingEmails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/scheduling/emails")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadSchedulingEmails.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(new ListSchedulingEmails(projectId), request.HttpContext.RequestAborted));
    }
}
