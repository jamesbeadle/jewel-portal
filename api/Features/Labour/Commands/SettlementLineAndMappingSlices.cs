using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Settlement lines (materials/travel at sign-off level) and the effective-dated Xero mappings
// (scope §3, §6). ManageSettlement-gated throughout.

public sealed class AddWorkerSettlementLineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddWorkerSettlementLineHandler handler;
    public AddWorkerSettlementLineEndpoint(SignedInUserResolver users, AddWorkerSettlementLineHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(AddWorkerSettlementLine))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/settlement-lines")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<AddWorkerSettlementLine>();
        if (command is null) return new BadRequestResult();
        if (command.Nature == SettlementLineNature.CisLabour)
            return new BadRequestObjectResult(new[] { "Labour lines come from approved timesheets only — add materials or travel here." });
        if (command.Amount <= 0m) return new BadRequestObjectResult(new[] { "The amount must be greater than zero." });
        if (string.IsNullOrWhiteSpace(command.ProjectId) || string.IsNullOrWhiteSpace(command.CostCode))
            return new BadRequestObjectResult(new[] { "Pick the site and the cost code the line lands on." });
        return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
    }
}

public sealed class AddWorkerSettlementLineHandler : ICommandHandler<AddWorkerSettlementLine, Acknowledgement>
{
    private readonly JpmsContext context;
    public AddWorkerSettlementLineHandler(JpmsContext context) { this.context = context; }

    public Task<Acknowledgement> HandleAsync(AddWorkerSettlementLine command, CancellationToken cancellationToken) =>
        HandleAsync(command, createdByEmail: "", cancellationToken);

    public async Task<Acknowledgement> HandleAsync(AddWorkerSettlementLine command, string createdByEmail, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken);
        if (worker is null) throw new InvalidOperationException($"Worker {command.WorkerId} does not exist.");
        var entity = new WorkerSettlementLineEntity
        {
            WorkerSettlementLineId = LabourIdentifierFactory.NextWorkerSettlementLineId(),
            WorkerId = command.WorkerId,
            Month = new DateTimeOffset(new DateTime(command.Year, command.Month, 1), TimeSpan.Zero),
            ProjectId = command.ProjectId,
            CostCode = command.CostCode,
            Nature = (int)command.Nature,
            Amount = command.Amount,
            Note = command.Note ?? "",
            CreatedByEmail = createdByEmail,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        context.WorkerSettlementLines.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(entity.WorkerSettlementLineId);
    }
}

public sealed class RemoveWorkerSettlementLineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<RemoveWorkerSettlementLine, Acknowledgement> handler;
    public RemoveWorkerSettlementLineEndpoint(SignedInUserResolver users, ICommandHandler<RemoveWorkerSettlementLine, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RemoveWorkerSettlementLine))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/settlement-lines/remove")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<RemoveWorkerSettlementLine>();
        if (command is null) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class RemoveWorkerSettlementLineHandler : ICommandHandler<RemoveWorkerSettlementLine, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveWorkerSettlementLineHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveWorkerSettlementLine command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkerSettlementLines.FindAsync(new object[] { command.WorkerSettlementLineId }, cancellationToken);
        if (entity is not null)
        {
            context.WorkerSettlementLines.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.WorkerSettlementLineId);
    }
}

// ---- Xero mappings --------------------------------------------------------------------------

public sealed class ListXeroMappingsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListXeroMappings, XeroMappingsSnapshot> handler;
    public ListXeroMappingsEndpoint(SignedInUserResolver users, IQueryHandler<ListXeroMappings, XeroMappingsSnapshot> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListXeroMappings))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labour/xero-mappings")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListXeroMappings(), request.HttpContext.RequestAborted));
    }
}

public sealed class ListXeroMappingsHandler : IQueryHandler<ListXeroMappings, XeroMappingsSnapshot>
{
    private readonly JpmsContext context;
    public ListXeroMappingsHandler(JpmsContext context) { this.context = context; }

    public async Task<XeroMappingsSnapshot> HandleAsync(ListXeroMappings query, CancellationToken cancellationToken)
    {
        var projectNames = await context.Projects
            .ToDictionaryAsync(project => project.ProjectId, project => project.Name, cancellationToken);
        var sites = await context.SiteXeroMappings
            .OrderBy(row => row.ProjectId).ThenBy(row => row.EffectiveFrom)
            .ToListAsync(cancellationToken);
        var codes = await context.CostCodeXeroMappings
            .OrderBy(row => row.CostCode).ThenBy(row => row.EffectiveFrom)
            .ToListAsync(cancellationToken);
        return new XeroMappingsSnapshot(
            sites.Select(row => new SiteXeroMapping(row.SiteXeroMappingId, row.ProjectId,
                projectNames.TryGetValue(row.ProjectId, out var name) ? name : row.ProjectId,
                row.XeroTrackingOptionId, row.XeroTrackingOptionName, row.EffectiveFrom, row.EffectiveTo)).ToList(),
            codes.Select(row => new CostCodeXeroMapping(row.CostCodeXeroMappingId, row.CostCode,
                row.XeroTrackingOptionId, row.XeroTrackingOptionName, row.LabourAccountCode,
                row.MaterialsAccountCode, row.TravelAccountCode, row.EffectiveFrom, row.EffectiveTo)).ToList());
    }
}

public sealed class SetSiteXeroMappingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<SetSiteXeroMapping, Acknowledgement> handler;
    public SetSiteXeroMappingEndpoint(SignedInUserResolver users, ICommandHandler<SetSiteXeroMapping, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SetSiteXeroMapping))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/xero-mappings/site")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<SetSiteXeroMapping>();
        if (command is null) return new BadRequestResult();
        if (string.IsNullOrWhiteSpace(command.ProjectId) || string.IsNullOrWhiteSpace(command.XeroTrackingOptionName))
            return new BadRequestObjectResult(new[] { "Pick the project and the Xero tracking option." });
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class SetSiteXeroMappingHandler : ICommandHandler<SetSiteXeroMapping, Acknowledgement>
{
    private readonly JpmsContext context;
    public SetSiteXeroMappingHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(SetSiteXeroMapping command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        // Close the open row rather than editing it — historic reads keep translating through it.
        var open = await context.SiteXeroMappings
            .Where(row => row.ProjectId == command.ProjectId && row.EffectiveTo == null)
            .ToListAsync(cancellationToken);
        foreach (var row in open) row.EffectiveTo = now;
        var entity = new SiteXeroMappingEntity
        {
            SiteXeroMappingId = LabourIdentifierFactory.NextSiteXeroMappingId(),
            ProjectId = command.ProjectId,
            XeroTrackingOptionId = command.XeroTrackingOptionId ?? "",
            XeroTrackingOptionName = command.XeroTrackingOptionName,
            EffectiveFrom = now,
        };
        context.SiteXeroMappings.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(entity.SiteXeroMappingId);
    }
}

public sealed class SetCostCodeXeroMappingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<SetCostCodeXeroMapping, Acknowledgement> handler;
    public SetCostCodeXeroMappingEndpoint(SignedInUserResolver users, ICommandHandler<SetCostCodeXeroMapping, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SetCostCodeXeroMapping))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/xero-mappings/cost-code")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<SetCostCodeXeroMapping>();
        if (command is null) return new BadRequestResult();
        if (string.IsNullOrWhiteSpace(command.CostCode))
            return new BadRequestObjectResult(new[] { "Pick the cost code to map." });
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class SetCostCodeXeroMappingHandler : ICommandHandler<SetCostCodeXeroMapping, Acknowledgement>
{
    private readonly JpmsContext context;
    public SetCostCodeXeroMappingHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(SetCostCodeXeroMapping command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var open = await context.CostCodeXeroMappings
            .Where(row => row.CostCode == command.CostCode && row.EffectiveTo == null)
            .ToListAsync(cancellationToken);
        foreach (var row in open) row.EffectiveTo = now;
        var entity = new CostCodeXeroMappingEntity
        {
            CostCodeXeroMappingId = LabourIdentifierFactory.NextCostCodeXeroMappingId(),
            CostCode = command.CostCode,
            XeroTrackingOptionId = command.XeroTrackingOptionId ?? "",
            XeroTrackingOptionName = command.XeroTrackingOptionName ?? "",
            LabourAccountCode = command.LabourAccountCode ?? "",
            MaterialsAccountCode = command.MaterialsAccountCode ?? "",
            TravelAccountCode = command.TravelAccountCode ?? "",
            EffectiveFrom = now,
        };
        context.CostCodeXeroMappings.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(entity.CostCodeXeroMappingId);
    }
}
