using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET /api/mailbox/search?q=… — free-text search of the whole projects mailbox, feeding the record
// pages' "Find emails" dialog. Triage-gated, deliberately tighter than ListRecordEmails' AllInternal:
// reading a record's OWN mail is a project-view concern, but searching the mailbox at large is the
// triage power — and the link the dialog exists to make is triage-gated anyway (RecordLinksEndpoints).
public sealed class SearchMailboxMessagesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<SearchMailboxMessages, IReadOnlyList<MailboxMessage>> handler;

    public SearchMailboxMessagesEndpoint(
        SignedInUserResolver users,
        IQueryHandler<SearchMailboxMessages, IReadOnlyList<MailboxMessage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(SearchMailboxMessages))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/search")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var query = request.Query["q"].ToString();
        if (string.IsNullOrWhiteSpace(query))
            return new BadRequestObjectResult("A search query is required (q=…).");

        var take = int.TryParse(request.Query["take"], out var parsed) ? parsed : 25;

        return new OkObjectResult(
            await handler.HandleAsync(new SearchMailboxMessages(query, take), cancellationToken));
    }
}
