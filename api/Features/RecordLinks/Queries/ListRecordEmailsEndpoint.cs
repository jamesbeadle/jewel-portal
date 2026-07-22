using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// GET the emails tagged to one record (any linkable type) — feeds record pages' Communications
// panels (e.g. the VOQ page listing the mail behind the quote and its VO). Reading a record's mail
// is a project-view concern, not a triage one — but it is still internal-only mailbox content, so
// the gate is every internal role (never external portal logins), same as scheduling emails.
public sealed class ListRecordEmailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRecordEmails, IReadOnlyList<MailboxMessage>> handler;

    public ListRecordEmailsEndpoint(SignedInUserResolver users, IQueryHandler<ListRecordEmails, IReadOnlyList<MailboxMessage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    // Internal mailbox content: every internal role, no externals.
    private static readonly RoleSet RolesThatMayReadRecordEmails = JpmsRoleSets.AllInternal;

    [Function(nameof(ListRecordEmails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "records/{type}/{recordId}/emails")] HttpRequest request,
        string type,
        string recordId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RolesThatMayReadRecordEmails.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        // Accept the record type either by name ("VariationQuote") or numeric value ("7").
        if (!Enum.TryParse<RecordType>(type, ignoreCase: true, out var recordType) || !Enum.IsDefined(recordType))
            return new BadRequestObjectResult("A valid record type is required (e.g. VariationQuote).");

        return new OkObjectResult(await handler.HandleAsync(new ListRecordEmails(recordType, recordId), request.HttpContext.RequestAborted));
    }
}
