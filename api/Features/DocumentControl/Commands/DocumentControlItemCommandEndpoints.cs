using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

/// <summary>
/// The item-scoped Document Control commands, one route each under
/// <c>document-control/items/{itemId}/…</c>: file-as-drawing, file-as-payment-certificate,
/// file-to-subcontractor, discard, restore. One class because the plumbing is identical —
/// resolve the user, gate on DocumentControlRoles, stamp the audit actor, run the handler,
/// surface InvalidOperationException as a 400 the dialog shows verbatim.
/// </summary>
public sealed class DocumentControlItemCommandEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly FileDocumentAsDrawingAuthorisation drawingAuthorisation;
    private readonly FileDocumentAsDrawingValidation drawingValidation;
    private readonly ICommandHandler<FileDocumentAsDrawing, DocumentControlItem> drawingHandler;
    private readonly FileDocumentAsPaymentCertificateAuthorisation certificateAuthorisation;
    private readonly FileDocumentAsPaymentCertificateValidation certificateValidation;
    private readonly ICommandHandler<FileDocumentAsPaymentCertificate, DocumentControlItem> certificateHandler;
    private readonly FileDocumentToSubcontractorAuthorisation subcontractorAuthorisation;
    private readonly FileDocumentToSubcontractorValidation subcontractorValidation;
    private readonly ICommandHandler<FileDocumentToSubcontractor, DocumentControlItem> subcontractorHandler;
    private readonly ICommandHandler<DiscardDocumentControlItem, DocumentControlItem> discardHandler;
    private readonly ICommandHandler<RestoreDocumentControlItem, DocumentControlItem> restoreHandler;

    public DocumentControlItemCommandEndpoints(
        SignedInUserResolver users, AuditActor auditActor,
        FileDocumentAsDrawingAuthorisation drawingAuthorisation,
        FileDocumentAsDrawingValidation drawingValidation,
        ICommandHandler<FileDocumentAsDrawing, DocumentControlItem> drawingHandler,
        FileDocumentAsPaymentCertificateAuthorisation certificateAuthorisation,
        FileDocumentAsPaymentCertificateValidation certificateValidation,
        ICommandHandler<FileDocumentAsPaymentCertificate, DocumentControlItem> certificateHandler,
        FileDocumentToSubcontractorAuthorisation subcontractorAuthorisation,
        FileDocumentToSubcontractorValidation subcontractorValidation,
        ICommandHandler<FileDocumentToSubcontractor, DocumentControlItem> subcontractorHandler,
        ICommandHandler<DiscardDocumentControlItem, DocumentControlItem> discardHandler,
        ICommandHandler<RestoreDocumentControlItem, DocumentControlItem> restoreHandler)
    {
        this.users = users; this.auditActor = auditActor;
        this.drawingAuthorisation = drawingAuthorisation;
        this.drawingValidation = drawingValidation;
        this.drawingHandler = drawingHandler;
        this.certificateAuthorisation = certificateAuthorisation;
        this.certificateValidation = certificateValidation;
        this.certificateHandler = certificateHandler;
        this.subcontractorAuthorisation = subcontractorAuthorisation;
        this.subcontractorValidation = subcontractorValidation;
        this.subcontractorHandler = subcontractorHandler;
        this.discardHandler = discardHandler;
        this.restoreHandler = restoreHandler;
    }

    [Function(nameof(FileDocumentAsDrawing))]
    public async Task<IActionResult> FileAsDrawing(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "document-control/items/{itemId}/file-as-drawing")] HttpRequest request,
        string itemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<FileDocumentAsDrawing>();
        if (command is null) return new BadRequestResult();
        if (command.DocumentControlItemId != itemId) return new BadRequestObjectResult("Route itemId does not match body.");

        if (!drawingAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = drawingValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try { return new OkObjectResult(await drawingHandler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }

    [Function(nameof(FileDocumentAsPaymentCertificate))]
    public async Task<IActionResult> FileAsPaymentCertificate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "document-control/items/{itemId}/file-as-payment-certificate")] HttpRequest request,
        string itemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<FileDocumentAsPaymentCertificate>();
        if (command is null) return new BadRequestResult();
        if (command.DocumentControlItemId != itemId) return new BadRequestObjectResult("Route itemId does not match body.");

        if (!certificateAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = certificateValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try { return new OkObjectResult(await certificateHandler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }

    [Function(nameof(FileDocumentToSubcontractor))]
    public async Task<IActionResult> FileToSubcontractor(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "document-control/items/{itemId}/file-to-subcontractor")] HttpRequest request,
        string itemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<FileDocumentToSubcontractor>();
        if (command is null) return new BadRequestResult();
        if (command.DocumentControlItemId != itemId) return new BadRequestObjectResult("Route itemId does not match body.");

        if (!subcontractorAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = subcontractorValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try { return new OkObjectResult(await subcontractorHandler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }

    [Function(nameof(DiscardDocumentControlItem))]
    public async Task<IActionResult> Discard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "document-control/items/{itemId}/discard")] HttpRequest request,
        string itemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DocumentControlRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        auditActor.Email = signedInUser.Email;

        try { return new OkObjectResult(await discardHandler.HandleAsync(new DiscardDocumentControlItem(itemId), cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }

    [Function(nameof(RestoreDocumentControlItem))]
    public async Task<IActionResult> Restore(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "document-control/items/{itemId}/restore")] HttpRequest request,
        string itemId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!DocumentControlRoles.AllowedToManage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        auditActor.Email = signedInUser.Email;

        try { return new OkObjectResult(await restoreHandler.HandleAsync(new RestoreDocumentControlItem(itemId), cancellationToken)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
