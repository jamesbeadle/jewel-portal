using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET /api/mailbox/tags/resolve?tags=… — mailbox tag stems (comma-separated; references never
// contain commas) back to the records they name, feeding the tagged-email search's record chips.
// Triage-gated to match the mailbox search the chips ride on (SearchMailboxMessagesEndpoint): the
// caller is by definition holding whole-mailbox search results.
public sealed class ResolveRecordTagsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ResolveRecordTags, IReadOnlyList<LinkableRecord>> handler;

    public ResolveRecordTagsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ResolveRecordTags, IReadOnlyList<LinkableRecord>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ResolveRecordTags))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/tags/resolve")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var tags = request.Query["tags"].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tags.Length == 0)
            return new BadRequestObjectResult("At least one tag stem is required (tags=…).");

        return new OkObjectResult(
            await handler.HandleAsync(new ResolveRecordTags(tags), cancellationToken));
    }
}
