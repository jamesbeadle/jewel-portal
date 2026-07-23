using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// HTTP surface for the record-agnostic link layer: list the records of a type on a project (for the
// triage picker) and link a mailbox message to one. Same gating as the rest of triage. Message ids
// travel in the JSON body (Graph ids contain path-unsafe characters), so the link route is static.
public sealed class RecordLinksEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly Audit.AuditActor auditActor;
    private readonly IQueryHandler<ListLinkableRecords, IReadOnlyList<LinkableRecord>> list;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;

    public RecordLinksEndpoints(
        SignedInUserResolver users,
        Audit.AuditActor auditActor,
        IQueryHandler<ListLinkableRecords, IReadOnlyList<LinkableRecord>> list,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.list = list;
        this.link = link;
    }

    [Function(nameof(ListLinkableRecords))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/records")] HttpRequest request,
        string projectId)
    {
        if (await Gate(request) is { } deny) return deny;
        var typeRaw = request.Query["type"].ToString();
        if (!TryParseRecordType(typeRaw, out var type))
            return new BadRequestObjectResult("A valid record type is required (e.g. type=Request).");
        return new OkObjectResult(await list.HandleAsync(new ListLinkableRecords(projectId, type), request.HttpContext.RequestAborted));
    }

    [Function(nameof(LinkMessageToRecord))]
    public async Task<IActionResult> Link(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/link")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var command = await ReadBody<LinkMessageToRecord>(request);
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId) || string.IsNullOrWhiteSpace(command.RecordId))
            return new BadRequestObjectResult("messageId and recordId are required.");
        return new OkObjectResult(await link.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    // Accept the record type either by name ("Request") or numeric value ("0").
    private static bool TryParseRecordType(string raw, out RecordType type)
    {
        if (Enum.TryParse(raw, ignoreCase: true, out type) && Enum.IsDefined(type))
            return true;
        type = default;
        return false;
    }

    private async Task<IActionResult?> Gate(HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        auditActor.Email = signedInUser.Email; // audit rows record who acted
        return null;
    }

    private static async Task<T?> ReadBody<T>(HttpRequest request) where T : class
    {
        try { return await request.ReadFromJsonAsync<T>(); }
        catch { return null; }
    }
}
