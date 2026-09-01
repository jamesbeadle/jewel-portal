using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ClientPortal.Commands;

/// <summary>POST /api/client-portal/my/variation-orders/{voId}/messages — the signed-in client
/// adds to the order's shared thread. ClientId and the author come from the session, never the body.</summary>
public sealed class PostMyClientVariationOrderMessageEndpoint
{
    private const int BodyLimit = 4000;

    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<PostMyClientVariationOrderMessage, VariationOrderMessage> handler;
    private readonly AuditActor auditActor;
    private readonly AuditTrail audit;

    public PostMyClientVariationOrderMessageEndpoint(
        SignedInUserResolver users, ICommandHandler<PostMyClientVariationOrderMessage, VariationOrderMessage> handler,
        AuditActor auditActor, AuditTrail audit)
    {
        this.users = users; this.handler = handler; this.auditActor = auditActor; this.audit = audit;
    }

    [Function("PostMyClientVariationOrderMessage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "client-portal/my/variation-orders/{voId}/messages")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is null) return new StatusCodeResult(403);

        var posted = await request.ReadFromJsonAsync<PostMyClientVariationOrderMessage>();
        if (posted is null) return new BadRequestResult();
        if (posted.VariationOrderId != voId) return new BadRequestObjectResult("Route voId does not match body.");
        if (string.IsNullOrWhiteSpace(posted.Body)) return new BadRequestObjectResult("Message body is required.");
        if (posted.Body.Length > BodyLimit) return new BadRequestObjectResult($"Message body must be {BodyLimit} characters or fewer.");

        var command = posted with
        {
            ClientId = clientId,
            AuthorEmail = signedInUser.Email,
            AuthorName = signedInUser.DisplayName
        };
        auditActor.Email = signedInUser.Email;

        VariationOrderMessage message;
        try { message = await handler.HandleAsync(command, cancellationToken); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }

        await audit.WriteAsync(AuditEventType.VariationNotePosted,
            command.ParentMessageId is null
                ? "Client posted a message on the variation order's conversation."
                : "Client replied on the variation order's conversation.",
            pathway: "Client", recordType: RecordType.Variation, recordId: command.VariationOrderId,
            cancellationToken: cancellationToken);

        return new OkObjectResult(message);
    }
}
