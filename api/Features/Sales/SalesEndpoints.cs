using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales;

// The Sales section's HTTP surface — two classes (leads, strategies), one function per read or
// write. Reads are gated on SalesRoles.Readers; each write runs its command's Authorisation and
// Validation, with the actor stamped from the signed-in user, never taken from the body.
// Business refusals (unknown lead, already Won, duplicate project reference, no Anthropic key)
// read back as 400 with the message, which the calling dialog shows in place.
public sealed class SalesLeadEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly IQueryHandler<ListLeads, IReadOnlyList<Lead>> list;
    private readonly IQueryHandler<GetLead, LeadDetail?> get;
    private readonly CaptureLeadAuthorisation captureAuthorisation;
    private readonly CaptureLeadValidation captureValidation;
    private readonly ICommandHandler<CaptureLead, Lead> capture;
    private readonly UpdateLeadAuthorisation updateAuthorisation;
    private readonly UpdateLeadValidation updateValidation;
    private readonly ICommandHandler<UpdateLead, Lead> update;
    private readonly MoveLeadStageAuthorisation moveAuthorisation;
    private readonly MoveLeadStageValidation moveValidation;
    private readonly ICommandHandler<MoveLeadStage, Lead> move;
    private readonly WinLeadAuthorisation winAuthorisation;
    private readonly WinLeadValidation winValidation;
    private readonly ICommandHandler<WinLead, LeadWonOutcome> win;
    private readonly LogLeadActivityAuthorisation logAuthorisation;
    private readonly LogLeadActivityValidation logValidation;
    private readonly ICommandHandler<LogLeadActivity, LeadActivity> log;

    public SalesLeadEndpoints(
        SignedInUserResolver users,
        AuditActor auditActor,
        IQueryHandler<ListLeads, IReadOnlyList<Lead>> list,
        IQueryHandler<GetLead, LeadDetail?> get,
        CaptureLeadAuthorisation captureAuthorisation,
        CaptureLeadValidation captureValidation,
        ICommandHandler<CaptureLead, Lead> capture,
        UpdateLeadAuthorisation updateAuthorisation,
        UpdateLeadValidation updateValidation,
        ICommandHandler<UpdateLead, Lead> update,
        MoveLeadStageAuthorisation moveAuthorisation,
        MoveLeadStageValidation moveValidation,
        ICommandHandler<MoveLeadStage, Lead> move,
        WinLeadAuthorisation winAuthorisation,
        WinLeadValidation winValidation,
        ICommandHandler<WinLead, LeadWonOutcome> win,
        LogLeadActivityAuthorisation logAuthorisation,
        LogLeadActivityValidation logValidation,
        ICommandHandler<LogLeadActivity, LeadActivity> log)
    {
        this.users = users; this.auditActor = auditActor; this.list = list; this.get = get;
        this.captureAuthorisation = captureAuthorisation; this.captureValidation = captureValidation; this.capture = capture;
        this.updateAuthorisation = updateAuthorisation; this.updateValidation = updateValidation; this.update = update;
        this.moveAuthorisation = moveAuthorisation; this.moveValidation = moveValidation; this.move = move;
        this.winAuthorisation = winAuthorisation; this.winValidation = winValidation; this.win = win;
        this.logAuthorisation = logAuthorisation; this.logValidation = logValidation; this.log = log;
    }

    [Function(nameof(ListLeads))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/leads")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await list.HandleAsync(new ListLeads(), request.HttpContext.RequestAborted));
    }

    [Function(nameof(GetLead))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/leads/{leadId}")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var detail = await get.HandleAsync(new GetLead(leadId), request.HttpContext.RequestAborted);
        // Null renders as 204 — the query client reads an empty success as "no such record".
        return new OkObjectResult(detail);
    }

    [Function(nameof(CaptureLead))]
    public async Task<IActionResult> Capture(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<CaptureLead>();
        if (command is null) return new BadRequestResult();
        auditActor.Email = signedInUser.Email;
        if (!captureAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = captureValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => capture.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(UpdateLead))]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sales/leads/{leadId}")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateLead>();
        if (command is null) return new BadRequestResult();
        if (command.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");
        auditActor.Email = signedInUser.Email;
        if (!updateAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = updateValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => update.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(MoveLeadStage))]
    public async Task<IActionResult> Move(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/stage")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<MoveLeadStage>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");
        var command = posted with { ChangedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!moveAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = moveValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => move.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(WinLead))]
    public async Task<IActionResult> Win(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/win")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<WinLead>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");
        var command = posted with { DecidedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!winAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = winValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => win.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(LogLeadActivity))]
    public async Task<IActionResult> Log(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/activities")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<LogLeadActivity>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");
        var command = posted with { RecordedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!logAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = logValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => log.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    private static async Task<IActionResult> Run<T>(Func<Task<T>> handle)
    {
        try { return new OkObjectResult(await handle()); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}

public sealed class SalesStrategyEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly IQueryHandler<ListSalesStrategies, IReadOnlyList<SalesStrategyOverview>> list;
    private readonly IQueryHandler<GetSalesStrategy, SalesStrategyDetail?> get;
    private readonly CreateSalesStrategyAuthorisation createAuthorisation;
    private readonly CreateSalesStrategyValidation createValidation;
    private readonly ICommandHandler<CreateSalesStrategy, SalesStrategy> create;
    private readonly UpdateSalesStrategyAuthorisation updateAuthorisation;
    private readonly UpdateSalesStrategyValidation updateValidation;
    private readonly ICommandHandler<UpdateSalesStrategy, SalesStrategy> update;
    private readonly SetSalesStrategyStatusAuthorisation statusAuthorisation;
    private readonly SetSalesStrategyStatusValidation statusValidation;
    private readonly ICommandHandler<SetSalesStrategyStatus, SalesStrategy> setStatus;
    private readonly GenerateStrategyApproachPlanAuthorisation planAuthorisation;
    private readonly GenerateStrategyApproachPlanValidation planValidation;
    private readonly ICommandHandler<GenerateStrategyApproachPlan, SalesStrategy> generatePlan;

    public SalesStrategyEndpoints(
        SignedInUserResolver users,
        AuditActor auditActor,
        IQueryHandler<ListSalesStrategies, IReadOnlyList<SalesStrategyOverview>> list,
        IQueryHandler<GetSalesStrategy, SalesStrategyDetail?> get,
        CreateSalesStrategyAuthorisation createAuthorisation,
        CreateSalesStrategyValidation createValidation,
        ICommandHandler<CreateSalesStrategy, SalesStrategy> create,
        UpdateSalesStrategyAuthorisation updateAuthorisation,
        UpdateSalesStrategyValidation updateValidation,
        ICommandHandler<UpdateSalesStrategy, SalesStrategy> update,
        SetSalesStrategyStatusAuthorisation statusAuthorisation,
        SetSalesStrategyStatusValidation statusValidation,
        ICommandHandler<SetSalesStrategyStatus, SalesStrategy> setStatus,
        GenerateStrategyApproachPlanAuthorisation planAuthorisation,
        GenerateStrategyApproachPlanValidation planValidation,
        ICommandHandler<GenerateStrategyApproachPlan, SalesStrategy> generatePlan)
    {
        this.users = users; this.auditActor = auditActor; this.list = list; this.get = get;
        this.createAuthorisation = createAuthorisation; this.createValidation = createValidation; this.create = create;
        this.updateAuthorisation = updateAuthorisation; this.updateValidation = updateValidation; this.update = update;
        this.statusAuthorisation = statusAuthorisation; this.statusValidation = statusValidation; this.setStatus = setStatus;
        this.planAuthorisation = planAuthorisation; this.planValidation = planValidation; this.generatePlan = generatePlan;
    }

    [Function(nameof(ListSalesStrategies))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/strategies")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await list.HandleAsync(new ListSalesStrategies(), request.HttpContext.RequestAborted));
    }

    [Function(nameof(GetSalesStrategy))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/strategies/{strategyId}")] HttpRequest request, string strategyId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var detail = await get.HandleAsync(new GetSalesStrategy(strategyId), request.HttpContext.RequestAborted);
        // Null renders as 204 — the query client reads an empty success as "no such record".
        return new OkObjectResult(detail);
    }

    [Function(nameof(CreateSalesStrategy))]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/strategies")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<CreateSalesStrategy>();
        if (command is null) return new BadRequestResult();
        auditActor.Email = signedInUser.Email;
        if (!createAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = createValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => create.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(UpdateSalesStrategy))]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sales/strategies/{strategyId}")] HttpRequest request, string strategyId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateSalesStrategy>();
        if (command is null) return new BadRequestResult();
        if (command.StrategyId != strategyId) return new BadRequestObjectResult("Route strategyId does not match body.");
        auditActor.Email = signedInUser.Email;
        if (!updateAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = updateValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => update.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(SetSalesStrategyStatus))]
    public async Task<IActionResult> SetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/strategies/{strategyId}/status")] HttpRequest request, string strategyId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<SetSalesStrategyStatus>();
        if (command is null) return new BadRequestResult();
        if (command.StrategyId != strategyId) return new BadRequestObjectResult("Route strategyId does not match body.");
        auditActor.Email = signedInUser.Email;
        if (!statusAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = statusValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => setStatus.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(GenerateStrategyApproachPlan))]
    public async Task<IActionResult> GeneratePlan(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/strategies/{strategyId}/plan")] HttpRequest request, string strategyId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<GenerateStrategyApproachPlan>();
        if (command is null) return new BadRequestResult();
        if (command.StrategyId != strategyId) return new BadRequestObjectResult("Route strategyId does not match body.");
        auditActor.Email = signedInUser.Email;
        if (!planAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = planValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => generatePlan.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    private static async Task<IActionResult> Run<T>(Func<Task<T>> handle)
    {
        try { return new OkObjectResult(await handle()); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
