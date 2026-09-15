using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales;

// The estimates on a lead (2026-09-15), beside SalesLeadEndpoints: one function per read or
// write, the same gating as the lead ones — reads on SalesRoles.Readers, each write through its
// command's Authorisation and Validation with the actor stamped from the signed-in user, never
// taken from the body. Business refusals read back as 400 with the message. The edit and the
// status move are SalesEstimateEndpoints.Changes.
public sealed partial class SalesEstimateEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly IQueryHandler<GetEstimate, LeadEstimate?> get;
    private readonly CreateEstimateAuthorisation createAuthorisation;
    private readonly CreateEstimateValidation createValidation;
    private readonly ICommandHandler<CreateEstimate, LeadEstimate> create;
    private readonly UpdateEstimateDetailsAuthorisation updateAuthorisation;
    private readonly UpdateEstimateDetailsValidation updateValidation;
    private readonly ICommandHandler<UpdateEstimateDetails, LeadEstimate> update;
    private readonly MoveEstimateStatusAuthorisation moveAuthorisation;
    private readonly MoveEstimateStatusValidation moveValidation;
    private readonly ICommandHandler<MoveEstimateStatus, LeadEstimate> move;

    public SalesEstimateEndpoints(
        SignedInUserResolver users,
        AuditActor auditActor,
        IQueryHandler<GetEstimate, LeadEstimate?> get,
        CreateEstimateAuthorisation createAuthorisation,
        CreateEstimateValidation createValidation,
        ICommandHandler<CreateEstimate, LeadEstimate> create,
        UpdateEstimateDetailsAuthorisation updateAuthorisation,
        UpdateEstimateDetailsValidation updateValidation,
        ICommandHandler<UpdateEstimateDetails, LeadEstimate> update,
        MoveEstimateStatusAuthorisation moveAuthorisation,
        MoveEstimateStatusValidation moveValidation,
        ICommandHandler<MoveEstimateStatus, LeadEstimate> move)
    {
        this.users = users; this.auditActor = auditActor; this.get = get;
        this.createAuthorisation = createAuthorisation; this.createValidation = createValidation; this.create = create;
        this.updateAuthorisation = updateAuthorisation; this.updateValidation = updateValidation; this.update = update;
        this.moveAuthorisation = moveAuthorisation; this.moveValidation = moveValidation; this.move = move;
    }

    [Function(nameof(GetEstimate))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/estimates/{estimateId}")] HttpRequest request, string estimateId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        // Null renders as 204 — the query client reads an empty success as "no such record".
        return new OkObjectResult(await get.HandleAsync(new GetEstimate(estimateId), request.HttpContext.RequestAborted));
    }

    [Function(nameof(CreateEstimate))]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/estimates")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<CreateEstimate>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");
        var command = posted with { CreatedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!createAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = createValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => create.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    private static async Task<IActionResult> Run<T>(Func<Task<T>> handle)
    {
        try { return new OkObjectResult(await handle()); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
