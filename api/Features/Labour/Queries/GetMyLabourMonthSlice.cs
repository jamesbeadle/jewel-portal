using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>GET /api/my/labour/months/{year}/{month} — one calendar month of the signed-in worker's
/// days with the totals they invoice from: approved hours and days, and what is still outstanding.</summary>
public sealed class MyLabourMonthEndpoint
{
    private const int FirstYear = 2020;
    private const int LastYear = 2100;
    private readonly SignedInUserResolver users;
    private readonly MyLabourMonthHandler handler;
    public MyLabourMonthEndpoint(SignedInUserResolver users, MyLabourMonthHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetMyLabourMonth))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my/labour/months/{year:int}/{month:int}")] HttpRequest request, int year, int month)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var isAMonth = year >= FirstYear && year <= LastYear && month >= 1 && month <= 12;
        if (!isAMonth) return new BadRequestResult();
        try { return new OkObjectResult(await handler.HandleAsync(new GetMyLabourMonth(year, month), signedInUser.Email, cancellationToken)); }
        catch (InvalidOperationException rejection) { return new BadRequestObjectResult(new[] { rejection.Message }); }
    }
}

public sealed class MyLabourMonthHandler : IQueryHandler<GetMyLabourMonth, MyLabourMonth>
{
    private readonly JpmsContext context;
    private readonly MyDayProjects projects;
    private readonly MyDayDays days;
    public MyLabourMonthHandler(JpmsContext context, MyDayProjects projects, MyDayDays days)
    { this.context = context; this.projects = projects; this.days = days; }

    public Task<MyLabourMonth> HandleAsync(GetMyLabourMonth query, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("GetMyLabourMonth requires the signed-in email — use the endpoint.");

    public async Task<MyLabourMonth> HandleAsync(GetMyLabourMonth query, string email, CancellationToken cancellationToken)
    {
        var today = SiteClock.Today();
        var first = new DateTimeOffset(new DateTime(query.Year, query.Month, 1), TimeSpan.Zero);
        var last = first.AddMonths(1).AddDays(-1);
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);
        var cards = await projects.ForAsync(worker, email, today, cancellationToken);
        var rows = await days.BetweenAsync(worker, email, MyDayWeek.SitesOf(cards), first, last, cancellationToken);
        return new MyLabourMonth(query.Year, query.Month, today, rows, MyDayMonthTotals.Of(rows, today));
    }
}
