using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PostRequestMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PostRequestMessageAuthorisation authorisation;
    private readonly PostRequestMessageValidation validation;
    private readonly ICommandHandler<PostRequestMessage, RequestMessage> handler;
    private readonly AuditActor auditActor;
    private readonly AuditTrail audit;

    public PostRequestMessageEndpoint(
        SignedInUserResolver users, PostRequestMessageAuthorisation authorisation,
        PostRequestMessageValidation validation, ICommandHandler<PostRequestMessage, RequestMessage> handler,
        AuditActor auditActor, AuditTrail audit)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.auditActor = auditActor;
        this.audit = audit;
    }

    [Function(nameof(PostRequestMessage))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/messages")] HttpRequest request, string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<PostRequestMessage>();
        if (posted is null) return new BadRequestResult();
        if (posted.RequestId != requestId) return new BadRequestObjectResult("Route requestId does not match body.");

        // The author is always the signed-in user — never trusted from the client body.
        var command = posted with { AuthorEmail = signedInUser.Email, AuthorName = signedInUser.DisplayName };
        auditActor.Email = signedInUser.Email;

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        RequestMessage message;
        try { message = await handler.HandleAsync(command, cancellationToken); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }

        await audit.WriteAsync(AuditEventType.NotePosted,
            command.ParentMessageId is null
                ? "Posted a message on the request's conversation."
                : "Replied on the request's conversation.",
            recordType: RecordType.Request, recordId: command.RequestId,
            cancellationToken: cancellationToken);

        return new OkObjectResult(message);
    }
}
