using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>GET /api/my/labour/weeks/{weekStart} — one week of the signed-in worker's days, Monday
/// to Sunday, with the week's submission if it has been sent to the office. Any week up to the
/// current one; a week still to come is refused.</summary>
public sealed class MyLabourWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MyLabourWeekHandler handler;
    public MyLabourWeekEndpoint(SignedInUserResolver users, MyLabourWeekHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetMyLabourWeek))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my/labour/weeks/{weekStart}")] HttpRequest request, string weekStart)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!DateTimeOffset.TryParse(weekStart, out var inWeek)) return new BadRequestObjectResult(new[] { "Name the week by a date in it." });
        try { return new OkObjectResult(await handler.HandleAsync(new GetMyLabourWeek(inWeek), signedInUser.Email, cancellationToken)); }
        catch (InvalidOperationException rejection) { return new BadRequestObjectResult(new[] { rejection.Message }); }
    }
}

public sealed class MyLabourWeekHandler : IQueryHandler<GetMyLabourWeek, MyLabourWeek>
{
    private readonly JpmsContext context;
    private readonly MyDayProjects projects;
    private readonly MyDayDays days;
    public MyLabourWeekHandler(JpmsContext context, MyDayProjects projects, MyDayDays days)
    { this.context = context; this.projects = projects; this.days = days; }

    public Task<MyLabourWeek> HandleAsync(GetMyLabourWeek query, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("GetMyLabourWeek requires the signed-in email — use the endpoint.");

    public async Task<MyLabourWeek> HandleAsync(GetMyLabourWeek query, string email, CancellationToken cancellationToken)
    {
        var today = SiteClock.Today();
        var monday = LabourWeeks.MondayOf(query.WeekStart);
        if (monday > today) throw new InvalidOperationException("That week hasn't started yet.");
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);
        return await ForAsync(worker, email, monday, today, cancellationToken);
    }

    public async Task<MyLabourWeek> ForAsync(WorkerEntity worker, string email, DateTimeOffset monday, DateTimeOffset today, CancellationToken cancellationToken)
    {
        var cards = await projects.ForAsync(worker, email, today, cancellationToken);
        var rows = await days.BetweenAsync(worker, email, MyDayWeek.SitesOf(cards), monday, monday.AddDays(LabourWeeks.DaysInWeek - 1), cancellationToken);
        var submission = await WorkerWeekSubmissions.MineAsync(context, worker.WorkerId, monday, cancellationToken);
        var refusal = MyWeekSubmissionRules.Refusal(monday, today, rows, submission);
        return new MyLabourWeek(monday, today, rows, submission, refusal is null, refusal ?? "");
    }
}
