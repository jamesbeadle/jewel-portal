using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Queries;

/// <summary>The project's audits, newest first — the H&S tab's list.</summary>
public sealed class ListHsAuditsForProjectHandler : IQueryHandler<ListHsAuditsForProject, IReadOnlyList<HsAudit>>
{
    private readonly JpmsContext context;
    public ListHsAuditsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<HsAudit>> HandleAsync(ListHsAuditsForProject query, CancellationToken cancellationToken)
    {
        var audits = await context.HsAudits.AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderByDescending(row => row.Number)
            .ToListAsync(cancellationToken);
        return audits.Select(row => row.ToModel()).ToList();
    }
}

/// <summary>Every project's audits, newest first — the officer's home, one read for every site.</summary>
public sealed class ListHsAuditsAcrossProjectsHandler : IQueryHandler<ListHsAuditsAcrossProjects, IReadOnlyList<HsAudit>>
{
    private readonly JpmsContext context;
    public ListHsAuditsAcrossProjectsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<HsAudit>> HandleAsync(ListHsAuditsAcrossProjects query, CancellationToken cancellationToken)
    {
        var audits = await context.HsAudits.AsNoTracking()
            .OrderByDescending(row => row.InspectionDate)
            .ThenByDescending(row => row.Number)
            .ToListAsync(cancellationToken);
        return audits.Select(row => row.ToModel()).ToList();
    }
}

/// <summary>One audit with its items in template order — the form page's one fetch.</summary>
public sealed class GetHsAuditHandler : IQueryHandler<GetHsAudit, HsAuditView>
{
    private readonly JpmsContext context;
    public GetHsAuditHandler(JpmsContext context) { this.context = context; }

    public async Task<HsAuditView> HandleAsync(GetHsAudit query, CancellationToken cancellationToken)
    {
        var audit = await context.HsAudits.AsNoTracking()
            .FirstOrDefaultAsync(row => row.HsAuditId == query.HsAuditId, cancellationToken)
            ?? throw new InvalidOperationException("That audit no longer exists.");
        var items = await context.HsAuditItems.AsNoTracking()
            .Where(row => row.HsAuditId == query.HsAuditId)
            .OrderBy(row => row.DisplayOrder)
            .ToListAsync(cancellationToken);
        return new HsAuditView(audit.ToModel(), items.Select(row => row.ToModel()).ToList());
    }
}

public sealed class HsAuditQueryEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListHsAuditsForProject, IReadOnlyList<HsAudit>> list;
    private readonly IQueryHandler<ListHsAuditsAcrossProjects, IReadOnlyList<HsAudit>> listAcrossProjects;
    private readonly IQueryHandler<GetHsAudit, HsAuditView> get;

    public HsAuditQueryEndpoints(
        SignedInUserResolver users,
        IQueryHandler<ListHsAuditsForProject, IReadOnlyList<HsAudit>> list,
        IQueryHandler<ListHsAuditsAcrossProjects, IReadOnlyList<HsAudit>> listAcrossProjects,
        IQueryHandler<GetHsAudit, HsAuditView> get)
    {
        this.users = users;
        this.list = list;
        this.listAcrossProjects = listAcrossProjects;
        this.get = get;
    }

    [Function(nameof(ListHsAuditsAcrossProjects))]
    public async Task<IActionResult> ListAcrossProjects(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-audits")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!HsAuditRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await listAcrossProjects.HandleAsync(new ListHsAuditsAcrossProjects(), cancellationToken));
    }

    [Function(nameof(ListHsAuditsForProject))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/hs-audits")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!HsAuditRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await list.HandleAsync(new ListHsAuditsForProject(projectId), cancellationToken));
    }

    [Function(nameof(GetHsAudit))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hs-audits/{auditId}")] HttpRequest request,
        string auditId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!HsAuditRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await get.HandleAsync(new GetHsAudit(auditId), cancellationToken));
    }
}
