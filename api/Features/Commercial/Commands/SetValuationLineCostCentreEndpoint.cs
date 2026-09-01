using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetValuationLineCostCentreEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly SetValuationLineCostCentreValidation validation;
    private readonly ICommandHandler<SetValuationLineCostCentre, ValuationLineItem> handler;
    private readonly Audit.AuditActor auditActor;
    public SetValuationLineCostCentreEndpoint(SignedInUserResolver users, ValuationReportAuthorisation authorisation, SetValuationLineCostCentreValidation validation, ICommandHandler<SetValuationLineCostCentre, ValuationLineItem> handler, Audit.AuditActor auditActor)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; this.auditActor = auditActor; }

    [Function(nameof(SetValuationLineCostCentre))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "valuation-lines/{lineItemId}/cost-centre")] HttpRequest request, string lineItemId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email; // audit rows record who moved the money
        var command = await request.ReadFromJsonAsync<SetValuationLineCostCentre>();
        if (command is null) return new BadRequestResult();
        if (command.ValuationLineItemId != lineItemId) return new BadRequestObjectResult("Route lineItemId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
