using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UploadComplianceDocumentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UploadComplianceDocumentAuthorisation authorisation;
    private readonly UploadComplianceDocumentValidation validation;
    private readonly ICommandHandler<UploadComplianceDocument, ComplianceDocument> handler;

    public UploadComplianceDocumentEndpoint(SignedInUserResolver users, UploadComplianceDocumentAuthorisation authorisation, UploadComplianceDocumentValidation validation, ICommandHandler<UploadComplianceDocument, ComplianceDocument> handler)
    {
        this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler;
    }

    [Function(nameof(UploadComplianceDocument))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subcontractors/{subcontractorId}/compliance")] HttpRequest request,
        string subcontractorId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<UploadComplianceDocument>();
        if (command is null) return new BadRequestResult();
        if (command.SubcontractorId != subcontractorId) return new BadRequestObjectResult("Route subcontractorId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
