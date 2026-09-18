using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// POST /api/variation-orders/{variationOrderId}/email — email the variation order's official
/// document from the projects mailbox to the project's client side. Optional JSON body
/// { "recipientOverride": "someone@x.com", "saveAsDraftOnly": true } addresses it to one ad-hoc
/// email instead, and/or leaves the reviewed draft in Drafts for Outlook rather than sending.
/// </summary>
public sealed class SendVariationOrderEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendVariationOrderEmailAuthorisation authorisation;
    private readonly SendVariationOrderEmailValidation validation;
    private readonly ICommandHandler<SendVariationOrderEmail, VariationOrderEmailOutcome> handler;
    private readonly AuditActor auditActor;

    public SendVariationOrderEmailEndpoint(
        SignedInUserResolver users,
        SendVariationOrderEmailAuthorisation authorisation,
        SendVariationOrderEmailValidation validation,
        ICommandHandler<SendVariationOrderEmail, VariationOrderEmailOutcome> handler,
        AuditActor auditActor)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.auditActor = auditActor;
    }

    [Function(nameof(SendVariationOrderEmail))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post",
            Route = "variation-orders/{variationOrderId}/email")] HttpRequest request,
        string variationOrderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        // The dispatcher's audit row attributes the email to whoever pressed the button — commands
        // don't carry the caller's identity, so it reaches the handler through the scoped actor.
        auditActor.Email = signedInUser.Email;

        SendVariationOrderEmail? body = null;
        if (request.ContentLength > 0)
        {
            try { body = await request.ReadFromJsonAsync<SendVariationOrderEmail>(); }
            catch { /* an empty or non-JSON body means "no override, send it" */ }
        }
        var command = new SendVariationOrderEmail(
            variationOrderId, body?.RecipientOverride, body?.SaveAsDraftOnly ?? false);

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // A quoting-stage variation, no client contact, an unconfigured mailbox — all
            // user-fixable, so the message is surfaced verbatim for the dialog to show.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
