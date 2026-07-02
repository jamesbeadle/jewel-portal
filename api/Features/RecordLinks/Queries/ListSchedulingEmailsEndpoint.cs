using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET a project's scheduling-tagged emails for the Schedule tab's Communications view. Signed-in
// gate only, matching the bid-package emails endpoint — reading a record's mail is a project-view
// concern, not a triage one.
public sealed class ListSchedulingEmailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListSchedulingEmails, IReadOnlyList<MailboxMessage>> handler;

    public ListSchedulingEmailsEndpoint(SignedInUserResolver users, IQueryHandler<ListSchedulingEmails, IReadOnlyList<MailboxMessage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListSchedulingEmails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/scheduling/emails")] HttpRequest request,
        string projectId)
    {
        if (await users.ResolveAsync(request, request.HttpContext.RequestAborted) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListSchedulingEmails(projectId), request.HttpContext.RequestAborted));
    }
}
