using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.DocumentControl;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Its own class rather than another arm of DocumentControlItemCommandEndpoints — the plumbing is
// the same (resolve, gate, run, surface InvalidOperationException as a readable 400), but that
// class's constructor is already at the edge of legibility.
public sealed class ExtractDocumentControlArchiveEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly ExtractDocumentControlArchiveAuthorisation authorisation;
    private readonly ExtractDocumentControlArchiveValidation validation;
    private readonly ICommandHandler<ExtractDocumentControlArchive, IReadOnlyList<DocumentControlItem>> handler;

    public ExtractDocumentControlArchiveEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        ExtractDocumentControlArchiveAuthorisation authorisation,
        ExtractDocumentControlArchiveValidation validation,
        ICommandHandler<ExtractDocumentControlArchive, IReadOnlyList<DocumentControlItem>> handler)
    {
        this.users = users; this.auditActor = auditActor;
        this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(ExtractDocumentControlArchive))]
    public async Task<IActionResult> Extract(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "document-control/items/{itemId}/extract-archive")] HttpRequest request,
        string itemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = new ExtractDocumentControlArchive(itemId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try { return new OkObjectResult(await handler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
