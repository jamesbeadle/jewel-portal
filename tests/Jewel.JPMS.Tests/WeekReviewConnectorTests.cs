using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Jewel.JPMS.Tests.MyDayWeeksSeed;

namespace Jewel.JPMS.Tests;

/// <summary>The week review and the week planner reach the connector (1 Oct 2026): the reads stay
/// internal, the answers are the directors' and confirm-first where cost posts, and the by-name
/// wrappers land on the portal's own handlers.</summary>
public sealed class WeekReviewConnectorTests
{
    [Fact]
    public void TheActions_areDeclared_gated_andSignOffIsConfirmFirst()
    {
        var actions = AiActionRegistry.All;
        var names = actions.Select(action => action.Name).ToList();
        Assert.Contains("sign_off_submitted_week", names);
        Assert.Contains("send_back_submitted_week", names);
        Assert.Contains("plan_worker_days", names);
        Assert.True(actions.Single(action => action.Name == "sign_off_submitted_week").RequiresConfirmation);
        var projectManager = new SignedInUser("pm@jewelbb.co.uk", "PM", new[] { Role.ProjectManager });
        Assert.DoesNotContain(actions, action => action.Name == "sign_off_submitted_week" && action.VisibleTo.IncludesAny(projectManager.Roles));
        Assert.Contains(actions, action => action.Name == "plan_worker_days" && action.VisibleTo.IncludesAny(projectManager.Roles));
    }

    [Fact]
    public void TheReads_exist_forTheOffice_andNotForASiteRole()
    {
        var director = AiToolCatalogue.ForConnector(new SignedInUser("md@jewelbb.co.uk", "MD", new[] { Role.ManagingDirector })).Select(tool => tool.Name).ToList();
        var operative = AiToolCatalogue.ForConnector(new SignedInUser("jack@example.com", "Jack", new[] { Role.SiteOperative })).Select(tool => tool.Name).ToList();
        Assert.Contains("list_submitted_weeks", director);
        Assert.Contains("view_week_plan", director);
        Assert.DoesNotContain("list_submitted_weeks", operative);
        Assert.DoesNotContain("view_week_plan", operative);
    }

    [Fact]
    public async Task SigningOffByName_findsTheWorkersWeek_andRefusesAWeekNeverSubmitted()
    {
        await using var context = await SeededAsync();
        await LogDaysAsync(context, LastMonday, LastMonday.AddDays(1), LastMonday.AddDays(2), LastMonday.AddDays(3), LastMonday.AddDays(4));
        var handler = new SignOffSubmittedWeekByNameHandler(context, SignOffHandler(context));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new SignOffSubmittedWeekByName("Jack", LastMonday.AddDays(2), Director), CancellationToken.None));
        await SubmitHandler(context).HandleAsync(new MySubmitWeek(LastMonday), Email, CancellationToken.None);

        var signedOff = await handler.HandleAsync(new SignOffSubmittedWeekByName("Jack", LastMonday.AddDays(2), Director), CancellationToken.None);

        Assert.Equal(WorkerWeekSubmissionStatus.SignedOff, signedOff.Status);
        Assert.Equal(Director, signedOff.ReviewedByEmail);
        Assert.All(await context.Timesheets.ToListAsync(), row => Assert.Equal((int)TimesheetStatus.Approved, row.Status));
    }

    [Fact]
    public async Task PlanningByName_marksTheDaysNotIn_andBackIn()
    {
        await using var context = await SeededAsync();
        var handler = new PlanWorkerDaysByNameHandler(context, new PlanWorkerDaysHandler(context));

        await handler.HandleAsync(new PlanWorkerDaysByName("Jack", new[] { LastMonday }, IsIn: false, Director), CancellationToken.None);
        var absence = await context.WorkerAbsences.SingleAsync();
        Assert.Equal(Director, absence.RecordedByEmail);

        await handler.HandleAsync(new PlanWorkerDaysByName("Jack", new[] { LastMonday }, IsIn: true, Director), CancellationToken.None);
        Assert.Empty(await context.WorkerAbsences.ToListAsync());
    }
}
