using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class ImportXeroSupplierEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly ImportXeroSupplierAuthorisation authorisation;
    private readonly ImportXeroSupplierValidation validation;
    private readonly ICommandHandler<ImportXeroSupplier, Subcontractor> handler;

    public ImportXeroSupplierEndpoint(SignedInUserResolver users, AuditActor auditActor, ImportXeroSupplierAuthorisation authorisation, ImportXeroSupplierValidation validation, ICommandHandler<ImportXeroSupplier, Subcontractor> handler)
    {
        this.users = users; this.auditActor = auditActor; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(ImportXeroSupplier))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/import-from-xero")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<ImportXeroSupplier>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        auditActor.Email = signedInUser.Email; // the Xero link records who imported

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // The handler's guards (supplier already imported, supplier gone from Xero, Xero unreachable) are answers to what was asked, not faults — 400 so the
            // dialog shows the reason instead of the client falling back to an opaque 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
