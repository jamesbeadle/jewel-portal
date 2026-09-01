using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class PostVariationOrderMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PostVariationOrderMessageAuthorisation authorisation;
    private readonly PostVariationOrderMessageValidation validation;
    private readonly ICommandHandler<PostVariationOrderMessage, VariationOrderMessage> handler;
    private readonly AuditActor auditActor;
    private readonly AuditTrail audit;

    public PostVariationOrderMessageEndpoint(
        SignedInUserResolver users, PostVariationOrderMessageAuthorisation authorisation,
        PostVariationOrderMessageValidation validation,
        ICommandHandler<PostVariationOrderMessage, VariationOrderMessage> handler,
        AuditActor auditActor, AuditTrail audit)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.auditActor = auditActor;
        this.audit = audit;
    }

    [Function(nameof(PostVariationOrderMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "variation-orders/{voId}/messages")] HttpRequest request,
        string voId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<PostVariationOrderMessage>();
        if (posted is null) return new BadRequestResult();
        if (posted.VariationOrderId != voId) return new BadRequestObjectResult("Route voId does not match body.");

        // The author is always the signed-in user — never trusted from the client body.
        var command = posted with { AuthorEmail = signedInUser.Email, AuthorName = signedInUser.DisplayName };
        auditActor.Email = signedInUser.Email;

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        VariationOrderMessage message;
        try { message = await handler.HandleAsync(command, cancellationToken); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }

        await audit.WriteAsync(AuditEventType.VariationNotePosted,
            command.ParentMessageId is null
                ? "Posted a message on the variation order's conversation."
                : "Replied on the variation order's conversation.",
            recordType: RecordType.Variation, recordId: command.VariationOrderId,
            cancellationToken: cancellationToken);

        return new OkObjectResult(message);
    }
}
