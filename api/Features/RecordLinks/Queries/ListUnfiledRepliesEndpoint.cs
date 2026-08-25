using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET the replies that arrived on one record's threads after its last filed email and aren't
// tagged to it yet. Same gate as reading the record's emails: every internal role, no externals.
public sealed class ListUnfiledRepliesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListUnfiledReplies, IReadOnlyList<MailboxMessage>> handler;

    public ListUnfiledRepliesEndpoint(SignedInUserResolver users, IQueryHandler<ListUnfiledReplies, IReadOnlyList<MailboxMessage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    private static readonly RoleSet RolesThatMayReadRecordEmails = JpmsRoleSets.AllInternal;

    [Function(nameof(ListUnfiledReplies))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "records/{type}/{recordId}/unfiled-replies")] HttpRequest request,
        string type,
        string recordId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRecordEmails.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        if (!Enum.TryParse<RecordType>(type, ignoreCase: true, out var recordType) || !Enum.IsDefined(recordType))
            return new BadRequestObjectResult("A valid record type is required (e.g. Todo).");

        return new OkObjectResult(await handler.HandleAsync(new ListUnfiledReplies(recordType, recordId), request.HttpContext.RequestAborted));
    }
}
