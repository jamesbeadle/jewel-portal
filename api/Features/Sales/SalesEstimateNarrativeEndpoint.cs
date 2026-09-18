using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales;

// PUT sales/estimates/{estimateId}/narrative — the estimate document's three texts (2026-09-18).
// Its own endpoint class rather than a sixth set of gates on SalesEstimateEndpoints, whose
// constructor is already the whole of that file.
public sealed class SalesEstimateNarrativeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly SetEstimateNarrativeAuthorisation authorisation;
    private readonly SetEstimateNarrativeValidation validation;
    private readonly ICommandHandler<SetEstimateNarrative, LeadEstimate> handler;

    public SalesEstimateNarrativeEndpoint(
        SignedInUserResolver users,
        AuditActor auditActor,
        SetEstimateNarrativeAuthorisation authorisation,
        SetEstimateNarrativeValidation validation,
        ICommandHandler<SetEstimateNarrative, LeadEstimate> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetEstimateNarrative))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sales/estimates/{estimateId}/narrative")] HttpRequest request, string estimateId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<SetEstimateNarrative>();
        if (command is null) return new BadRequestResult();
        if (command.EstimateId != estimateId) return new BadRequestObjectResult("Route estimateId does not match body.");
        auditActor.Email = signedInUser.Email;
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);

        try { return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted)); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
