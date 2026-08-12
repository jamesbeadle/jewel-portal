using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class SendAttachmentsToDocumentControlEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly SendAttachmentsToDocumentControlAuthorisation authorisation;
    private readonly SendAttachmentsToDocumentControlValidation validation;
    private readonly ICommandHandler<SendAttachmentsToDocumentControl, IReadOnlyList<DocumentControlItem>> handler;

    public SendAttachmentsToDocumentControlEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        SendAttachmentsToDocumentControlAuthorisation authorisation,
        SendAttachmentsToDocumentControlValidation validation,
        ICommandHandler<SendAttachmentsToDocumentControl, IReadOnlyList<DocumentControlItem>> handler)
    {
        this.users = users; this.auditActor = auditActor;
        this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(SendAttachmentsToDocumentControl))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "document-control/send")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<SendAttachmentsToDocumentControl>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
