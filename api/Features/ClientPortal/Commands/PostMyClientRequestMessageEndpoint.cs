using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.ClientPortal;

namespace Jewel.JPMS.Api.Features.ClientPortal.Commands;

/// <summary>POST /api/client-portal/my/requests/{requestId}/messages — the signed-in client adds
/// to the request's shared thread. ClientId and the author come from the session, never the body.</summary>
public sealed class PostMyClientRequestMessageEndpoint
{
    private const int BodyLimit = 4000;

    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<PostMyClientRequestMessage, RequestMessage> handler;
    private readonly AuditActor auditActor;
    private readonly AuditTrail audit;

    public PostMyClientRequestMessageEndpoint(
        SignedInUserResolver users, ICommandHandler<PostMyClientRequestMessage, RequestMessage> handler,
        AuditActor auditActor, AuditTrail audit)
    {
        this.users = users; this.handler = handler; this.auditActor = auditActor; this.audit = audit;
    }

    [Function("PostMyClientRequestMessage")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "client-portal/my/requests/{requestId}/messages")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var clientId = ClientScope.OwnClientId(signedInUser);
        if (clientId is null) return new StatusCodeResult(403);

        var posted = await request.ReadFromJsonAsync<PostMyClientRequestMessage>();
        if (posted is null) return new BadRequestResult();
        if (posted.RequestId != requestId) return new BadRequestObjectResult("Route requestId does not match body.");
        if (string.IsNullOrWhiteSpace(posted.Body)) return new BadRequestObjectResult("Message body is required.");
        if (posted.Body.Length > BodyLimit) return new BadRequestObjectResult($"Message body must be {BodyLimit} characters or fewer.");

        var command = posted with
        {
            ClientId = clientId,
            AuthorEmail = signedInUser.Email,
            AuthorName = signedInUser.DisplayName
        };
        auditActor.Email = signedInUser.Email;

        RequestMessage message;
        try { message = await handler.HandleAsync(command, cancellationToken); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }

        await audit.WriteAsync(AuditEventType.NotePosted,
            command.ParentMessageId is null
                ? "Client posted a message on the request's conversation."
                : "Client replied on the request's conversation.",
            pathway: "Client", recordType: RecordType.Request, recordId: command.RequestId,
            cancellationToken: cancellationToken);

        return new OkObjectResult(message);
    }
}
